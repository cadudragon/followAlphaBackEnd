using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TrackFi.Domain.Entities;
using TrackFi.Domain.Enums;
using TrackFi.Infrastructure.Blockchain;
using TrackFi.Infrastructure.Caching;

namespace TrackFi.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repository for TokenMetadata with layered caching (in-memory + Redis).
/// Provides fast access to token metadata to avoid repeated Alchemy API calls.
/// THREAD-SAFE: Uses IServiceScopeFactory to create separate DbContext instances for concurrent operations.
/// </summary>
public class TokenMetadataRepository
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly DistributedCacheService _cacheService;
    private readonly CacheOptions _cacheOptions;
    private readonly ILogger<TokenMetadataRepository> _logger;

    public TokenMetadataRepository(
        IServiceScopeFactory scopeFactory,
        DistributedCacheService cacheService,
        IOptions<CacheOptions> cacheOptions,
        ILogger<TokenMetadataRepository> logger)
    {
        _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
        _cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));
        _cacheOptions = cacheOptions?.Value ?? throw new ArgumentNullException(nameof(cacheOptions));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Gets token metadata by contract address and network.
    /// Checks cache first, then DB, returns null if not found.
    /// </summary>
    public async Task<TokenMetadata?> GetByAddressAsync(
        string contractAddress,
        BlockchainNetwork network,
        CancellationToken cancellationToken = default)
    {
        var lowerAddress = contractAddress.ToLowerInvariant();
        var cacheKey = DistributedCacheService.GenerateKey(
            "token_metadata",
            network.ToString(),
            lowerAddress);

        // Check cache first
        var cached = await _cacheService.GetAsync<TokenMetadata>(cacheKey, cancellationToken);
        if (cached != null)
        {
            _logger.LogDebug(
                "Token metadata cache HIT for {Symbol} ({Address}) on {Network}",
                cached.Symbol,
                lowerAddress,
                network);
            return cached;
        }

        // Cache miss - query database
        _logger.LogDebug(
            "Token metadata cache MISS for {Address} on {Network}, checking DB",
            lowerAddress,
            network);

        await using var scope = _scopeFactory.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<TrackFiDbContext>();

        var metadata = await context.Set<TokenMetadata>()
            .AsNoTracking()
            .FirstOrDefaultAsync(
                t => t.ContractAddress == lowerAddress && t.Network == network,
                cancellationToken);

        if (metadata != null)
        {
            // Cache using configured TTL (default: 30 days) - metadata rarely changes
            await _cacheService.SetAsync(
                cacheKey,
                metadata,
                _cacheOptions.TokenMetadataTtl,
                cancellationToken);

            _logger.LogInformation(
                "Token metadata found in DB and cached: {Symbol} ({Address}) on {Network}",
                metadata.Symbol,
                lowerAddress,
                network);
        }

        return metadata;
    }

    /// <summary>
    /// Gets metadata for multiple tokens in parallel.
    /// Returns dictionary keyed by lowercase contract address.
    /// </summary>
    public async Task<Dictionary<string, TokenMetadata>> GetByAddressesAsync(
        List<string> contractAddresses,
        BlockchainNetwork network,
        CancellationToken cancellationToken = default)
    {
        var result = new Dictionary<string, TokenMetadata>(StringComparer.OrdinalIgnoreCase);
        var lowerAddresses = contractAddresses.Select(a => a.ToLowerInvariant()).Distinct().ToList();

        // Try cache first for each address
        var uncachedAddresses = new List<string>();

        foreach (var address in lowerAddresses)
        {
            var metadata = await GetByAddressAsync(address, network, cancellationToken);
            if (metadata != null)
            {
                result[address] = metadata;
            }
            else
            {
                uncachedAddresses.Add(address);
            }
        }

        // If all were cached, return early
        if (uncachedAddresses.Count == 0)
        {
            _logger.LogInformation(
                "All {Count} token metadata requests served from cache on {Network}",
                lowerAddresses.Count,
                network);
            return result;
        }

        // Fetch uncached from DB in batch
        _logger.LogInformation(
            "Fetching {Count} uncached token metadata from DB on {Network}",
            uncachedAddresses.Count,
            network);

        await using var scope = _scopeFactory.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<TrackFiDbContext>();

        var dbResults = await context.Set<TokenMetadata>()
            .AsNoTracking()
            .Where(t => uncachedAddresses.Contains(t.ContractAddress) && t.Network == network)
            .ToListAsync(cancellationToken);

        // Cache DB results
        foreach (var metadata in dbResults)
        {
            result[metadata.ContractAddress] = metadata;

            var cacheKey = DistributedCacheService.GenerateKey(
                "token_metadata",
                network.ToString(),
                metadata.ContractAddress);

            await _cacheService.SetAsync(
                cacheKey,
                metadata,
                _cacheOptions.TokenMetadataTtl,
                cancellationToken);
        }

        _logger.LogInformation(
            "Token metadata batch fetch complete: {CacheHits} from cache, {DbHits} from DB, {Total} total on {Network}",
            lowerAddresses.Count - uncachedAddresses.Count,
            dbResults.Count,
            lowerAddresses.Count,
            network);

        return result;
    }

    /// <summary>
    /// Adds or updates token metadata.
    /// If token already exists, increments encounter count and optionally updates metadata.
    /// </summary>
    public async Task<TokenMetadata> AddOrUpdateAsync(
        TokenMetadata metadata,
        bool updateExisting = true,
        CancellationToken cancellationToken = default)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<TrackFiDbContext>();

        var existing = await context.Set<TokenMetadata>()
            .FirstOrDefaultAsync(
                t => t.ContractAddress == metadata.ContractAddress &&
                     t.Network == metadata.Network,
                cancellationToken);

        if (existing != null)
        {
            // Increment encounter count
            existing.IncrementEncounterCount();

            // Optionally update metadata (e.g., if logo URL changed)
            if (updateExisting)
            {
                existing.UpdateMetadata(
                    metadata.Symbol,
                    metadata.Name,
                    metadata.Decimals,
                    metadata.LogoUrl);
            }

            context.Set<TokenMetadata>().Update(existing);

            _logger.LogInformation(
                "Updated existing token metadata: {Symbol} ({Address}) on {Network}, encounter count: {Count}",
                existing.Symbol,
                existing.ContractAddress,
                existing.Network,
                existing.EncounterCount);

            metadata = existing;
        }
        else
        {
            context.Set<TokenMetadata>().Add(metadata);

            _logger.LogInformation(
                "Added new token metadata: {Symbol} ({Address}) on {Network}",
                metadata.Symbol,
                metadata.ContractAddress,
                metadata.Network);
        }

        await context.SaveChangesAsync(cancellationToken);

        // Invalidate cache to force refresh
        await InvalidateCacheAsync(metadata.ContractAddress, metadata.Network, cancellationToken);

        return metadata;
    }

    /// <summary>
    /// Invalidates cache for a specific token.
    /// </summary>
    public async Task InvalidateCacheAsync(
        string contractAddress,
        BlockchainNetwork network,
        CancellationToken cancellationToken = default)
    {
        var lowerAddress = contractAddress.ToLowerInvariant();
        var cacheKey = DistributedCacheService.GenerateKey(
            "token_metadata",
            network.ToString(),
            lowerAddress);

        await _cacheService.RemoveAsync(cacheKey, cancellationToken);

        _logger.LogDebug(
            "Invalidated token metadata cache for {Address} on {Network}",
            lowerAddress,
            network);
    }

    /// <summary>
    /// Gets most encountered tokens for analytics.
    /// </summary>
    public async Task<List<TokenMetadata>> GetMostPopularTokensAsync(
        BlockchainNetwork? network = null,
        int limit = 100,
        CancellationToken cancellationToken = default)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<TrackFiDbContext>();

        var query = context.Set<TokenMetadata>().AsNoTracking();

        if (network.HasValue)
        {
            query = query.Where(t => t.Network == network.Value);
        }

        return await query
            .OrderByDescending(t => t.EncounterCount)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }
}
