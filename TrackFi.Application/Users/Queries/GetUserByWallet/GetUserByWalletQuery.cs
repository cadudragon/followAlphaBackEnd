using MediatR;
using TrackFi.Application.Common.DTOs;

namespace TrackFi.Application.Users.Queries.GetUserByWallet;

/// <summary>
/// Query to get a user by their wallet address.
/// Used for Web3 authentication lookups.
/// </summary>
public class GetUserByWalletQuery : IRequest<UserDto?>
{
    public string WalletAddress { get; set; } = string.Empty;
}
