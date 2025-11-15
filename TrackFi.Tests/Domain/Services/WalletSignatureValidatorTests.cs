using FluentAssertions;
using TrackFi.Domain.Enums;
using TrackFi.Domain.Services;

namespace TrackFi.Tests.Domain.Services;

public class WalletSignatureValidatorTests
{
    private readonly WalletSignatureValidator _validator = new();

    [Fact]
    public void CreateSignatureMessage_ShouldCreateValidMessage()
    {
        // Arrange
        const string walletAddress = "0x1234567890abcdef";
        const string nonce = "abc123xyz";

        // Act
        var message = _validator.CreateSignatureMessage(walletAddress, BlockchainNetwork.Ethereum, nonce);

        // Assert
        message.Should().Contain("TrackFi");
        message.Should().Contain(walletAddress);
        message.Should().Contain("Ethereum");
        message.Should().Contain(nonce);
    }

    [Fact]
    public void CreateSignatureMessage_WithEmptyWalletAddress_ShouldThrowException()
    {
        // Act
        Action act = () => _validator.CreateSignatureMessage("", BlockchainNetwork.Ethereum, "nonce");

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void IsValidMessageFormat_WithValidMessage_ShouldReturnTrue()
    {
        // Arrange
        const string nonce = "abc123";
        const string message = "Sign in to TrackFi\nNonce: abc123\nWallet: 0x123";

        // Act
        var isValid = _validator.IsValidMessageFormat(message, nonce);

        // Assert
        isValid.Should().BeTrue();
    }

    [Fact]
    public void IsValidMessageFormat_WithoutTrackFi_ShouldReturnFalse()
    {
        // Arrange
        const string message = "Some random message\nNonce: abc123";

        // Act
        var isValid = _validator.IsValidMessageFormat(message, "abc123");

        // Assert
        isValid.Should().BeFalse();
    }

    [Fact]
    public void IsValidMessageFormat_WithoutNonce_ShouldReturnFalse()
    {
        // Arrange
        const string message = "Sign in to TrackFi";

        // Act
        var isValid = _validator.IsValidMessageFormat(message, "abc123");

        // Assert
        isValid.Should().BeFalse();
    }

    [Fact]
    public void IsNonceValid_WithRecentTimestamp_ShouldReturnTrue()
    {
        // Arrange
        var timestamp = DateTime.UtcNow.AddMinutes(-1);
        var maxAge = TimeSpan.FromMinutes(5);

        // Act
        var isValid = _validator.IsNonceValid(timestamp, maxAge);

        // Assert
        isValid.Should().BeTrue();
    }

    [Fact]
    public void IsNonceValid_WithExpiredTimestamp_ShouldReturnFalse()
    {
        // Arrange
        var timestamp = DateTime.UtcNow.AddMinutes(-10);
        var maxAge = TimeSpan.FromMinutes(5);

        // Act
        var isValid = _validator.IsNonceValid(timestamp, maxAge);

        // Assert
        isValid.Should().BeFalse();
    }

    [Fact]
    public void GenerateNonce_ShouldReturnNonEmptyString()
    {
        // Act
        var nonce = _validator.GenerateNonce();

        // Assert
        nonce.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void GenerateNonce_ShouldReturnDifferentValuesOnEachCall()
    {
        // Act
        var nonce1 = _validator.GenerateNonce();
        var nonce2 = _validator.GenerateNonce();

        // Assert
        nonce1.Should().NotBe(nonce2);
    }
}
