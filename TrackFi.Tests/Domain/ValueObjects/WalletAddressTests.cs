using FluentAssertions;
using TrackFi.Domain.Enums;
using TrackFi.Domain.ValueObjects;

namespace TrackFi.Tests.Domain.ValueObjects;

public class WalletAddressTests
{
    [Fact]
    public void Create_WithValidAddress_ShouldCreateWalletAddress()
    {
        // Arrange
        const string address = "0x1234567890abcdef";

        // Act
        var walletAddress = WalletAddress.Create(address, BlockchainNetwork.Ethereum);

        // Assert
        walletAddress.Address.Should().Be(address);
        walletAddress.Network.Should().Be(BlockchainNetwork.Ethereum);
    }

    [Fact]
    public void Create_WithEmptyAddress_ShouldThrowException()
    {
        // Act
        Action act = () => WalletAddress.Create("", BlockchainNetwork.Ethereum);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Address cannot be empty*");
    }

    [Fact]
    public void Create_ShouldTrimAddress()
    {
        // Act
        var walletAddress = WalletAddress.Create("  0x123  ", BlockchainNetwork.Ethereum);

        // Assert
        walletAddress.Address.Should().Be("0x123");
    }

    [Fact]
    public void Equals_WithSameAddressDifferentCase_ShouldReturnTrue()
    {
        // Arrange
        var wallet1 = WalletAddress.Create("0xABCD", BlockchainNetwork.Ethereum);
        var wallet2 = WalletAddress.Create("0xabcd", BlockchainNetwork.Ethereum);

        // Act & Assert
        wallet1.Equals(wallet2).Should().BeTrue();
        (wallet1 == wallet2).Should().BeTrue();
    }

    [Fact]
    public void Equals_WithDifferentNetwork_ShouldReturnFalse()
    {
        // Arrange
        var wallet1 = WalletAddress.Create("0x123", BlockchainNetwork.Ethereum);
        var wallet2 = WalletAddress.Create("0x123", BlockchainNetwork.Polygon);

        // Act & Assert
        wallet1.Equals(wallet2).Should().BeFalse();
    }

    [Fact]
    public void ToString_ShouldIncludeAddressAndNetwork()
    {
        // Arrange
        var walletAddress = WalletAddress.Create("0x123", BlockchainNetwork.Ethereum);

        // Act
        var result = walletAddress.ToString();

        // Assert
        result.Should().Contain("0x123");
        result.Should().Contain("Ethereum");
    }
}
