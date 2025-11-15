using System.Collections.Concurrent;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TrackFi.Domain.Entities;
using TrackFi.Domain.Enums;
using TrackFi.Domain.Interfaces;
using TrackFi.Infrastructure.Caching;

namespace TrackFi.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repository implementation for VerifiedToken with layered caching (in-memory + Redis).
/// Optimized for high-concurrency read scenarios such as multi-network portfolio aggregation.
/// </summary>
public class VerifiedTokenRepository : IVerifiedTokenRepository
{
    private const string CachePrefix = "verified_tokens";
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(15);

    private static readonly ConcurrentDictionary<BlockchainNetwork, CacheEntry> MemoryCache = new();
    private static readonly ConcurrentDictionary<BlockchainNetwork, SemaphoreSlim> Locks = new();

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly DistributedCacheService _cacheService;
    private readonly ILogger<VerifiedTokenRepository> _logger;
    private readonly bool _cacheEnabled;

    public VerifiedTokenRepository(
        IServiceScopeFactory scopeFactory,
        DistributedCacheService cacheService,
        IOptions<CacheOptions> cacheOptions,
        ILogger<VerifiedTokenRepository> logger)
    {
        _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
        _cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _cacheEnabled = cacheOptions?.Value?.Enabled ?? false;
    }

    public async Task<Dictionary<string, VerifiedTokenCacheEntry>> GetVerifiedTokensAsync(
        BlockchainNetwork network,
        CancellationToken cancellationToken = default)
    {
        if (MemoryCache.TryGetValue(network, out var cachedEntry) && !cachedEntry.IsExpired)
        {
            return cachedEntry.Value;
        }

        var semaphore = Locks.GetOrAdd(network, _ => new SemaphoreSlim(1, 1));
        await semaphore.WaitAsync(cancellationToken);

        try
        {
            if (MemoryCache.TryGetValue(network, out cachedEntry) && !cachedEntry.IsExpired)
            {
                return cachedEntry.Value;
            }

            List<VerifiedTokenCacheEntry>? cachedTokens = null;
            if (_cacheEnabled)
            {
                var cacheKey = DistributedCacheService.GenerateKey(CachePrefix, network.ToString().ToLowerInvariant());
                cachedTokens = await _cacheService.GetAsync<List<VerifiedTokenCacheEntry>>(cacheKey, cancellationToken);
            }

            if (_cacheEnabled && cachedTokens != null)
            {
                _logger.LogInformation("Loaded {Count} verified tokens for {Network} from distributed cache", cachedTokens.Count, network);
                var dict = cachedTokens.ToDictionary(t => t.ContractAddress.ToLowerInvariant(), t => t);
                MemoryCache[network] = new CacheEntry(dict, DateTimeOffset.UtcNow.Add(CacheDuration));
                return dict;
            }

            var tokensFromDb = await LoadVerifiedTokensFromDatabaseAsync(network, cancellationToken);

            if (_cacheEnabled)
            {
                var cacheKey = DistributedCacheService.GenerateKey(CachePrefix, network.ToString().ToLowerInvariant());
                await _cacheService.SetAsync(cacheKey, tokensFromDb, CacheDuration, cancellationToken);
            }

            var result = tokensFromDb.ToDictionary(t => t.ContractAddress.ToLowerInvariant(), t => t);
            MemoryCache[network] = new CacheEntry(result, DateTimeOffset.UtcNow.Add(CacheDuration));

            _logger.LogInformation("Loaded {Count} verified tokens for {Network} from database", result.Count, network);

            return result;
        }
        finally
        {
            semaphore.Release();
        }
    }

    public async Task<List<VerifiedToken>> GetAllAsync(
        BlockchainNetwork? network = null,
        CancellationToken cancellationToken = default)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<TrackFiDbContext>();

        var query = context.VerifiedTokens
            .AsNoTracking()
            .AsQueryable();

        if (network.HasValue)
        {
            query = query.Where(t => t.Network == network.Value);
        }

        return await query
            .OrderBy(t => t.Network)
            .ThenBy(t => t.Symbol)
            .ToListAsync(cancellationToken);
    }

    public async Task<VerifiedToken?> GetByAddressAsync(
        string contractAddress,
        BlockchainNetwork network,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(contractAddress))
            return null;

        await using var scope = _scopeFactory.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<TrackFiDbContext>();

        var normalized = contractAddress.ToLowerInvariant();

        return await context.VerifiedTokens
            .AsNoTracking()
            .FirstOrDefaultAsync(t =>
                t.ContractAddress == normalized &&
                t.Network == network,
                cancellationToken);
    }

    public async Task<VerifiedToken> AddAsync(
        VerifiedToken verifiedToken,
        CancellationToken cancellationToken = default)
    {
        if (verifiedToken == null)
            throw new ArgumentNullException(nameof(verifiedToken));

        await using var scope = _scopeFactory.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<TrackFiDbContext>();

        await context.VerifiedTokens.AddAsync(verifiedToken, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);

        await InvalidateCacheAsync(verifiedToken.Network, cancellationToken);

        return verifiedToken;
    }

    public async Task UpdateAsync(
        VerifiedToken verifiedToken,
        CancellationToken cancellationToken = default)
    {
        if (verifiedToken == null)
            throw new ArgumentNullException(nameof(verifiedToken));

        await using var scope = _scopeFactory.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<TrackFiDbContext>();

        context.VerifiedTokens.Update(verifiedToken);
        await context.SaveChangesAsync(cancellationToken);

        await InvalidateCacheAsync(verifiedToken.Network, cancellationToken);
    }

    public async Task<bool> RemoveAsync(
        string contractAddress,
        BlockchainNetwork network,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(contractAddress))
            return false;

        await using var scope = _scopeFactory.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<TrackFiDbContext>();

        var normalized = contractAddress.ToLowerInvariant();
        var token = await context.VerifiedTokens
            .FirstOrDefaultAsync(t =>
                t.ContractAddress == normalized &&
                t.Network == network,
                cancellationToken);

        if (token == null)
            return false;

        context.VerifiedTokens.Remove(token);
        await context.SaveChangesAsync(cancellationToken);

        await InvalidateCacheAsync(network, cancellationToken);

        return true;
    }

    public async Task<bool> IsVerifiedAsync(
        string contractAddress,
        BlockchainNetwork network,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(contractAddress))
            return false;

        var verifiedTokens = await GetVerifiedTokensAsync(network, cancellationToken);
        return verifiedTokens.ContainsKey(contractAddress.ToLowerInvariant());
    }

    private async Task<List<VerifiedTokenCacheEntry>> LoadVerifiedTokensFromDatabaseAsync(
        BlockchainNetwork network,
        CancellationToken cancellationToken)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<TrackFiDbContext>();

        return await context.VerifiedTokens
            .AsNoTracking()
            .Where(t => t.Network == network && t.Status == VerificationStatus.Verified)
            .Select(t => new VerifiedTokenCacheEntry(
                t.ContractAddress.ToLower(),
                t.Network,
                t.Symbol,
                t.Name,
                t.Decimals,
                t.LogoUrl,
                t.CoinGeckoId,
                t.Standard,
                t.WebsiteUrl,
                t.Description,
                t.IsNative))
            .ToListAsync(cancellationToken);
    }

    public async Task InvalidateCacheAsync(
        BlockchainNetwork network,
        CancellationToken cancellationToken = default)
    {
        MemoryCache.TryRemove(network, out _);

        if (_cacheEnabled)
        {
            var cacheKey = DistributedCacheService.GenerateKey(CachePrefix, network.ToString().ToLowerInvariant());
            await _cacheService.RemoveAsync(cacheKey, cancellationToken);
        }
    }

    private sealed record CacheEntry(Dictionary<string, VerifiedTokenCacheEntry> Value, DateTimeOffset ExpiresAt)
    {
        public bool IsExpired => DateTimeOffset.UtcNow >= ExpiresAt;
    }
}
