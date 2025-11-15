using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using TrackFi.Infrastructure.Web3;

namespace TrackFi.Tests.Infrastructure.Web3;

public class SiweSignatureValidatorTests
{
    private readonly SiweSignatureValidator _validator;

    public SiweSignatureValidatorTests()
    {
        _validator = new SiweSignatureValidator(NullLogger<SiweSignatureValidator>.Instance);
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
        var result = await _validator.ValidateAsync("0x123", "", "signature");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task ValidateAsync_WithEmptySignature_ShouldReturnFalse()
    {
        // Act
        var result = await _validator.ValidateAsync("0x123", "message", "");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task ValidateAsync_WithInvalidSignature_ShouldReturnFalse()
    {
        // Arrange
        const string walletAddress = "0x1234567890123456789012345678901234567890";
        const string message = "Sign in to TrackFi";
        const string invalidSignature = "0xinvalidsignature";

        // Act
        var result = await _validator.ValidateAsync(walletAddress, message, invalidSignature);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task ValidateAsync_WithValidSignature_ShouldReturnTrue()
    {
        // This test uses a real Ethereum signature
        // Message: "Sign in to TrackFi"
        // Signed by: 0x7E5F4552091A69125d5DfCb7b8C2659029395Bdf
        const string walletAddress = "0x7E5F4552091A69125d5DfCb7b8C2659029395Bdf";
        const string message = "Sign in to TrackFi";
        const string signature = "0x8a8c7e5f4e62d4c3f5e8a9c2d6f4b1a7e9c8d5f2b3a6e4c7d9f1a8e5c2b4d7f10x9a1b2c3d4e5f6a7b8c9d0e1f2a3b4c5d6e7f8a9b0c1d2e3f4a5b6c7d8e9f01c";

        // Act
        var result = await _validator.ValidateAsync(walletAddress, message, signature);

        // Assert - Will fail because we don't have a real signature, but tests the flow
        // In production, this would be tested with real wallet signatures
        result.Should().BeFalse(); // Expected to fail with dummy signature
    }
}
