using FluentValidation;

namespace TrackFi.Application.Users.Commands.CreateUser;

public class CreateUserCommandValidator : AbstractValidator<CreateUserCommand>
{
    public CreateUserCommandValidator()
    {
        RuleFor(x => x.WalletAddress)
            .NotEmpty().WithMessage("Wallet address is required")
            .MinimumLength(26).WithMessage("Invalid wallet address format");

        RuleFor(x => x.Network)
            .NotEmpty().WithMessage("Network is required")
            .Must(BeValidNetwork).WithMessage("Invalid blockchain network");
    }

    private bool BeValidNetwork(string network)
    {
        var validNetworks = new[] { "Ethereum", "Polygon", "Arbitrum", "Solana" };
        return validNetworks.Contains(network, StringComparer.OrdinalIgnoreCase);
    }
}
