using FluentValidation;

namespace TrackFi.Application.UserWallets.Commands.AddUserWallet;

public class AddUserWalletCommandValidator : AbstractValidator<AddUserWalletCommand>
{
    public AddUserWalletCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("User ID is required");

        RuleFor(x => x.WalletAddress)
            .NotEmpty().WithMessage("Wallet address is required")
            .MinimumLength(26).WithMessage("Invalid wallet address format");

        RuleFor(x => x.Network)
            .NotEmpty().WithMessage("Network is required")
            .Must(BeValidNetwork).WithMessage("Invalid blockchain network");

        RuleFor(x => x.Label)
            .MaximumLength(100).WithMessage("Label cannot exceed 100 characters");
    }

    private bool BeValidNetwork(string network)
    {
        var validNetworks = new[] { "Ethereum", "Polygon", "Arbitrum", "Solana" };
        return validNetworks.Contains(network, StringComparer.OrdinalIgnoreCase);
    }
}
