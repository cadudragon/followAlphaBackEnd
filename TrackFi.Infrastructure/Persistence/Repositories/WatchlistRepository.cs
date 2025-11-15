using Microsoft.EntityFrameworkCore;
using TrackFi.Domain.Entities;
using TrackFi.Domain.Enums;
using TrackFi.Domain.Interfaces;

namespace TrackFi.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repository implementation for WatchlistEntry entity using EF Core.
/// </summary>
public class WatchlistRepository : IWatchlistRepository
{
    private readonly TrackFiDbContext _context;

    public WatchlistRepository(TrackFiDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<WatchlistEntry?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Watchlist
            .Include(w => w.User)
            .FirstOrDefaultAsync(w => w.Id == id, cancellationToken);
    }

    public async Task<List<WatchlistEntry>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _context.Watchlist
            .Where(w => w.UserId == userId)
            .OrderByDescending(w => w.AddedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<WatchlistEntry?> GetByWalletAddressAsync(
        Guid userId,
        string walletAddress,
        BlockchainNetwork network,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(walletAddress))
            return null;

        return await _context.Watchlist
            .FirstOrDefaultAsync(w =>
                w.UserId == userId &&
                w.WalletAddress == walletAddress &&
                w.Network == network,
                cancellationToken);
    }

    public async Task AddAsync(WatchlistEntry entry, CancellationToken cancellationToken = default)
    {
        if (entry == null)
            throw new ArgumentNullException(nameof(entry));

        await _context.Watchlist.AddAsync(entry, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(WatchlistEntry entry, CancellationToken cancellationToken = default)
    {
        if (entry == null)
            throw new ArgumentNullException(nameof(entry));

        _context.Watchlist.Update(entry);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(WatchlistEntry entry, CancellationToken cancellationToken = default)
    {
        if (entry == null)
            throw new ArgumentNullException(nameof(entry));

        _context.Watchlist.Remove(entry);
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

        return await _context.Watchlist
            .AnyAsync(w =>
                w.UserId == userId &&
                w.WalletAddress == walletAddress &&
                w.Network == network,
                cancellationToken);
    }

    public async Task<int> GetCountByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _context.Watchlist
            .CountAsync(w => w.UserId == userId, cancellationToken);
    }
}
