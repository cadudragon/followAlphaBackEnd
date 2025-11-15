using TrackFi.Domain.Entities;
using TrackFi.Domain.Enums;

namespace TrackFi.Domain.Interfaces;

/// <summary>
/// Repository for managing verified (whitelisted) tokens.
/// </summary>
public interface IVerifiedTokenRepository
{
    /// <summary>
    /// Get all verified token addresses for a specific network (for filtering).
    /// Returns a dictionary of lowercase address -> VerifiedToken for fast lookups.
    /// </summary>
    Task<Dictionary<string, VerifiedTokenCacheEntry>> GetVerifiedTokensAsync(BlockchainNetwork network, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all verified tokens (for admin management).
    /// </summary>
    Task<List<VerifiedToken>> GetAllAsync(BlockchainNetwork? network = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get a specific verified token by address and network.
    /// </summary>
    Task<VerifiedToken?> GetByAddressAsync(string contractAddress, BlockchainNetwork network, CancellationToken cancellationToken = default);

    /// <summary>
    /// Add a token to the verified list.
    /// </summary>
    Task<VerifiedToken> AddAsync(VerifiedToken verifiedToken, CancellationToken cancellationToken = default);

    /// <summary>
    /// Update a verified token.
    /// </summary>
    Task UpdateAsync(VerifiedToken verifiedToken, CancellationToken cancellationToken = default);

    /// <summary>
    /// Remove a token from the verified list.
    /// </summary>
    Task<bool> RemoveAsync(string contractAddress, BlockchainNetwork network, CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if a specific token is verified.
    /// </summary>
    Task<bool> IsVerifiedAsync(string contractAddress, BlockchainNetwork network, CancellationToken cancellationToken = default);

    /// <summary>
    /// Invalidate the cache for a specific network.
    /// </summary>
    Task InvalidateCacheAsync(BlockchainNetwork network, CancellationToken cancellationToken = default);
}
