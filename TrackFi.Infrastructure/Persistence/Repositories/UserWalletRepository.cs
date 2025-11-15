using Microsoft.EntityFrameworkCore;
using TrackFi.Domain.Entities;
using TrackFi.Domain.Enums;
using TrackFi.Domain.Interfaces;

namespace TrackFi.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repository implementation for UserWallet entity using EF Core.
/// </summary>
public class UserWalletRepository : IUserWalletRepository
{
    private readonly TrackFiDbContext _context;

    public UserWalletRepository(TrackFiDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<UserWallet?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.UserWallets
            .Include(w => w.User)
            .FirstOrDefaultAsync(w => w.Id == id, cancellationToken);
    }

    public async Task<List<UserWallet>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _context.UserWallets
            .Where(w => w.UserId == userId)
            .OrderByDescending(w => w.AddedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<UserWallet?> GetByWalletAddressAsync(
        Guid userId,
        string walletAddress,
        BlockchainNetwork network,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(walletAddress))
            return null;

        return await _context.UserWallets
            .FirstOrDefaultAsync(w =>
                w.UserId == userId &&
                w.WalletAddress == walletAddress &&
                w.Network == network,
                cancellationToken);
    }

    public async Task AddAsync(UserWallet wallet, CancellationToken cancellationToken = default)
    {
        if (wallet == null)
            throw new ArgumentNullException(nameof(wallet));

        await _context.UserWallets.AddAsync(wallet, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(UserWallet wallet, CancellationToken cancellationToken = default)
    {
        if (wallet == null)
            throw new ArgumentNullException(nameof(wallet));

        _context.UserWallets.Update(wallet);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(UserWallet wallet, CancellationToken cancellationToken = default)
    {
        if (wallet == null)
            throw new ArgumentNullException(nameof(wallet));

        _context.UserWallets.Remove(wallet);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> ExistsAsync(
        Guid userId,
        string walletAddress,
        BlockchainNetwork network,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(walletAddress))
            return false;

        return await _context.UserWallets
            .AnyAsync(w =>
                w.UserId == userId &&
                w.WalletAddress == walletAddress &&
                w.Network == network,
                cancellationToken);
    }
}
