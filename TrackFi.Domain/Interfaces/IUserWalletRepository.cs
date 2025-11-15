using TrackFi.Domain.Entities;
using TrackFi.Domain.Enums;

namespace TrackFi.Domain.Interfaces;

/// <summary>
/// Repository contract for UserWallet entity persistence.
/// </summary>
public interface IUserWalletRepository
{
    Task<UserWallet?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<List<UserWallet>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<UserWallet?> GetByWalletAddressAsync(Guid userId, string walletAddress, BlockchainNetwork network, CancellationToken cancellationToken = default);

    Task AddAsync(UserWallet wallet, CancellationToken cancellationToken = default);

    Task UpdateAsync(UserWallet wallet, CancellationToken cancellationToken = default);

    Task DeleteAsync(UserWallet wallet, CancellationToken cancellationToken = default);

    Task<bool> ExistsAsync(Guid userId, string walletAddress, BlockchainNetwork network, CancellationToken cancellationToken = default);
}
