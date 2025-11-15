using AutoMapper;
using MediatR;
using TrackFi.Application.Common.DTOs;
using TrackFi.Domain.Interfaces;

namespace TrackFi.Application.Users.Queries.GetUserByWallet;

/// <summary>
/// Handler for GetUserByWalletQuery.
/// Retrieves a user by their primary wallet address.
/// </summary>
public class GetUserByWalletQueryHandler : IRequestHandler<GetUserByWalletQuery, UserDto?>
{
    private readonly IUserRepository _userRepository;
    private readonly IMapper _mapper;

    public GetUserByWalletQueryHandler(IUserRepository userRepository, IMapper mapper)
    {
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
    }

    public async Task<UserDto?> Handle(GetUserByWalletQuery request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByWalletAddressAsync(request.WalletAddress, cancellationToken);
        return user == null ? null : _mapper.Map<UserDto>(user);
    }
}
