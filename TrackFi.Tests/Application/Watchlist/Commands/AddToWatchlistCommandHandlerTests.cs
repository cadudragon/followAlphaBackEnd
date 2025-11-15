using AutoMapper;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using TrackFi.Application.Common.Mappings;
using TrackFi.Application.Watchlist.Commands.AddToWatchlist;
using TrackFi.Domain.Entities;
using TrackFi.Domain.Enums;
using TrackFi.Domain.Interfaces;

namespace TrackFi.Tests.Application.Watchlist.Commands;

public class AddToWatchlistCommandHandlerTests
{
    private readonly Mock<IWatchlistRepository> _watchlistRepositoryMock;
    private readonly IMapper _mapper;
    private readonly AddToWatchlistCommandHandler _handler;

    public AddToWatchlistCommandHandlerTests()
    {
        _watchlistRepositoryMock = new Mock<IWatchlistRepository>();

        var configExpression = new MapperConfigurationExpression();
        configExpression.AddProfile<MappingProfile>();
        var loggerFactory = LoggerFactory.Create(builder => { });
        var config = new MapperConfiguration(configExpression, loggerFactory);
        _mapper = config.CreateMapper();

        _handler = new AddToWatchlistCommandHandler(_watchlistRepositoryMock.Object, _mapper);
    }

    [Fact]
    public async Task Handle_WithValidCommand_ShouldAddToWatchlist()
    {
        // Arrange
        var command = new AddToWatchlistCommand
        {
            UserId = Guid.NewGuid(),
            WalletAddress = "0xvitalik",
            Network = "Ethereum",
            Label = "Vitalik",
            Notes = "Ethereum founder"
        };

        _watchlistRepositoryMock
            .Setup(x => x.ExistsAsync(command.UserId, command.WalletAddress, It.IsAny<BlockchainNetwork>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _watchlistRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<WatchlistEntry>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.WalletAddress.Should().Be(command.WalletAddress);
        result.Network.Should().Be("Ethereum");
        result.Label.Should().Be(command.Label);
        result.Notes.Should().Be(command.Notes);

        _watchlistRepositoryMock.Verify(x => x.AddAsync(It.IsAny<WatchlistEntry>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenWalletAlreadyInWatchlist_ShouldThrowException()
    {
        // Arrange
        var command = new AddToWatchlistCommand
        {
            UserId = Guid.NewGuid(),
            WalletAddress = "0xvitalik",
            Network = "Ethereum"
        };

        _watchlistRepositoryMock
            .Setup(x => x.ExistsAsync(command.UserId, command.WalletAddress, It.IsAny<BlockchainNetwork>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*already in watchlist*");

        _watchlistRepositoryMock.Verify(x => x.AddAsync(It.IsAny<WatchlistEntry>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
