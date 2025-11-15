using AutoMapper;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using TrackFi.Application.Common.Mappings;
using TrackFi.Application.Users.Queries.GetUser;
using TrackFi.Domain.Entities;
using TrackFi.Domain.Enums;
using TrackFi.Domain.Interfaces;

namespace TrackFi.Tests.Application.Users.Queries;

public class GetUserQueryHandlerTests
{
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly IMapper _mapper;
    private readonly GetUserQueryHandler _handler;

    public GetUserQueryHandlerTests()
    {
        _userRepositoryMock = new Mock<IUserRepository>();

        var configExpression = new MapperConfigurationExpression();
        configExpression.AddProfile<MappingProfile>();
        var loggerFactory = LoggerFactory.Create(builder => { });
        var config = new MapperConfiguration(configExpression, loggerFactory);
        _mapper = config.CreateMapper();

        _handler = new GetUserQueryHandler(_userRepositoryMock.Object, _mapper);
    }

    [Fact]
    public async Task Handle_WithExistingUser_ShouldReturnUserDto()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User("0x123", BlockchainNetwork.Ethereum);

        _userRepositoryMock
            .Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var query = new GetUserQuery { UserId = userId };

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.PrimaryWalletAddress.Should().Be("0x123");
        result.PrimaryWalletNetwork.Should().Be("Ethereum");
    }

    [Fact]
    public async Task Handle_WithNonExistingUser_ShouldReturnNull()
    {
        // Arrange
        var userId = Guid.NewGuid();

        _userRepositoryMock
            .Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        var query = new GetUserQuery { UserId = userId };

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }
}
