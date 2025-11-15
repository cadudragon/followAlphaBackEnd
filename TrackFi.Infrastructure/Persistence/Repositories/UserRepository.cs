using Microsoft.EntityFrameworkCore;
using TrackFi.Domain.Entities;
using TrackFi.Domain.Interfaces;

namespace TrackFi.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repository implementation for User entity using EF Core.
/// </summary>
public class UserRepository : IUserRepository
{
    private readonly TrackFiDbContext _context;

    public UserRepository(TrackFiDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Users
            .Include(u => u.Wallets)
            .Include(u => u.Watchlist)
            .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
    }

    public async Task<User?> GetByWalletAddressAsync(string walletAddress, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(walletAddress))
            return null;

        return await _context.Users
            .Include(u => u.Wallets)
            .Include(u => u.Watchlist)
            .FirstOrDefaultAsync(u => u.PrimaryWalletAddress == walletAddress, cancellationToken);
    }

    public async Task AddAsync(User user, CancellationToken cancellationToken = default)
    {
        if (user == null)
            throw new ArgumentNullException(nameof(user));

        await _context.Users.AddAsync(user, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(User user, CancellationToken cancellationToken = default)
    {
        if (user == null)
            throw new ArgumentNullException(nameof(user));

        _context.Users.Update(user);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(User user, CancellationToken cancellationToken = default)
    {
        if (user == null)
            throw new ArgumentNullException(nameof(user));

        _context.Users.Remove(user);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> ExistsAsync(string walletAddress, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(walletAddress))
            return false;

        return await _context.Users
            .AnyAsync(u => u.PrimaryWalletAddress == walletAddress, cancellationToken);
    }
}
