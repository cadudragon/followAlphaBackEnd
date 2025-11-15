using MediatR;
using TrackFi.Application.Common.DTOs;

namespace TrackFi.Application.Users.Commands.CreateUser;

/// <summary>
/// Command to create a new user via Web3 wallet authentication.
/// </summary>
public class CreateUserCommand : IRequest<UserDto>
{
    public string WalletAddress { get; set; } = string.Empty;
    public string Network { get; set; } = string.Empty;
}
