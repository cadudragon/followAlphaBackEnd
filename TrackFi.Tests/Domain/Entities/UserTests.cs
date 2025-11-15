using FluentAssertions;
using TrackFi.Domain.Entities;
using TrackFi.Domain.Enums;

namespace TrackFi.Tests.Domain.Entities;

public class UserTests
{
    [Fact]
    public void Constructor_WithValidData_ShouldCreateUser()
    {
        // Arrange
        const string walletAddress = "0x1234567890abcdef";

        // Act
        var user = new User(walletAddress, BlockchainNetwork.Ethereum);

        // Assert
        user.Id.Should().NotBeEmpty();
        user.PrimaryWalletAddress.Should().Be(walletAddress);
        user.PrimaryWalletNetwork.Should().Be(BlockchainNetwork.Ethereum);
        user.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        user.LastActiveAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void Constructor_WithEmptyWalletAddress_ShouldThrowException()
    {
        // Act
        Action act = () => new User("", BlockchainNetwork.Ethereum);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Primary wallet address cannot be empty*");
    }

    [Fact]
    public void UpdateCoverPicture_ShouldUpdateProperties()
    {
        // Arrange
        var user = new User("0x123", BlockchainNetwork.Ethereum);
        const string pictureUrl = "https://example.com/cover.jpg";
        const string nftContract = "0xabc";
        const string nftTokenId = "123";

        // Act
        user.UpdateCoverPicture(pictureUrl, nftContract, nftTokenId, BlockchainNetwork.Ethereum);

        // Assert
        user.CoverPictureUrl.Should().Be(pictureUrl);
        user.CoverNftContract.Should().Be(nftContract);
        user.CoverNftTokenId.Should().Be(nftTokenId);
        user.CoverNftNetwork.Should().Be(BlockchainNetwork.Ethereum);
    }

    [Fact]
    public void AddWallet_WithValidWallet_ShouldAddToCollection()
    {
        // Arrange
        var user = new User("0x123", BlockchainNetwork.Ethereum);
        var wallet = new UserWallet(user.Id, "0xabc", BlockchainNetwork.Polygon, "My Polygon Wallet");

        // Act
        user.AddWallet(wallet);

        // Assert
        user.Wallets.Should().ContainSingle();
        user.Wallets.First().Should().Be(wallet);
    }

    [Fact]
    public void AddToWatchlist_WithValidEntry_ShouldAddToCollection()
    {
        // Arrange
        var user = new User("0x123", BlockchainNetwork.Ethereum);
        var entry = new WatchlistEntry(user.Id, "0xvitalik", BlockchainNetwork.Ethereum, "Vitalik");

        // Act
        user.AddToWatchlist(entry);

        // Assert
        user.Watchlist.Should().ContainSingle();
        user.Watchlist.First().Should().Be(entry);
    }

    [Fact]
    public void RemoveFromWatchlist_ShouldRemoveEntry()
    {
        // Arrange
        var user = new User("0x123", BlockchainNetwork.Ethereum);
        var entry = new WatchlistEntry(user.Id, "0xvitalik", BlockchainNetwork.Ethereum, "Vitalik");
        user.AddToWatchlist(entry);

        // Act
        user.RemoveFromWatchlist(entry);

        // Assert
        user.Watchlist.Should().BeEmpty();
    }

    [Fact]
    public void UpdateLastActive_ShouldUpdateTimestamp()
    {
        // Arrange
        var user = new User("0x123", BlockchainNetwork.Ethereum);
        var originalTime = user.LastActiveAt;
        Thread.Sleep(10);

        // Act
        user.UpdateLastActive();

        // Assert
        user.LastActiveAt.Should().BeAfter(originalTime);
    }
}
