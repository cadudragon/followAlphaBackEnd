using FluentAssertions;
using TrackFi.Domain.Entities;
using TrackFi.Domain.Enums;
using TrackFi.Domain.ValueObjects;

namespace TrackFi.Tests.Domain.Entities;

public class PortfolioTests
{
    [Fact]
    public void Create_ShouldCreatePortfolioWithBaseCurrency()
    {
        // Act
        var portfolio = Portfolio.Create(Currency.USD);

        // Assert
        portfolio.Id.Should().NotBeEmpty();
        portfolio.BaseCurrency.Should().Be(Currency.USD);
        portfolio.Accounts.Should().BeEmpty();
    }

    [Fact]
    public void AddAccount_ShouldAddAccountToPortfolio()
    {
        // Arrange
        var portfolio = Portfolio.Create(Currency.USD);
        var walletAddress = WalletAddress.Create("0x123", BlockchainNetwork.Ethereum);
        var wallet = Wallet.Create(walletAddress);

        // Act
        portfolio.AddAccount(wallet);

        // Assert
        portfolio.Accounts.Should().ContainSingle();
        portfolio.Accounts.First().Should().Be(wallet);
    }

    [Fact]
    public void CalculateTotalNetWorth_WithMultipleAccounts_ShouldSumValues()
    {
        // Arrange
        var portfolio = Portfolio.Create(Currency.USD);

        // Create wallet 1 with ETH
        var wallet1Address = WalletAddress.Create("0x123", BlockchainNetwork.Ethereum);
        var wallet1 = Wallet.Create(wallet1Address);
        var ethMetadata = AssetMetadata.Create("Ethereum", "ETH", decimals: 18);
        var ethToken = Token.CreateNative(BlockchainNetwork.Ethereum, ethMetadata, Quantity.Create(2m));
        var ethPrice = PriceInfo.Create(Money.Create(2000m, Currency.USD), DateTime.UtcNow, "Test");
        ethToken.UpdatePrice(ethPrice);
        wallet1.AddHolding(ethToken);

        // Create wallet 2 with USDC
        var wallet2Address = WalletAddress.Create("0xabc", BlockchainNetwork.Polygon);
        var wallet2 = Wallet.Create(wallet2Address);
        var usdcMetadata = AssetMetadata.Create("USD Coin", "USDC", decimals: 6);
        var usdcContract = ContractAddress.Create("0xusdcaddress", BlockchainNetwork.Polygon);
        var usdcToken = Token.CreateErc20(BlockchainNetwork.Polygon, usdcMetadata, usdcContract, Quantity.Create(1000m));
        var usdcPrice = PriceInfo.Create(Money.Create(1m, Currency.USD), DateTime.UtcNow, "Test");
        usdcToken.UpdatePrice(usdcPrice);
        wallet2.AddHolding(usdcToken);

        portfolio.AddAccount(wallet1);
        portfolio.AddAccount(wallet2);

        // Act
        var netWorth = portfolio.CalculateTotalNetWorth();

        // Assert
        netWorth.Amount.Should().Be(5000m); // 2 ETH * $2000 + 1000 USDC * $1
        netWorth.Currency.Should().Be(Currency.USD);
    }

    [Fact]
    public void GetAllTokens_ShouldReturnAllTokensFromAllAccounts()
    {
        // Arrange
        var portfolio = Portfolio.Create(Currency.USD);
        var wallet1 = Wallet.Create(WalletAddress.Create("0x123", BlockchainNetwork.Ethereum));
        var wallet2 = Wallet.Create(WalletAddress.Create("0xabc", BlockchainNetwork.Polygon));

        var token1 = Token.CreateNative(
            BlockchainNetwork.Ethereum,
            AssetMetadata.Create("ETH", "ETH"),
            Quantity.Create(1m));

        var token2 = Token.CreateNative(
            BlockchainNetwork.Polygon,
            AssetMetadata.Create("MATIC", "MATIC"),
            Quantity.Create(100m));

        wallet1.AddHolding(token1);
        wallet2.AddHolding(token2);
        portfolio.AddAccount(wallet1);
        portfolio.AddAccount(wallet2);

        // Act
        var allTokens = portfolio.GetAllTokens();

        // Assert
        allTokens.Should().HaveCount(2);
        allTokens.Should().Contain(token1);
        allTokens.Should().Contain(token2);
    }

    [Fact]
    public void CalculateAllocation_ShouldCreateAssetAllocation()
    {
        // Arrange
        var portfolio = Portfolio.Create(Currency.USD);
        var wallet = Wallet.Create(WalletAddress.Create("0x123", BlockchainNetwork.Ethereum));

        var token = Token.CreateNative(
            BlockchainNetwork.Ethereum,
            AssetMetadata.Create("ETH", "ETH"),
            Quantity.Create(1m));

        var price = PriceInfo.Create(Money.Create(2000m, Currency.USD), DateTime.UtcNow, "Test");
        token.UpdatePrice(price);
        wallet.AddHolding(token);
        portfolio.AddAccount(wallet);

        // Act
        var allocation = portfolio.CalculateAllocation();

        // Assert
        allocation.Should().NotBeNull();
        allocation.TotalValue.Amount.Should().Be(2000m);
        allocation.ByType.Should().ContainKey(AssetType.Token);
        allocation.ByNetwork.Should().ContainKey(BlockchainNetwork.Ethereum);
    }
}
