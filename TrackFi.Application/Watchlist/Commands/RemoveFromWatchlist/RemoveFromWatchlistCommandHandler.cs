using MediatR;
using TrackFi.Domain.Interfaces;

namespace TrackFi.Application.Watchlist.Commands.RemoveFromWatchlist;

public class RemoveFromWatchlistCommandHandler : IRequestHandler<RemoveFromWatchlistCommand, Unit>
{
    private readonly IWatchlistRepository _watchlistRepository;

    public RemoveFromWatchlistCommandHandler(IWatchlistRepository watchlistRepository)
    {
        _watchlistRepository = watchlistRepository ?? throw new ArgumentNullException(nameof(watchlistRepository));
    }

    public async Task<Unit> Handle(RemoveFromWatchlistCommand request, CancellationToken cancellationToken)
    {
        var entry = await _watchlistRepository.GetByIdAsync(request.EntryId, cancellationToken);
        if (entry == null)
        {
            throw new InvalidOperationException($"Watchlist entry {request.EntryId} not found");
        }

        // Verify ownership
        if (entry.UserId != request.UserId)
        {
            throw new UnauthorizedAccessException("Cannot remove another user's watchlist entry");
        }

        await _watchlistRepository.DeleteAsync(entry, cancellationToken);

        return Unit.Value;
    }
}
