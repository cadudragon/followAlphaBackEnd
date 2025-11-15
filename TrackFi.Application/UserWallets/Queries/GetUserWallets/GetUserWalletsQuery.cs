using MediatR;
using TrackFi.Application.Common.DTOs;

namespace TrackFi.Application.UserWallets.Queries.GetUserWallets;

/// <summary>
/// Query to get all wallets for a user.
/// </summary>
public class GetUserWalletsQuery : IRequest<List<UserWalletDto>>
{
    public Guid UserId { get; set; }
}
