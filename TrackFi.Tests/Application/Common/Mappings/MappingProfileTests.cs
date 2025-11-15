using AutoMapper;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using TrackFi.Application.Common.DTOs;
using TrackFi.Application.Common.Mappings;
using TrackFi.Domain.Entities;
using TrackFi.Domain.Enums;

namespace TrackFi.Tests.Application.Common.Mappings;

public class MappingProfileTests
{
    private readonly IMapper _mapper;

    public MappingProfileTests()
    {
        var configExpression = new MapperConfigurationExpression();
        configExpression.AddProfile<MappingProfile>();
        var loggerFactory = LoggerFactory.Create(builder => { });
        var config = new MapperConfiguration(configExpression, loggerFactory);
        _mapper = config.CreateMapper();
    }

    [Fact]
    public void Configuration_ShouldBeValid()
    {
        // Act & Assert
        _mapper.ConfigurationProvider.AssertConfigurationIsValid();
    }

    [Fact]
    public void Map_UserToUserDto_ShouldMapCorrectly()
    {
        // Arrange
        var user = new User("0x123", BlockchainNetwork.Ethereum);

        // Act
        var dto = _mapper.Map<UserDto>(user);

        // Assert
        dto.Should().NotBeNull();
        dto.PrimaryWalletAddress.Should().Be("0x123");
        dto.PrimaryWalletNetwork.Should().Be("Ethereum"); // Enum converted to string
    }

    [Fact]
    public void Map_UserWalletToUserWalletDto_ShouldMapCorrectly()
    {
        // Arrange
        var wallet = new UserWallet(Guid.NewGuid(), "0xabc", BlockchainNetwork.Polygon, "My Wallet");

        // Act
        var dto = _mapper.Map<UserWalletDto>(wallet);

        // Assert
        dto.Should().NotBeNull();
        dto.WalletAddress.Should().Be("0xabc");
        dto.Network.Should().Be("Polygon"); // Enum converted to string
        dto.Label.Should().Be("My Wallet");
        dto.IsVerified.Should().BeFalse();
    }

    [Fact]
    public void Map_WatchlistEntryToWatchlistEntryDto_ShouldMapCorrectly()
    {
        // Arrange
        var entry = new WatchlistEntry(Guid.NewGuid(), "0xvitalik", BlockchainNetwork.Ethereum, "Vitalik", "Founder");

        // Act
        var dto = _mapper.Map<WatchlistEntryDto>(entry);

        // Assert
        dto.Should().NotBeNull();
        dto.WalletAddress.Should().Be("0xvitalik");
        dto.Network.Should().Be("Ethereum"); // Enum converted to string
        dto.Label.Should().Be("Vitalik");
        dto.Notes.Should().Be("Founder");
    }
}
