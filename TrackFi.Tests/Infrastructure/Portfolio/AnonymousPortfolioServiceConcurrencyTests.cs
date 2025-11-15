using System.Diagnostics;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using TrackFi.Domain.Entities;
using TrackFi.Domain.Enums;
using TrackFi.Domain.Interfaces;
using TrackFi.Infrastructure.Blockchain;
using TrackFi.Infrastructure.Caching;
using TrackFi.Infrastructure.Persistence;
using TrackFi.Infrastructure.Persistence.Repositories;
using TrackFi.Infrastructure.Portfolio;

namespace TrackFi.Tests.Infrastructure.Portfolio;

/// <summary>
/// Critical tests for AnonymousPortfolioService concurrency control.
/// Tests the Bulkhead Isolation Pattern and Cooperative Cancellation implementation.
/// These tests validate the fix for the "thundering herd" problem that caused database pool exhaustion.
/// </summary>
public class AnonymousPortfolioServiceConcurrencyTests : IDisposable
{
    private readonly TrackFiDbContext _dbContext;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly DistributedCacheService _cacheService;
    private readonly Mock<AlchemyService> _mockAlchemyService;
    private readonly AnonymousPortfolioService _service;

    public AnonymousPortfolioServiceConcurrencyTests()
    {
        // Setup in-memory database
        var dbOptions = new DbContextOptionsBuilder<TrackFiDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        var services = new ServiceCollection();
        services.AddScoped(_ => new TrackFiDbContext(dbOptions));
        var serviceProvider = services.BuildServiceProvider();
        _scopeFactory = serviceProvider.GetRequiredService<IServiceScopeFactory>();

        _dbContext = new TrackFiDbContext(dbOptions);
        SeedTestData();

        // Setup cache
        var cacheOptions = Options.Create(new CacheOptions { Enabled = true });
        _cacheService = new DistributedCacheService(
            new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions())),
            NullLogger<DistributedCacheService>.Instance,
            cacheOptions);

        // Setup Alchemy service mock
        var alchemyOptions = Options.Create(new AlchemyOptions
        {
            ApiKey = "test-key",
            MultiNetworkPriorityNetworks = new List<string> { "Ethereum" }
        });

        var httpClient = new HttpClient();
        _mockAlchemyService = new Mock<AlchemyService>(
            httpClient,
            alchemyOptions,
            NullLogger<AlchemyService>.Instance,
            _cacheService,
            new TokenMetadataRepository(_scopeFactory, _cacheService, cacheOptions, NullLogger<TokenMetadataRepository>.Instance),
            cacheOptions);

        // Setup repositories
        var verifiedTokenRepository = new VerifiedTokenRepository(_scopeFactory, _cacheService, cacheOptions, NullLogger<VerifiedTokenRepository>.Instance);
        var unlistedTokenRepository = new UnlistedTokenRepository(_scopeFactory, _cacheService, cacheOptions, NullLogger<UnlistedTokenRepository>.Instance);

        var mockCmcService = new Mock<CoinMarketCapService>(
            new HttpClient(),
            Options.Create(new CoinMarketCapOptions { ApiKey = "test" }),
            NullLogger<CoinMarketCapService>.Instance);

        var tokenVerificationService = new TokenVerificationService(
            verifiedTokenRepository,
            unlistedTokenRepository,
            mockCmcService.Object,
            _scopeFactory,
            NullLogger<TokenVerificationService>.Instance);

        var mockNetworkMetadataRepo = new Mock<INetworkMetadataRepository>();
        mockNetworkMetadataRepo.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<BlockchainNetwork, NetworkMetadata>());

        _service = new AnonymousPortfolioService(
            _mockAlchemyService.Object,
            _cacheService,
            verifiedTokenRepository,
            unlistedTokenRepository,
            tokenVerificationService,
            mockNetworkMetadataRepo.Object,
            cacheOptions,
            alchemyOptions,
            NullLogger<AnonymousPortfolioService>.Instance);
    }

    private void SeedTestData()
    {
        var verifiedToken = new VerifiedToken(
            "0xverified",
            BlockchainNetwork.Ethereum,
            "TEST",
            "Test Token",
            18,
            "coinmarketcap");

        _dbContext.VerifiedTokens.Add(verifiedToken);
        _dbContext.SaveChanges();
    }

    /// <summary>
    /// CRITICAL TEST: Validates that Bulkhead Isolation limits concurrent operations to 10.
    /// This prevents the "thundering herd" problem where 100+ concurrent operations exhausted database pool.
    /// </summary>
    [Fact]
    public async Task FetchMetadataForUnknownTokens_ShouldLimitConcurrentOperations_ToMaximum10()
    {
        // Arrange: Create 50 unknown tokens to simulate whale wallet scenario
        var unknownTokens = Enumerable.Range(1, 50)
            .Select(i => new AlchemyTokenBalance
            {
                ContractAddress = $"0xtoken{i:D3}",
                TokenBalance = "1000000000000000000",
                Error = null
            })
            .ToList();

        var concurrentCallsTracker = new ConcurrentCallsTracker();

        // Mock AlchemyService to track concurrent calls
        _mockAlchemyService.Setup(x => x.GetTokenMetadataAsync(
                It.IsAny<string>(),
                It.IsAny<BlockchainNetwork>(),
                It.IsAny<CancellationToken>()))
            .Returns(async (string address, BlockchainNetwork network, CancellationToken ct) =>
            {
                concurrentCallsTracker.Enter();
                try
                {
                    // Simulate API call delay
                    await Task.Delay(100, ct);
                    return new AlchemyTokenMetadata
                    {
                        Symbol = "TEST",
                        Name = "Test Token",
                        Decimals = 18,
                        Logo = null
                    };
                }
                finally
                {
                    concurrentCallsTracker.Exit();
                }
            });

        // Act: Trigger the metadata fetch (this will use reflection to access private method)
        var method = typeof(AnonymousPortfolioService).GetMethod(
            "FetchMetadataForUnknownTokensAsync",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        var task = (Task<Dictionary<string, (string Symbol, AlchemyTokenMetadata? Metadata)>>)method!.Invoke(
            _service,
            new object[] { unknownTokens, BlockchainNetwork.Ethereum, CancellationToken.None })!;

        await task;

        // Assert: Maximum concurrent calls should never exceed 10 (Bulkhead limit)
        concurrentCallsTracker.MaxConcurrentCalls.Should().BeLessOrEqualTo(10,
            "Bulkhead Isolation Pattern should limit concurrent operations to 10 to prevent pool exhaustion");

        concurrentCallsTracker.MaxConcurrentCalls.Should().BeGreaterThan(1,
            "There should be some parallelism (otherwise the semaphore isn't working)");
    }

    /// <summary>
    /// CRITICAL TEST: Validates that timeout cancels all pending operations.
    /// This prevents orphaned tasks from continuing after client disconnect.
    /// </summary>
    [Fact]
    public async Task FetchMetadataForUnknownTokens_ShouldCancelAllOperations_WhenTimeoutOccurs()
    {
        // Arrange: Create tokens that will exceed timeout
        var unknownTokens = Enumerable.Range(1, 30)
            .Select(i => new AlchemyTokenBalance
            {
                ContractAddress = $"0xtoken{i:D3}",
                TokenBalance = "1000000000000000000",
                Error = null
            })
            .ToList();

        var cancelledCount = 0;
        var completedCount = 0;

        // Mock AlchemyService to simulate slow operations
        _mockAlchemyService.Setup(x => x.GetTokenMetadataAsync(
                It.IsAny<string>(),
                It.IsAny<BlockchainNetwork>(),
                It.IsAny<CancellationToken>()))
            .Returns(async (string address, BlockchainNetwork network, CancellationToken ct) =>
            {
                try
                {
                    // Simulate very slow API call (would take 3 seconds per token)
                    await Task.Delay(3000, ct);
                    Interlocked.Increment(ref completedCount);
                    return new AlchemyTokenMetadata
                    {
                        Symbol = "TEST",
                        Name = "Test Token",
                        Decimals = 18,
                        Logo = null
                    };
                }
                catch (OperationCanceledException)
                {
                    Interlocked.Increment(ref cancelledCount);
                    throw;
                }
            });

        // Act: Use CancellationTokenSource with short timeout to simulate timeout scenario
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));

        var method = typeof(AnonymousPortfolioService).GetMethod(
            "FetchMetadataForUnknownTokensAsync",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        var task = (Task<Dictionary<string, (string Symbol, AlchemyTokenMetadata? Metadata)>>)method!.Invoke(
            _service,
            new object[] { unknownTokens, BlockchainNetwork.Ethereum, cts.Token })!;

        var result = await task;

        // Assert: Most operations should be cancelled (not completed)
        cancelledCount.Should().BeGreaterThan(0,
            "Timeout should cancel pending operations to prevent orphaned tasks");

        completedCount.Should().BeLessThan(unknownTokens.Count,
            "Not all operations should complete if timeout occurs");

        result.Should().NotBeNull(
            "Partial results should still be returned on timeout (best effort)");
    }

    /// <summary>
    /// CRITICAL TEST: Validates that semaphore is released even when exceptions occur.
    /// This prevents deadlock where semaphore slots are never released.
    /// </summary>
    [Fact]
    public async Task FetchMetadataForUnknownTokens_ShouldReleaseSemaphore_EvenWhenExceptionOccurs()
    {
        // Arrange: Create tokens where some will fail
        var unknownTokens = Enumerable.Range(1, 20)
            .Select(i => new AlchemyTokenBalance
            {
                ContractAddress = $"0xtoken{i:D3}",
                TokenBalance = "1000000000000000000",
                Error = null
            })
            .ToList();

        var callCount = 0;

        // Mock AlchemyService to throw exceptions for odd-numbered tokens
        _mockAlchemyService.Setup(x => x.GetTokenMetadataAsync(
                It.IsAny<string>(),
                It.IsAny<BlockchainNetwork>(),
                It.IsAny<CancellationToken>()))
            .Returns(async (string address, BlockchainNetwork network, CancellationToken ct) =>
            {
                var count = Interlocked.Increment(ref callCount);
                await Task.Delay(50, ct);

                if (count % 2 == 1)
                {
                    throw new HttpRequestException("Simulated API failure");
                }

                return new AlchemyTokenMetadata
                {
                    Symbol = "TEST",
                    Name = "Test Token",
                    Decimals = 18,
                    Logo = null
                };
            });

        // Act: Execute method
        var method = typeof(AnonymousPortfolioService).GetMethod(
            "FetchMetadataForUnknownTokensAsync",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        var task = (Task<Dictionary<string, (string Symbol, AlchemyTokenMetadata? Metadata)>>)method!.Invoke(
            _service,
            new object[] { unknownTokens, BlockchainNetwork.Ethereum, CancellationToken.None })!;

        var result = await task;

        // Assert: All operations should complete (not hang)
        callCount.Should().Be(unknownTokens.Count,
            "All tokens should be processed even if some fail");

        result.Should().NotBeNull(
            "Result should be returned even with partial failures");

        // Verify semaphore was properly released (all operations completed without deadlock)
        result.Count.Should().BeGreaterThan(0,
            "Some successful operations should return results");
    }

    /// <summary>
    /// CRITICAL TEST: Validates that client cancellation immediately stops all operations.
    /// This prevents wasted resources when client disconnects.
    /// </summary>
    [Fact]
    public async Task FetchMetadataForUnknownTokens_ShouldStopImmediately_WhenClientCancels()
    {
        // Arrange
        var unknownTokens = Enumerable.Range(1, 100)
            .Select(i => new AlchemyTokenBalance
            {
                ContractAddress = $"0xtoken{i:D3}",
                TokenBalance = "1000000000000000000",
                Error = null
            })
            .ToList();

        var startedCount = 0;
        var completedCount = 0;

        _mockAlchemyService.Setup(x => x.GetTokenMetadataAsync(
                It.IsAny<string>(),
                It.IsAny<BlockchainNetwork>(),
                It.IsAny<CancellationToken>()))
            .Returns(async (string address, BlockchainNetwork network, CancellationToken ct) =>
            {
                Interlocked.Increment(ref startedCount);
                await Task.Delay(1000, ct); // Simulate slow operation
                Interlocked.Increment(ref completedCount);
                return new AlchemyTokenMetadata { Symbol = "TEST", Name = "Test", Decimals = 18 };
            });

        // Act: Cancel after 500ms (simulating client disconnect)
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(500));

        var method = typeof(AnonymousPortfolioService).GetMethod(
            "FetchMetadataForUnknownTokensAsync",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        var stopwatch = Stopwatch.StartNew();

        var task = (Task<Dictionary<string, (string Symbol, AlchemyTokenMetadata? Metadata)>>)method!.Invoke(
            _service,
            new object[] { unknownTokens, BlockchainNetwork.Ethereum, cts.Token })!;

        await task;
        stopwatch.Stop();

        // Assert: Operation should complete quickly (not wait for all 100 operations)
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(3000,
            "Cancellation should stop operations quickly, not wait for all tasks to complete");

        completedCount.Should().BeLessThan(unknownTokens.Count,
            "Not all operations should complete when cancelled");

        startedCount.Should().BeLessThan(unknownTokens.Count,
            "Not all operations should even start when cancelled early");
    }

    /// <summary>
    /// PERFORMANCE TEST: Validates that with bulkhead, operations complete in reasonable time.
    /// Without bulkhead, 100 concurrent DB operations would cause pool exhaustion and timeout.
    /// With bulkhead (10 concurrent), operations queue but complete successfully.
    /// </summary>
    [Fact]
    public async Task FetchMetadataForUnknownTokens_ShouldCompleteSuccessfully_WithBulkheadLimit()
    {
        // Arrange: Simulate whale wallet scenario (100 tokens)
        var unknownTokens = Enumerable.Range(1, 100)
            .Select(i => new AlchemyTokenBalance
            {
                ContractAddress = $"0xtoken{i:D3}",
                TokenBalance = "1000000000000000000",
                Error = null
            })
            .ToList();

        _mockAlchemyService.Setup(x => x.GetTokenMetadataAsync(
                It.IsAny<string>(),
                It.IsAny<BlockchainNetwork>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AlchemyTokenMetadata
            {
                Symbol = "TEST",
                Name = "Test Token",
                Decimals = 18,
                Logo = null
            });

        // Act: Execute with generous timeout (should complete well within 30s with bulkhead)
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        var stopwatch = Stopwatch.StartNew();

        var method = typeof(AnonymousPortfolioService).GetMethod(
            "FetchMetadataForUnknownTokensAsync",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        var task = (Task<Dictionary<string, (string Symbol, AlchemyTokenMetadata? Metadata)>>)method!.Invoke(
            _service,
            new object[] { unknownTokens, BlockchainNetwork.Ethereum, cts.Token })!;

        var result = await task;
        stopwatch.Stop();

        // Assert: All operations should complete successfully
        result.Should().HaveCount(100,
            "All 100 tokens should be processed successfully with bulkhead control");

        stopwatch.Elapsed.Should().BeLessThan(TimeSpan.FromSeconds(30),
            "Operations should complete within timeout window");

        // Operations should take reasonable time (queuing overhead but no pool exhaustion)
        stopwatch.Elapsed.Should().BeGreaterThan(TimeSpan.FromMilliseconds(100),
            "Operations should take some time due to queuing");
    }

    public void Dispose()
    {
        _dbContext?.Dispose();
    }

    /// <summary>
    /// Helper class to track concurrent calls in tests.
    /// Used to validate that Bulkhead Isolation Pattern limits concurrency correctly.
    /// </summary>
    private class ConcurrentCallsTracker
    {
        private int _currentConcurrentCalls = 0;
        private int _maxConcurrentCalls = 0;
        private readonly object _lock = new();

        public int MaxConcurrentCalls => _maxConcurrentCalls;

        public void Enter()
        {
            lock (_lock)
            {
                _currentConcurrentCalls++;
                if (_currentConcurrentCalls > _maxConcurrentCalls)
                {
                    _maxConcurrentCalls = _currentConcurrentCalls;
                }
            }
        }

        public void Exit()
        {
            lock (_lock)
            {
                _currentConcurrentCalls--;
            }
        }
    }
}
