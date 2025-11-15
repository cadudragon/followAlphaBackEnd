using MediatR;
using TrackFi.Application.Common.DTOs;

namespace TrackFi.Application.Watchlist.Commands.AddToWatchlist;

public class AddToWatchlistCommand : IRequest<WatchlistEntryDto>
{
    public Guid UserId { get; set; }
    public string WalletAddress { get; set; } = string.Empty;
    public string Network { get; set; } = string.Empty;
    public string? Label { get; set; }
    public string? Notes { get; set; }
}
