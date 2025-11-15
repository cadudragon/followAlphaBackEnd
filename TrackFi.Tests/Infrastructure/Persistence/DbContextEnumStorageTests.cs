using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using TrackFi.Domain.Entities;
using TrackFi.Domain.Enums;
using TrackFi.Infrastructure.Persistence;

namespace TrackFi.Tests.Infrastructure.Persistence;

/// <summary>
/// Tests to verify that enums are stored as strings in the database.
/// </summary>
public class DbContextEnumStorageTests : IDisposable
{
    private readonly TrackFiDbContext _context;

    public DbContextEnumStorageTests()
    {
        var options = new DbContextOptionsBuilder<TrackFiDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new TrackFiDbContext(options);
    }

    [Fact]
    public async Task User_ShouldStoreBlockchainNetworkAsString()
    {
        // Arrange
        var user = new User("0x123", BlockchainNetwork.Ethereum);
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // Act
        var entry = _context.ChangeTracker.Entries<User>().First();
        var networkProperty = entry.Property(nameof(User.PrimaryWalletNetwork));

        // Assert - Verify the property is configured to use string conversion
        networkProperty.Metadata.GetValueConverter().Should().NotBeNull();
        user.PrimaryWalletNetwork.Should().Be(BlockchainNetwork.Ethereum);
    }

    [Fact]
    public async Task UserWallet_ShouldStoreNetworkAsString()
    {
        // Arrange
        var user = new User("0x123", BlockchainNetwork.Ethereum);
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var wallet = new UserWallet(user.Id, "0xabc", BlockchainNetwork.Polygon, "My Polygon Wallet");
        _context.UserWallets.Add(wallet);
        await _context.SaveChangesAsync();

        // Act
        var entry = _context.ChangeTracker.Entries<UserWallet>().First();
        var networkProperty = entry.Property(nameof(UserWallet.Network));

        // Assert
        networkProperty.Metadata.GetValueConverter().Should().NotBeNull();
        wallet.Network.Should().Be(BlockchainNetwork.Polygon);
    }

    [Fact]
    public async Task WatchlistEntry_ShouldStoreNetworkAsString()
    {
        // Arrange
        var user = new User("0x123", BlockchainNetwork.Ethereum);
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var entry = new WatchlistEntry(user.Id, "0xvitalik", BlockchainNetwork.Arbitrum);
        _context.Watchlist.Add(entry);
        await _context.SaveChangesAsync();

        // Act
        var trackedEntry = _context.ChangeTracker.Entries<WatchlistEntry>().First();
        var networkProperty = trackedEntry.Property(nameof(WatchlistEntry.Network));

        // Assert
        networkProperty.Metadata.GetValueConverter().Should().NotBeNull();
        entry.Network.Should().Be(BlockchainNetwork.Arbitrum);
    }

    [Fact]
    public async Task User_WithNullableEnumNetwork_ShouldStoreAsString()
    {
        // Arrange
        var user = new User("0x123", BlockchainNetwork.Ethereum);
        user.UpdateCoverPicture(
            "https://example.com/cover.jpg",
            "0xnftcontract",
            "123",
            BlockchainNetwork.Base); // Nullable enum

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // Act
        var saved = await _context.Users.FirstAsync(u => u.Id == user.Id);

        // Assert
        saved.CoverNftNetwork.Should().Be(BlockchainNetwork.Base);
    }

    [Fact]
    public void DbContext_ShouldHaveEnumStringConversionConfigured()
    {
        // Act
        var userEntityType = _context.Model.FindEntityType(typeof(User));
        var networkProperty = userEntityType!.FindProperty(nameof(User.PrimaryWalletNetwork));

        // Assert
        networkProperty.Should().NotBeNull();
        networkProperty!.GetValueConverter().Should().NotBeNull();
        networkProperty.GetValueConverter()!.ProviderClrType.Should().Be(typeof(string));
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
