using FluentAssertions;
using TrackFi.Application.Portfolio.DTOs;

namespace TrackFi.Tests.Application.Portfolio;

public class MultiNetworkDtoTests
{
    [Fact]
    public void MultiNetworkWalletDto_DefaultsAreInitialized()
    {
        var dto = new MultiNetworkWalletDto();

        dto.IsAnonymous.Should().BeTrue();
        dto.WalletAddress.Should().BeEmpty();
        dto.Networks.Should().NotBeNull().And.BeEmpty();
        dto.Summary.Should().NotBeNull();
        dto.CacheExpiresAt.Should().BeNull();
    }

    [Fact]
    public void MultiNetworkNftDto_DefaultsAreInitialized()
    {
        var dto = new MultiNetworkNftDto();

        dto.IsAnonymous.Should().BeTrue();
        dto.WalletAddress.Should().BeEmpty();
        dto.Networks.Should().NotBeNull().And.BeEmpty();
        dto.Summary.Should().NotBeNull();
        dto.CacheExpiresAt.Should().BeNull();
    }

    [Fact]
    public void NetworkWalletDto_CanAggregateTokenBalances()
    {
        var network = new NetworkWalletDto
        {
            Network = "Ethereum",
            Tokens =
            [
                new TokenBalanceDto { Symbol = "ETH", BalanceFormatted = 1.5m },
                new TokenBalanceDto { Symbol = "USDC", BalanceFormatted = 250m }
            ],
            TotalValueUsd = 3500m,
            TokenCount = 2
        };

        network.Tokens.Should().HaveCount(network.TokenCount);
        network.Tokens.First(t => t.Symbol == "ETH").BalanceFormatted.Should().Be(1.5m);
    }

    [Fact]
    public void NetworkNftDto_TracksCounts()
    {
        var nfts = new NetworkNftDto
        {
            Network = "Polygon",
            Nfts =
            [
                new NftDto { Name = "Cool Cat" },
                new NftDto { Name = "TrackFi Pass" }
            ],
            NftCount = 2
        };

        nfts.Nfts.Should().HaveCount(nfts.NftCount);
        nfts.Nfts.Select(n => n.Name).Should().Contain("TrackFi Pass");
    }
}
