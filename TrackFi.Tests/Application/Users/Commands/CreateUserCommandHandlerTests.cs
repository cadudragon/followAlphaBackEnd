using AutoMapper;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using TrackFi.Application.Common.Mappings;
using TrackFi.Application.Users.Commands.CreateUser;
using TrackFi.Domain.Entities;
using TrackFi.Domain.Enums;
using TrackFi.Domain.Interfaces;

namespace TrackFi.Tests.Application.Users.Commands;

public class CreateUserCommandHandlerTests
{
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly IMapper _mapper;
    private readonly CreateUserCommandHandler _handler;

    public CreateUserCommandHandlerTests()
    {
        _userRepositoryMock = new Mock<IUserRepository>();

        var configExpression = new MapperConfigurationExpression();
        configExpression.AddProfile<MappingProfile>();
        var loggerFactory = LoggerFactory.Create(builder => { });
        var config = new MapperConfiguration(configExpression, loggerFactory);
        _mapper = config.CreateMapper();

        _handler = new CreateUserCommandHandler(_userRepositoryMock.Object, _mapper);
    }

    [Fact]
    public async Task Handle_WithValidCommand_ShouldCreateUser()
    {
        // Arrange
        var command = new CreateUserCommand
        {
            WalletAddress = "0x1234567890abcdef",
            Network = "Ethereum"
        };

        _userRepositoryMock
            .Setup(x => x.GetByWalletAddressAsync(command.WalletAddress, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        _userRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.PrimaryWalletAddress.Should().Be(command.WalletAddress);
        result.PrimaryWalletNetwork.Should().Be("Ethereum");

        _userRepositoryMock.Verify(x => x.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenUserAlreadyExists_ShouldThrowException()
    {
        // Arrange
        var command = new CreateUserCommand
        {
            WalletAddress = "0x1234567890abcdef",
            Network = "Ethereum"
        };

        var existingUser = new User(command.WalletAddress, BlockchainNetwork.Ethereum);

        _userRepositoryMock
            .Setup(x => x.GetByWalletAddressAsync(command.WalletAddress, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingUser);

        // Act
        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*already exists*");

        _userRepositoryMock.Verify(x => x.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithInvalidNetwork_ShouldThrowException()
    {
        // Arrange
        var command = new CreateUserCommand
        {
            WalletAddress = "0x1234567890abcdef",
            Network = "InvalidNetwork"
        };

        _userRepositoryMock
            .Setup(x => x.GetByWalletAddressAsync(command.WalletAddress, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        // Act
        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*Invalid blockchain network*");
    }
}
