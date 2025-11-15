using System.Collections.Concurrent;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TrackFi.Domain.Entities;
using TrackFi.Domain.Enums;
using TrackFi.Infrastructure.Caching;

namespace TrackFi.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repository for UnlistedToken with layered caching (in-memory + Redis).
/// Caches tokens that were checked but not found in CoinMarketCap to avoid repeated API calls.
/// </summary>
public class UnlistedTokenRepository
{
    private const string CachePrefix = "unlisted_tokens";
    private static readonly TimeSpan CacheDuration = TimeSpan.FromHours(24); // Cache for 24 hours

    private static readonly ConcurrentDictionary<BlockchainNetwork, CacheEntry> MemoryCache = new();
    private static readonly ConcurrentDictionary<BlockchainNetwork, SemaphoreSlim> Locks = new();

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly DistributedCacheService _cacheService;
    private readonly ILogger<UnlistedTokenRepository> _logger;
    private readonly bool _cacheEnabled;

    public UnlistedTokenRepository(
        IServiceScopeFactory scopeFactory,
        DistributedCacheService cacheService,
        IOptions<CacheOptions> cacheOptions,
        ILogger<UnlistedTokenRepository> logger)
    {
        _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
        _cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _cacheEnabled = cacheOptions?.Value?.Enabled ?? false;
    }

    /// <summary>
    /// Gets all unlisted tokens for a network from cache or database.
    /// </summary>
    public async Task<Dictionary<string, UnlistedToken>> GetUnlistedTokensAsync(
        BlockchainNetwork network,
        CancellationToken cancellationToken = default)
    {
        // Check in-memory cache first
        if (MemoryCache.TryGetValue(network, out var cachedEntry) && !cachedEntry.IsExpired)
        {
            return cachedEntry.Value;
        }

        var lockObj = Locks.GetOrAdd(network, _ => new SemaphoreSlim(1, 1));
        await lockObj.WaitAsync(cancellationToken);

        try
        {
            // Double-check after acquiring lock
            if (MemoryCache.TryGetValue(network, out cachedEntry) && !cachedEntry.IsExpired)
            {
                return cachedEntry.Value;
            }

            // Try Redis cache
            if (_cacheEnabled)
            {
                var cacheKey = $"{CachePrefix}:{network}";
                var cachedData = await _cacheService.GetAsync<Dictionary<string, UnlistedToken>>(
                    cacheKey,
                    cancellationToken);

                if (cachedData != null)
                {
                    MemoryCache[network] = new CacheEntry(cachedData);
                    return cachedData;
                }
            }

            // Load from database
            await using var scope = _scopeFactory.CreateAsyncScope();
            var context = scope.ServiceProvider.GetRequiredService<TrackFiDbContext>();

            var tokens = await context.UnlistedTokens
                .Where(t => t.Network == network)
                .AsNoTracking()
                .ToListAsync(cancellationToken);

            var tokenDict = tokens.ToDictionary(
                t => t.ContractAddress.ToLowerInvariant(),
                StringComparer.OrdinalIgnoreCase);

            // Cache the result
            MemoryCache[network] = new CacheEntry(tokenDict);

            if (_cacheEnabled)
            {
                var cacheKey = $"{CachePrefix}:{network}";
                await _cacheService.SetAsync(cacheKey, tokenDict, CacheDuration, cancellationToken);
            }

            _logger.LogInformation(
                "Loaded {Count} unlisted tokens for {Network} from database",
                tokenDict.Count,
                network);

            return tokenDict;
        }
        finally
        {
            lockObj.Release();
        }
    }

    /// <summary>
    /// Checks if a token is in the unlisted cache.
    /// </summary>
    public async Task<bool> IsTokenUnlistedAsync(
        string contractAddress,
        BlockchainNetwork network,
        CancellationToken cancellationToken = default)
    {
        var unlistedTokens = await GetUnlistedTokensAsync(network, cancellationToken);
        return unlistedTokens.ContainsKey(contractAddress.ToLowerInvariant());
    }

    /// <summary>
    /// Adds a token to the unlisted cache.
    /// </summary>
    public async Task AddUnlistedTokenAsync(
        UnlistedToken token,
        CancellationToken cancellationToken = default)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<TrackFiDbContext>();

        // Check if already exists
        var existing = await context.UnlistedTokens
            .FirstOrDefaultAsync(
                t => t.ContractAddress == token.ContractAddress && t.Network == token.Network,
                cancellationToken);

        if (existing != null)
        {
            existing.RecordCheck();
            context.UnlistedTokens.Update(existing);
        }
        else
        {
            await context.UnlistedTokens.AddAsync(token, cancellationToken);
        }

        await context.SaveChangesAsync(cancellationToken);

        // Invalidate cache
        await InvalidateCacheAsync(token.Network);

        _logger.LogInformation(
            "Added unlisted token {Symbol} ({Address}) on {Network}",
            token.Symbol ?? "Unknown",
            token.ContractAddress,
            token.Network);
    }

    /// <summary>
    /// Invalidates the cache for a specific network.
    /// </summary>
    public async Task InvalidateCacheAsync(BlockchainNetwork network)
    {
        MemoryCache.TryRemove(network, out _);

        if (_cacheEnabled)
        {
            var cacheKey = $"{CachePrefix}:{network}";
            await _cacheService.RemoveAsync(cacheKey);
        }
    }

    private class CacheEntry
    {
        public Dictionary<string, UnlistedToken> Value { get; }
        public DateTime ExpiresAt { get; }

        public CacheEntry(Dictionary<string, UnlistedToken> value)
        {
            Value = value;
            ExpiresAt = DateTime.UtcNow.Add(CacheDuration);
        }

        public bool IsExpired => DateTime.UtcNow >= ExpiresAt;
    }
}
