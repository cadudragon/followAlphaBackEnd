using AutoMapper;
using MediatR;
using TrackFi.Application.Common.DTOs;
using TrackFi.Domain.Entities;
using TrackFi.Domain.Enums;
using TrackFi.Domain.Interfaces;

namespace TrackFi.Application.UserWallets.Commands.AddUserWallet;

public class AddUserWalletCommandHandler : IRequestHandler<AddUserWalletCommand, UserWalletDto>
{
    private readonly IUserWalletRepository _walletRepository;
    private readonly IUserRepository _userRepository;
    private readonly IMapper _mapper;

    public AddUserWalletCommandHandler(
        IUserWalletRepository walletRepository,
        IUserRepository userRepository,
        IMapper mapper)
    {
        _walletRepository = walletRepository ?? throw new ArgumentNullException(nameof(walletRepository));
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
    }

    public async Task<UserWalletDto> Handle(AddUserWalletCommand request, CancellationToken cancellationToken)
    {
        // Verify user exists
        var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);
        if (user == null)
        {
            throw new InvalidOperationException($"User with ID {request.UserId} not found");
        }

        // Parse network
        if (!Enum.TryParse<BlockchainNetwork>(request.Network, ignoreCase: true, out var network))
        {
            throw new ArgumentException($"Invalid blockchain network: {request.Network}");
        }

        // Check if wallet already exists for this user
        var exists = await _walletRepository.ExistsAsync(request.UserId, request.WalletAddress, network, cancellationToken);
        if (exists)
        {
            throw new InvalidOperationException($"Wallet {request.WalletAddress} already added for this user");
        }

        // Create wallet (unverified initially)
        var wallet = new UserWallet(request.UserId, request.WalletAddress, network, request.Label);

        // Save
        await _walletRepository.AddAsync(wallet, cancellationToken);

        return _mapper.Map<UserWalletDto>(wallet);
    }
}
