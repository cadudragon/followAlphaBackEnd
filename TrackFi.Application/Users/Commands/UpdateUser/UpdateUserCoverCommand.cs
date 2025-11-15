using MediatR;

namespace TrackFi.Application.Users.Commands.UpdateUser;

/// <summary>
/// Command to update user's cover picture (URL or NFT).
/// </summary>
public class UpdateUserCoverCommand : IRequest<Unit>
{
    public Guid UserId { get; set; }
    public string? CoverPictureUrl { get; set; }
    public string? CoverNftContract { get; set; }
    public string? CoverNftTokenId { get; set; }
    public string? CoverNftNetwork { get; set; }
}
