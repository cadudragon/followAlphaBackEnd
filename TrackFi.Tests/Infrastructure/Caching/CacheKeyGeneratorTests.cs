using FluentAssertions;
using TrackFi.Infrastructure.Caching;

namespace TrackFi.Tests.Infrastructure.Caching;

public class CacheKeyGeneratorTests
{
    [Fact]
    public void ForPortfolio_WithWalletAddressOnly_ShouldGenerateCorrectKey()
    {
        // Act
        var key = CacheKeyGenerator.ForPortfolio("0x123");

        // Assert
        key.Should().Be("portfolio:0x123");
    }

    [Fact]
    public void ForPortfolio_WithAdditionalKey_ShouldGenerateCorrectKey()
    {
        // Act
        var key = CacheKeyGenerator.ForPortfolio("0x123", "hash");

        // Assert
        key.Should().Be("portfolio:0x123:hash");
    }

    [Fact]
    public void ForPrice_ShouldGenerateCorrectKey()
    {
        // Act
        var key = CacheKeyGenerator.ForPrice("ETH");

        // Assert
        key.Should().Be("price:ETH");
    }

    [Fact]
    public void ForNftMetadata_ShouldGenerateCorrectKey()
    {
        // Act
        var key = CacheKeyGenerator.ForNftMetadata("0xcontract", "123");

        // Assert
        key.Should().Be("nft:metadata:0xcontract:123");
    }

    [Fact]
    public void ForTokenMetadata_ShouldGenerateCorrectKey()
    {
        // Act
        var key = CacheKeyGenerator.ForTokenMetadata("0xcontract");

        // Assert
        key.Should().Be("token:metadata:0xcontract");
    }

    [Fact]
    public void ForRecentTransactions_ShouldGenerateCorrectKey()
    {
        // Act
        var key = CacheKeyGenerator.ForRecentTransactions("0xwallet");

        // Assert
        key.Should().Be("transactions:0xwallet:recent");
    }
}
