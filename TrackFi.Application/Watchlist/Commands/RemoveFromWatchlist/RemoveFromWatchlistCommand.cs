using MediatR;

namespace TrackFi.Application.Watchlist.Commands.RemoveFromWatchlist;

public class RemoveFromWatchlistCommand : IRequest<Unit>
{
    public Guid EntryId { get; set; }
    public Guid UserId { get; set; }
}
