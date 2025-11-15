using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace TrackFi.Infrastructure.Caching;

/// <summary>
/// Service for distributed caching using Redis.
/// Provides type-safe caching with serialization/deserialization.
/// Can be disabled via configuration for development/testing.
/// </summary>
public class DistributedCacheService
{
    private readonly IDistributedCache _cache;
    private readonly ILogger<DistributedCacheService> _logger;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly CacheOptions _cacheOptions;

    public DistributedCacheService(
        IDistributedCache cache,
        ILogger<DistributedCacheService> logger,
        IOptions<CacheOptions> cacheOptions)
    {
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _cacheOptions = cacheOptions?.Value ?? throw new ArgumentNullException(nameof(cacheOptions));
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };

        if (!_cacheOptions.Enabled)
        {
            _logger.LogWarning("Caching is DISABLED. All cache operations will be bypassed.");
        }
    }

    /// <summary>
    /// Gets a cached value or creates it using the provided factory.
    /// </summary>
    public async Task<T?> GetOrCreateAsync<T>(
        string key,
        Func<Task<T>> factory,
        TimeSpan expiration,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Cache key cannot be empty", nameof(key));

        if (factory == null)
            throw new ArgumentNullException(nameof(factory));

        // Try to get from cache
        var cached = await GetAsync<T>(key, cancellationToken);
        if (cached != null)
        {
            _logger.LogDebug("Cache HIT: {Key}", key);
            return cached;
        }

        _logger.LogDebug("Cache MISS: {Key}", key);

        // Fetch fresh data
        var value = await factory();

        if (value != null)
        {
            // Store in cache
            await SetAsync(key, value, expiration, cancellationToken);
        }

        return value;
    }

    /// <summary>
    /// Gets a value from cache.
    /// </summary>
    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        if (!_cacheOptions.Enabled)
        {
            _logger.LogDebug("Cache is disabled, skipping GET for key: {Key}", key);
            return default;
        }

        if (string.IsNullOrWhiteSpace(key))
            return default;

        try
        {
            var cached = await _cache.GetStringAsync(key, cancellationToken);
            if (cached == null)
                return default;

            return JsonSerializer.Deserialize<T>(cached, _jsonOptions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting value from cache for key: {Key}", key);
            return default;
        }
    }

    /// <summary>
    /// Sets a value in cache.
    /// </summary>
    public async Task SetAsync<T>(
        string key,
        T value,
        TimeSpan expiration,
        CancellationToken cancellationToken = default)
    {
        if (!_cacheOptions.Enabled)
        {
            _logger.LogDebug("Cache is disabled, skipping SET for key: {Key}", key);
            return;
        }

        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Cache key cannot be empty", nameof(key));

        if (value == null)
            return;

        try
        {
            var json = JsonSerializer.Serialize(value, _jsonOptions);
            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = expiration
            };

            await _cache.SetStringAsync(key, json, options, cancellationToken);
            _logger.LogDebug("Cached value for key: {Key} with expiration: {Expiration}", key, expiration);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting value in cache for key: {Key}", key);
        }
    }

    /// <summary>
    /// Removes a value from cache.
    /// </summary>
    public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(key))
            return;

        try
        {
            await _cache.RemoveAsync(key, cancellationToken);
            _logger.LogDebug("Removed cache entry for key: {Key}", key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing value from cache for key: {Key}", key);
        }
    }

    /// <summary>
    /// Generates a cache key from multiple parts.
    /// </summary>
    public static string GenerateKey(params string[] parts)
    {
        return string.Join(":", parts);
    }
}
