using MediatR;
using TrackFi.Domain.Interfaces;

namespace TrackFi.Application.UserWallets.Commands.VerifyUserWallet;

public class VerifyUserWalletCommandHandler : IRequestHandler<VerifyUserWalletCommand, Unit>
{
    private readonly IUserWalletRepository _walletRepository;

    public VerifyUserWalletCommandHandler(IUserWalletRepository walletRepository)
    {
        _walletRepository = walletRepository ?? throw new ArgumentNullException(nameof(walletRepository));
    }

    public async Task<Unit> Handle(VerifyUserWalletCommand request, CancellationToken cancellationToken)
    {
        // Get wallet
        var wallet = await _walletRepository.GetByIdAsync(request.WalletId, cancellationToken);
        if (wallet == null)
        {
            throw new InvalidOperationException($"Wallet with ID {request.WalletId} not found");
        }

        // Note: Actual signature validation would be done via a Web3 validator service
        // For now, we just mark as verified
        wallet.Verify(request.Signature, request.Message);

        // Save
        await _walletRepository.UpdateAsync(wallet, cancellationToken);

        return Unit.Value;
    }
}
