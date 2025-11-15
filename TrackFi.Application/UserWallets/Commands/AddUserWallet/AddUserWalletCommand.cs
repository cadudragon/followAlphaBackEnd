using MediatR;
using TrackFi.Application.Common.DTOs;

namespace TrackFi.Application.UserWallets.Commands.AddUserWallet;

/// <summary>
/// Command to add a new wallet to a user's account.
/// Requires signature verification before marking as verified.
/// </summary>
public class AddUserWalletCommand : IRequest<UserWalletDto>
{
    public Guid UserId { get; set; }
    public string WalletAddress { get; set; } = string.Empty;
    public string Network { get; set; } = string.Empty;
    public string? Label { get; set; }
}
