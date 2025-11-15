using MediatR;

namespace TrackFi.Application.UserWallets.Commands.VerifyUserWallet;

/// <summary>
/// Command to verify a user's wallet via cryptographic signature.
/// </summary>
public class VerifyUserWalletCommand : IRequest<Unit>
{
    public Guid WalletId { get; set; }
    public string Signature { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}
