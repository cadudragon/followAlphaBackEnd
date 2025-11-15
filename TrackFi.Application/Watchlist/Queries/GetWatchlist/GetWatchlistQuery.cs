using MediatR;
using TrackFi.Application.Common.DTOs;

namespace TrackFi.Application.Watchlist.Queries.GetWatchlist;

public class GetWatchlistQuery : IRequest<List<WatchlistEntryDto>>
{
    public Guid UserId { get; set; }
}
