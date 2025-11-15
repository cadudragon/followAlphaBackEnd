using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TrackFi.Domain.Entities;
using TrackFi.Domain.Enums;
using TrackFi.Domain.Interfaces;

namespace TrackFi.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repository implementation for NetworkMetadata with memory caching.
/// Network metadata is static reference data, so it's cached indefinitely in memory.
/// </summary>
public class NetworkMetadataRepository : INetworkMetadataRepository
{
    private static readonly SemaphoreSlim Lock = new(1, 1);
    private static CacheEntry? MemoryCache;

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<NetworkMetadataRepository> _logger;

    public NetworkMetadataRepository(
        IServiceScopeFactory scopeFactory,
        ILogger<NetworkMetadataRepository> logger)
    {
        _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Dictionary<BlockchainNetwork, NetworkMetadata>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        // Check cache first (no expiration for static reference data)
        if (MemoryCache != null)
        {
            return MemoryCache.Value;
        }

        await Lock.WaitAsync(cancellationToken);

        try
        {
            // Double-check after acquiring lock
            if (MemoryCache != null)
            {
                return MemoryCache.Value;
            }

            // Load from database
            await using var scope = _scopeFactory.CreateAsyncScope();
            var context = scope.ServiceProvider.GetRequiredService<TrackFiDbContext>();

            var networkMetadata = await context.NetworkMetadata
                .AsNoTracking()
                .ToDictionaryAsync(n => n.Network, n => n, cancellationToken);

            // Cache in memory
            // SonarQube S2696: Suppressed - Static field update is intentional and thread-safe.
            // This repository is registered as Singleton, and all updates are protected by SemaphoreSlim.
            // Static cache is shared across all instances for performance (reference data never changes).
#pragma warning disable S2696
            MemoryCache = new CacheEntry(networkMetadata);
#pragma warning restore S2696

            _logger.LogInformation("Loaded {Count} network metadata entries into memory cache", networkMetadata.Count);

            return networkMetadata;
        }
        finally
        {
            Lock.Release();
        }
    }

    public async Task<NetworkMetadata?> GetByNetworkAsync(
        BlockchainNetwork network,
        CancellationToken cancellationToken = default)
    {
        var allMetadata = await GetAllAsync(cancellationToken);
        return allMetadata.TryGetValue(network, out var metadata) ? metadata : null;
    }

    public async Task InvalidateCacheAsync(CancellationToken cancellationToken = default)
    {
        await Lock.WaitAsync(cancellationToken);

        try
        {
            // SonarQube S2696: Suppressed - Static field update is intentional and thread-safe.
#pragma warning disable S2696
            MemoryCache = null;
#pragma warning restore S2696
            _logger.LogInformation("Network metadata cache invalidated");
        }
        finally
        {
            Lock.Release();
        }
    }

    private sealed record CacheEntry(Dictionary<BlockchainNetwork, NetworkMetadata> Value);
}
