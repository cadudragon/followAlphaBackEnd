using AutoMapper;
using MediatR;
using TrackFi.Application.Common.DTOs;
using TrackFi.Domain.Interfaces;

namespace TrackFi.Application.Watchlist.Queries.GetWatchlist;

public class GetWatchlistQueryHandler : IRequestHandler<GetWatchlistQuery, List<WatchlistEntryDto>>
{
    private readonly IWatchlistRepository _watchlistRepository;
    private readonly IMapper _mapper;

    public GetWatchlistQueryHandler(IWatchlistRepository watchlistRepository, IMapper mapper)
    {
        _watchlistRepository = watchlistRepository ?? throw new ArgumentNullException(nameof(watchlistRepository));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
    }

    public async Task<List<WatchlistEntryDto>> Handle(GetWatchlistQuery request, CancellationToken cancellationToken)
    {
        var entries = await _watchlistRepository.GetByUserIdAsync(request.UserId, cancellationToken);
        return _mapper.Map<List<WatchlistEntryDto>>(entries);
    }
}
