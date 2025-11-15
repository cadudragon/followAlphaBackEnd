using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using TrackFi.Infrastructure.Web3;

namespace TrackFi.Tests.Infrastructure.Web3;

public class SolanaSignatureValidatorTests
{
    private readonly SolanaSignatureValidator _validator;

    public SolanaSignatureValidatorTests()
    {
        _validator = new SolanaSignatureValidator(NullLogger<SolanaSignatureValidator>.Instance);
    }

    [Fact]
    public async Task ValidateAsync_WithEmptyWalletAddress_ShouldReturnFalse()
    {
        // Act
        var result = await _validator.ValidateAsync("", "message", "signature");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task ValidateAsync_WithEmptyMessage_ShouldReturnFalse()
    {
        // Act
        var result = await _validator.ValidateAsync("validaddress", "", "signature");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task ValidateAsync_WithEmptySignature_ShouldReturnFalse()
    {
        // Act
        var result = await _validator.ValidateAsync("validaddress", "message", "");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task ValidateAsync_WithInvalidPublicKey_ShouldReturnFalse()
    {
        // Arrange
        const string invalidAddress = "invalidpublickey";
        const string message = "Sign in to TrackFi";
        const string signature = "dGVzdHNpZ25hdHVyZQ==";

        // Act
        var result = await _validator.ValidateAsync(invalidAddress, message, signature);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task ValidateAsync_WithValidSolanaPublicKey_ShouldHandleGracefully()
    {
        // Arrange - Valid Solana public key format
        const string validAddress = "DYw8jCTfwHNRJhhmFcbXvVDTqWMEVFBX6ZKUmG5CNSKK";
        const string message = "Sign in to TrackFi";
        const string signature = "dGVzdHNpZ25hdHVyZQ=="; // Dummy base64 signature

        // Act
        var result = await _validator.ValidateAsync(validAddress, message, signature);

        // Assert - Will fail because signature is not valid, but tests the flow
        result.Should().BeFalse();
    }
}
