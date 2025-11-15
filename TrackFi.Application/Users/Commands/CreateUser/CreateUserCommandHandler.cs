using AutoMapper;
using MediatR;
using TrackFi.Application.Common.DTOs;
using TrackFi.Domain.Entities;
using TrackFi.Domain.Enums;
using TrackFi.Domain.Interfaces;

namespace TrackFi.Application.Users.Commands.CreateUser;

/// <summary>
/// Handler for CreateUserCommand.
/// Creates a new user with the provided wallet as primary wallet.
/// </summary>
public class CreateUserCommandHandler : IRequestHandler<CreateUserCommand, UserDto>
{
    private readonly IUserRepository _userRepository;
    private readonly IMapper _mapper;

    public CreateUserCommandHandler(IUserRepository userRepository, IMapper mapper)
    {
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
    }

    public async Task<UserDto> Handle(CreateUserCommand request, CancellationToken cancellationToken)
    {
        // Check if user already exists
        var existingUser = await _userRepository.GetByWalletAddressAsync(request.WalletAddress, cancellationToken);
        if (existingUser != null)
        {
            throw new InvalidOperationException($"User with wallet {request.WalletAddress} already exists");
        }

        // Parse network enum
        if (!Enum.TryParse<BlockchainNetwork>(request.Network, ignoreCase: true, out var network))
        {
            throw new ArgumentException($"Invalid blockchain network: {request.Network}");
        }

        // Create new user
        var user = new User(request.WalletAddress, network);

        // Save to database
        await _userRepository.AddAsync(user, cancellationToken);

        // Return DTO
        return _mapper.Map<UserDto>(user);
    }
}
