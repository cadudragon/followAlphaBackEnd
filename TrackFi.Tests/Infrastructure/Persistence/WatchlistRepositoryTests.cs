using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using TrackFi.Domain.Entities;
using TrackFi.Domain.Enums;
using TrackFi.Infrastructure.Persistence;
using TrackFi.Infrastructure.Persistence.Repositories;

namespace TrackFi.Tests.Infrastructure.Persistence;

public class WatchlistRepositoryTests : IDisposable
{
    private readonly TrackFiDbContext _context;
    private readonly WatchlistRepository _repository;
    private readonly User _testUser;

    public WatchlistRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<TrackFiDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new TrackFiDbContext(options);
        _repository = new WatchlistRepository(_context);

        // Create test user
        _testUser = new User("0xowner", BlockchainNetwork.Ethereum);
        _context.Users.Add(_testUser);
        _context.SaveChanges();
    }

    [Fact]
    public async Task AddAsync_ShouldAddWatchlistEntry()
    {
        // Arrange
        var entry = new WatchlistEntry(
            _testUser.Id,
            "0xvitalik",
            BlockchainNetwork.Ethereum,
            "Vitalik Buterin",
            "Ethereum founder");

        // Act
        await _repository.AddAsync(entry);

        // Assert
        var saved = await _context.Watchlist.FirstOrDefaultAsync(w => w.Id == entry.Id);
        saved.Should().NotBeNull();
        saved!.Label.Should().Be("Vitalik Buterin");
    }

    [Fact]
    public async Task GetByUserIdAsync_ShouldReturnUserWatchlist()
    {
        // Arrange
        var entry1 = new WatchlistEntry(_testUser.Id, "0xwallet1", BlockchainNetwork.Ethereum, "Wallet 1");
        var entry2 = new WatchlistEntry(_testUser.Id, "0xwallet2", BlockchainNetwork.Polygon, "Wallet 2");
        await _repository.AddAsync(entry1);
        await _repository.AddAsync(entry2);

        // Act
        var watchlist = await _repository.GetByUserIdAsync(_testUser.Id);

        // Assert
        watchlist.Should().HaveCount(2);
        watchlist.Should().Contain(w => w.WalletAddress == "0xwallet1");
        watchlist.Should().Contain(w => w.WalletAddress == "0xwallet2");
    }

    [Fact]
    public async Task GetByWalletAddressAsync_WithExistingEntry_ShouldReturnEntry()
    {
        // Arrange
        var entry = new WatchlistEntry(_testUser.Id, "0xspecific", BlockchainNetwork.Arbitrum);
        await _repository.AddAsync(entry);

        // Act
        var result = await _repository.GetByWalletAddressAsync(
            _testUser.Id,
            "0xspecific",
            BlockchainNetwork.Arbitrum);

        // Assert
        result.Should().NotBeNull();
        result!.WalletAddress.Should().Be("0xspecific");
        result.Network.Should().Be(BlockchainNetwork.Arbitrum);
    }

    [Fact]
    public async Task UpdateAsync_ShouldUpdateEntry()
    {
        // Arrange
        var entry = new WatchlistEntry(_testUser.Id, "0xwallet", BlockchainNetwork.Ethereum);
        await _repository.AddAsync(entry);

        // Act
        entry.Update("Updated Label", "Updated notes");
        await _repository.UpdateAsync(entry);

        // Assert
        var updated = await _context.Watchlist.FirstOrDefaultAsync(w => w.Id == entry.Id);
        updated!.Label.Should().Be("Updated Label");
        updated.Notes.Should().Be("Updated notes");
    }

    [Fact]
    public async Task DeleteAsync_ShouldRemoveEntry()
    {
        // Arrange
        var entry = new WatchlistEntry(_testUser.Id, "0xwallet", BlockchainNetwork.Ethereum);
        await _repository.AddAsync(entry);

        // Act
        await _repository.DeleteAsync(entry);

        // Assert
        var deleted = await _context.Watchlist.FirstOrDefaultAsync(w => w.Id == entry.Id);
        deleted.Should().BeNull();
    }

    [Fact]
    public async Task ExistsAsync_WithExistingEntry_ShouldReturnTrue()
    {
        // Arrange
        var entry = new WatchlistEntry(_testUser.Id, "0xwallet", BlockchainNetwork.Ethereum);
        await _repository.AddAsync(entry);

        // Act
        var exists = await _repository.ExistsAsync(_testUser.Id, "0xwallet", BlockchainNetwork.Ethereum);

        // Assert
        exists.Should().BeTrue();
    }

    [Fact]
    public async Task GetCountByUserIdAsync_ShouldReturnCorrectCount()
    {
        // Arrange
        await _repository.AddAsync(new WatchlistEntry(_testUser.Id, "0xwallet1", BlockchainNetwork.Ethereum));
        await _repository.AddAsync(new WatchlistEntry(_testUser.Id, "0xwallet2", BlockchainNetwork.Polygon));
        await _repository.AddAsync(new WatchlistEntry(_testUser.Id, "0xwallet3", BlockchainNetwork.Arbitrum));

        // Act
        var count = await _repository.GetCountByUserIdAsync(_testUser.Id);

        // Assert
        count.Should().Be(3);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
