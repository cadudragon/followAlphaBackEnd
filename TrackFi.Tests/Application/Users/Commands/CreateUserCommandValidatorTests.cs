using FluentAssertions;
using FluentValidation.TestHelper;
using TrackFi.Application.Users.Commands.CreateUser;

namespace TrackFi.Tests.Application.Users.Commands;

public class CreateUserCommandValidatorTests
{
    private readonly CreateUserCommandValidator _validator;

    public CreateUserCommandValidatorTests()
    {
        _validator = new CreateUserCommandValidator();
    }

    [Fact]
    public void Validate_WithValidCommand_ShouldNotHaveErrors()
    {
        // Arrange
        var command = new CreateUserCommand
        {
            WalletAddress = "0x1234567890abcdef1234567890",
            Network = "Ethereum"
        };

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WithEmptyWalletAddress_ShouldHaveError()
    {
        // Arrange
        var command = new CreateUserCommand
        {
            WalletAddress = "",
            Network = "Ethereum"
        };

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.WalletAddress)
            .WithErrorMessage("Wallet address is required");
    }

    [Fact]
    public void Validate_WithShortWalletAddress_ShouldHaveError()
    {
        // Arrange
        var command = new CreateUserCommand
        {
            WalletAddress = "0x123",
            Network = "Ethereum"
        };

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.WalletAddress)
            .WithErrorMessage("Invalid wallet address format");
    }

    [Fact]
    public void Validate_WithInvalidNetwork_ShouldHaveError()
    {
        // Arrange
        var command = new CreateUserCommand
        {
            WalletAddress = "0x1234567890abcdef1234567890",
            Network = "InvalidNetwork"
        };

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Network)
            .WithErrorMessage("Invalid blockchain network");
    }

    [Theory]
    [InlineData("Ethereum")]
    [InlineData("Polygon")]
    [InlineData("Arbitrum")]
    [InlineData("Solana")]
    [InlineData("ethereum")] // Case insensitive
    public void Validate_WithValidNetworks_ShouldNotHaveError(string network)
    {
        // Arrange
        var command = new CreateUserCommand
        {
            WalletAddress = "0x1234567890abcdef1234567890",
            Network = network
        };

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Network);
    }
}
