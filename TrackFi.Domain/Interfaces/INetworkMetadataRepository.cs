using TrackFi.Domain.Entities;
using TrackFi.Domain.Enums;

namespace TrackFi.Domain.Interfaces;

/// <summary>
/// Repository interface for accessing network metadata with memory caching.
/// </summary>
public interface INetworkMetadataRepository
{
    /// <summary>
    /// Gets all network metadata entries.
    /// Results are cached in memory for fast access.
    /// </summary>
    Task<Dictionary<BlockchainNetwork, NetworkMetadata>> GetAllAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets metadata for a specific network.
    /// </summary>
    Task<NetworkMetadata?> GetByNetworkAsync(
        BlockchainNetwork network,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Invalidates the memory cache.
    /// Call this after updating network metadata in the database.
    /// </summary>
    Task InvalidateCacheAsync(CancellationToken cancellationToken = default);
}
