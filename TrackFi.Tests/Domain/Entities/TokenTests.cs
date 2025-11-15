using FluentAssertions;
using TrackFi.Domain.Entities;
using TrackFi.Domain.Enums;
using TrackFi.Domain.ValueObjects;

namespace TrackFi.Tests.Domain.Entities;

public class TokenTests
{
    [Fact]
    public void CreateNative_ShouldCreateNativeToken()
    {
        // Arrange
        var metadata = AssetMetadata.Create("Ethereum", "ETH", decimals: 18);
        var balance = Quantity.Create(10.5m);

        // Act
        var token = Token.CreateNative(BlockchainNetwork.Ethereum, metadata, balance);

        // Assert
        token.Should().NotBeNull();
        token.IsNative.Should().BeTrue();
        token.Standard.Should().Be(TokenStandard.Native);
        token.Balance.Should().Be(balance);
        token.ContractAddress.Should().BeNull();
        token.Network.Should().Be(BlockchainNetwork.Ethereum);
    }

    [Fact]
    public void CreateErc20_ShouldCreateErc20Token()
    {
        // Arrange
        var metadata = AssetMetadata.Create("USD Coin", "USDC", decimals: 6);
        var contractAddress = ContractAddress.Create("0xa0b86991c6218b36c1d19d4a2e9eb0ce3606eb48", BlockchainNetwork.Ethereum);
        var balance = Quantity.Create(1000m);

        // Act
        var token = Token.CreateErc20(BlockchainNetwork.Ethereum, metadata, contractAddress, balance);

        // Assert
        token.Should().NotBeNull();
        token.IsNative.Should().BeFalse();
        token.Standard.Should().Be(TokenStandard.ERC20);
        token.ContractAddress.Should().Be(contractAddress);
        token.Balance.Should().Be(balance);
    }

    [Fact]
    public void UpdateBalance_ShouldUpdateBalance()
    {
        // Arrange
        var metadata = AssetMetadata.Create("Ethereum", "ETH", decimals: 18);
        var token = Token.CreateNative(BlockchainNetwork.Ethereum, metadata, Quantity.Create(10m));
        var newBalance = Quantity.Create(15m);

        // Act
        token.UpdateBalance(newBalance);

        // Assert
        token.Balance.Should().Be(newBalance);
    }

    [Fact]
    public void CalculateValue_WithPrice_ShouldCalculateCorrectly()
    {
        // Arrange
        var metadata = AssetMetadata.Create("Ethereum", "ETH", decimals: 18);
        var token = Token.CreateNative(BlockchainNetwork.Ethereum, metadata, Quantity.Create(2m));
        var price = Money.Create(2000m, Currency.USD);
        var priceInfo = PriceInfo.Create(price, DateTime.UtcNow, "CoinGecko");

        // Act
        token.UpdatePrice(priceInfo);
        var value = token.CalculateValue(Currency.USD);

        // Assert
        value.Amount.Should().Be(4000m); // 2 ETH * $2000
        value.Currency.Should().Be(Currency.USD);
    }

    [Fact]
    public void CalculateValue_WithoutPrice_ShouldReturnZero()
    {
        // Arrange
        var metadata = AssetMetadata.Create("Ethereum", "ETH", decimals: 18);
        var token = Token.CreateNative(BlockchainNetwork.Ethereum, metadata, Quantity.Create(2m));

        // Act
        var value = token.CalculateValue(Currency.USD);

        // Assert
        value.Amount.Should().Be(0);
    }
}
