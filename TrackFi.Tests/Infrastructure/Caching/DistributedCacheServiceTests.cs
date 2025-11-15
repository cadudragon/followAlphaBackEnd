using FluentAssertions;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using System.Text;
using System.Text.Json;
using TrackFi.Infrastructure.Caching;

namespace TrackFi.Tests.Infrastructure.Caching;

public class DistributedCacheServiceTests
{
    private readonly Mock<IDistributedCache> _cacheMock;
    private readonly DistributedCacheService _service;

    public DistributedCacheServiceTests()
    {
        _cacheMock = new Mock<IDistributedCache>();
        var cacheOptions = Options.Create(new CacheOptions { Enabled = true, DefaultTtlMinutes = 15 });
        _service = new DistributedCacheService(
            _cacheMock.Object,
            NullLogger<DistributedCacheService>.Instance,
            cacheOptions);
    }

    [Fact]
    public async Task GetAsync_WithCachedValue_ShouldReturnValue()
    {
        // Arrange
        var testData = new TestData { Id = 1, Name = "Test" };
        var json = JsonSerializer.Serialize(testData, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        _cacheMock.Setup(x => x.GetAsync("test-key", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Encoding.UTF8.GetBytes(json));

        // Act
        var result = await _service.GetAsync<TestData>("test-key");

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(1);
        result.Name.Should().Be("Test");
    }

    [Fact]
    public async Task GetAsync_WithNoCache_ShouldReturnNull()
    {
        // Arrange
        _cacheMock.Setup(x => x.GetAsync("test-key", It.IsAny<CancellationToken>()))
            .ReturnsAsync((byte[]?)null);

        // Act
        var result = await _service.GetAsync<TestData>("test-key");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task SetAsync_ShouldStoreValueInCache()
    {
        // Arrange
        var testData = new TestData { Id = 1, Name = "Test" };
        var expiration = TimeSpan.FromMinutes(5);

        // Act
        await _service.SetAsync("test-key", testData, expiration);

        // Assert
        _cacheMock.Verify(x => x.SetAsync(
            "test-key",
            It.IsAny<byte[]>(),
            It.Is<DistributedCacheEntryOptions>(o =>
                o.AbsoluteExpirationRelativeToNow == expiration),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetOrCreateAsync_WithCacheHit_ShouldReturnCachedValue()
    {
        // Arrange
        var testData = new TestData { Id = 1, Name = "Cached" };
        var json = JsonSerializer.Serialize(testData, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        _cacheMock.Setup(x => x.GetAsync("test-key", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Encoding.UTF8.GetBytes(json));

        var factoryCalled = false;
        Func<Task<TestData>> factory = () =>
        {
            factoryCalled = true;
            return Task.FromResult(new TestData { Id = 2, Name = "Fresh" });
        };

        // Act
        var result = await _service.GetOrCreateAsync("test-key", factory, TimeSpan.FromMinutes(5));

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(1);
        result.Name.Should().Be("Cached");
        factoryCalled.Should().BeFalse(); // Factory should not be called
    }

    [Fact]
    public async Task GetOrCreateAsync_WithCacheMiss_ShouldCallFactoryAndCache()
    {
        // Arrange
        _cacheMock.Setup(x => x.GetAsync("test-key", It.IsAny<CancellationToken>()))
            .ReturnsAsync((byte[]?)null);

        var testData = new TestData { Id = 2, Name = "Fresh" };
        Task<TestData> factory() => Task.FromResult(testData);

        // Act
        var result = await _service.GetOrCreateAsync("test-key", factory, TimeSpan.FromMinutes(5));

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(2);
        result.Name.Should().Be("Fresh");

        _cacheMock.Verify(x => x.SetAsync(
            "test-key",
            It.IsAny<byte[]>(),
            It.IsAny<DistributedCacheEntryOptions>(),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task RemoveAsync_ShouldRemoveFromCache()
    {
        // Act
        await _service.RemoveAsync("test-key");

        // Assert
        _cacheMock.Verify(x => x.RemoveAsync("test-key", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public void GenerateKey_ShouldCombinePartsWithColon()
    {
        // Act
        var key = DistributedCacheService.GenerateKey("portfolio", "0x123", "hash");

        // Assert
        key.Should().Be("portfolio:0x123:hash");
    }

    private class TestData
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }
}
