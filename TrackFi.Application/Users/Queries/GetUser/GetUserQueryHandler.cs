using AutoMapper;
using MediatR;
using TrackFi.Application.Common.DTOs;
using TrackFi.Domain.Interfaces;

namespace TrackFi.Application.Users.Queries.GetUser;

/// <summary>
/// Handler for GetUserQuery.
/// Retrieves a user by their ID.
/// </summary>
public class GetUserQueryHandler : IRequestHandler<GetUserQuery, UserDto?>
{
    private readonly IUserRepository _userRepository;
    private readonly IMapper _mapper;

    public GetUserQueryHandler(IUserRepository userRepository, IMapper mapper)
    {
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
    }

    public async Task<UserDto?> Handle(GetUserQuery request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);
        return user == null ? null : _mapper.Map<UserDto>(user);
    }
}
