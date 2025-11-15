using TrackFi.Domain.Entities;
using TrackFi.Domain.Enums;

namespace TrackFi.Domain.Interfaces;

/// <summary>
/// Repository contract for WatchlistEntry entity persistence.
/// </summary>
public interface IWatchlistRepository
{
    Task<WatchlistEntry?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<List<WatchlistEntry>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<WatchlistEntry?> GetByWalletAddressAsync(Guid userId, string walletAddress, BlockchainNetwork network, CancellationToken cancellationToken = default);

    Task AddAsync(WatchlistEntry entry, CancellationToken cancellationToken = default);

    Task UpdateAsync(WatchlistEntry entry, CancellationToken cancellationToken = default);

    Task DeleteAsync(WatchlistEntry entry, CancellationToken cancellationToken = default);

    Task<bool> ExistsAsync(Guid userId, string walletAddress, BlockchainNetwork network, CancellationToken cancellationToken = default);

    Task<int> GetCountByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
}
