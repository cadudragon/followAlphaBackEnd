using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using TrackFi.Domain.Entities;
using TrackFi.Domain.Enums;
using TrackFi.Infrastructure.Persistence;
using TrackFi.Infrastructure.Persistence.Repositories;

namespace TrackFi.Tests.Infrastructure.Persistence;

public class UserRepositoryTests : IDisposable
{
    private readonly TrackFiDbContext _context;
    private readonly UserRepository _repository;

    public UserRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<TrackFiDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new TrackFiDbContext(options);
        _repository = new UserRepository(_context);
    }

    [Fact]
    public async Task AddAsync_ShouldAddUserToDatabase()
    {
        // Arrange
        var user = new User("0x1234567890abcdef", BlockchainNetwork.Ethereum);

        // Act
        await _repository.AddAsync(user);

        // Assert
        var savedUser = await _context.Users.FirstOrDefaultAsync(u => u.Id == user.Id);
        savedUser.Should().NotBeNull();
        savedUser!.PrimaryWalletAddress.Should().Be("0x1234567890abcdef");
    }

    [Fact]
    public async Task GetByIdAsync_WithExistingUser_ShouldReturnUser()
    {
        // Arrange
        var user = new User("0x123", BlockchainNetwork.Ethereum);
        await _repository.AddAsync(user);

        // Act
        var result = await _repository.GetByIdAsync(user.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(user.Id);
    }

    [Fact]
    public async Task GetByWalletAddressAsync_WithExistingUser_ShouldReturnUser()
    {
        // Arrange
        var user = new User("0xabcdef", BlockchainNetwork.Polygon);
        await _repository.AddAsync(user);

        // Act
        var result = await _repository.GetByWalletAddressAsync("0xabcdef");

        // Assert
        result.Should().NotBeNull();
        result!.PrimaryWalletAddress.Should().Be("0xabcdef");
        result.PrimaryWalletNetwork.Should().Be(BlockchainNetwork.Polygon);
    }

    [Fact]
    public async Task UpdateAsync_ShouldUpdateUserInDatabase()
    {
        // Arrange
        var user = new User("0x123", BlockchainNetwork.Ethereum);
        await _repository.AddAsync(user);

        // Act
        user.UpdateCoverPicture("https://example.com/cover.jpg", null, null, null);
        await _repository.UpdateAsync(user);

        // Assert
        var updatedUser = await _context.Users.FirstOrDefaultAsync(u => u.Id == user.Id);
        updatedUser!.CoverPictureUrl.Should().Be("https://example.com/cover.jpg");
    }

    [Fact]
    public async Task DeleteAsync_ShouldRemoveUserFromDatabase()
    {
        // Arrange
        var user = new User("0x123", BlockchainNetwork.Ethereum);
        await _repository.AddAsync(user);

        // Act
        await _repository.DeleteAsync(user);

        // Assert
        var deletedUser = await _context.Users.FirstOrDefaultAsync(u => u.Id == user.Id);
        deletedUser.Should().BeNull();
    }

    [Fact]
    public async Task ExistsAsync_WithExistingWallet_ShouldReturnTrue()
    {
        // Arrange
        var user = new User("0x123", BlockchainNetwork.Ethereum);
        await _repository.AddAsync(user);

        // Act
        var exists = await _repository.ExistsAsync("0x123");

        // Assert
        exists.Should().BeTrue();
    }

    [Fact]
    public async Task ExistsAsync_WithNonExistingWallet_ShouldReturnFalse()
    {
        // Act
        var exists = await _repository.ExistsAsync("0xnonexistent");

        // Assert
        exists.Should().BeFalse();
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
