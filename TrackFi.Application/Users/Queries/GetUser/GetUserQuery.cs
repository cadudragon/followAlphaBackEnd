using MediatR;
using TrackFi.Application.Common.DTOs;

namespace TrackFi.Application.Users.Queries.GetUser;

/// <summary>
/// Query to get a user by ID.
/// </summary>
public class GetUserQuery : IRequest<UserDto?>
{
    public Guid UserId { get; set; }
}
