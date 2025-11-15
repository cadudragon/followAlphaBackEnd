using TrackFi.Domain.Entities;

namespace TrackFi.Domain.Interfaces;

/// <summary>
/// Repository contract for User entity persistence.
/// </summary>
public interface IUserRepository
{
    Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<User?> GetByWalletAddressAsync(string walletAddress, CancellationToken cancellationToken = default);

    Task AddAsync(User user, CancellationToken cancellationToken = default);

    Task UpdateAsync(User user, CancellationToken cancellationToken = default);

    Task DeleteAsync(User user, CancellationToken cancellationToken = default);

    Task<bool> ExistsAsync(string walletAddress, CancellationToken cancellationToken = default);
}
