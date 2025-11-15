using AutoMapper;
using MediatR;
using TrackFi.Application.Common.DTOs;
using TrackFi.Domain.Entities;
using TrackFi.Domain.Enums;
using TrackFi.Domain.Interfaces;

namespace TrackFi.Application.Watchlist.Commands.AddToWatchlist;

public class AddToWatchlistCommandHandler : IRequestHandler<AddToWatchlistCommand, WatchlistEntryDto>
{
    private readonly IWatchlistRepository _watchlistRepository;
    private readonly IMapper _mapper;

    public AddToWatchlistCommandHandler(IWatchlistRepository watchlistRepository, IMapper mapper)
    {
        _watchlistRepository = watchlistRepository ?? throw new ArgumentNullException(nameof(watchlistRepository));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
    }

    public async Task<WatchlistEntryDto> Handle(AddToWatchlistCommand request, CancellationToken cancellationToken)
    {
        // Parse network
        if (!Enum.TryParse<BlockchainNetwork>(request.Network, ignoreCase: true, out var network))
        {
            throw new ArgumentException($"Invalid blockchain network: {request.Network}");
        }

        // Check if already in watchlist
        var exists = await _watchlistRepository.ExistsAsync(request.UserId, request.WalletAddress, network, cancellationToken);
        if (exists)
        {
            throw new InvalidOperationException($"Wallet {request.WalletAddress} already in watchlist");
        }

        // Create entry
        var entry = new WatchlistEntry(request.UserId, request.WalletAddress, network, request.Label, request.Notes);

        // Save
        await _watchlistRepository.AddAsync(entry, cancellationToken);

        return _mapper.Map<WatchlistEntryDto>(entry);
    }
}
