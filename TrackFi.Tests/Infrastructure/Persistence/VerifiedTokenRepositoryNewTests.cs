using System;
using System.Reflection;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using TrackFi.Domain.Entities;
using TrackFi.Domain.Enums;
using TrackFi.Infrastructure.Caching;
using TrackFi.Infrastructure.Persistence;
using TrackFi.Infrastructure.Persistence.Repositories;

namespace TrackFi.Tests.Infrastructure.Persistence;

public class VerifiedTokenRepositoryNewTests : IDisposable
{
    public VerifiedTokenRepositoryNewTests() => ClearStaticCaches();

    public void Dispose() => ClearStaticCaches();

    [Fact]
    public async Task GetVerifiedTokensAsync_LoadsFromDatabaseAndCachesResults()
    {
        using var fixture = new RepositoryFixture();

        await fixture.Repository.AddAsync(CreateToken("0xABCDEF1234567890ABCDEF1234567890ABCDEF12", BlockchainNetwork.Ethereum, "ETHX"));
        await fixture.Repository.AddAsync(CreateToken("0xBBBBBB1234567890ABCDEF1234567890ABCDEF12", BlockchainNetwork.Polygon, "POLY"));

        var result = await fixture.Repository.GetVerifiedTokensAsync(BlockchainNetwork.Ethereum);

        result.Should().ContainKey("0xabcdef1234567890abcdef1234567890abcdef12");
        result["0xabcdef1234567890abcdef1234567890abcdef12"].Symbol.Should().Be("ETHX");

        var cacheKey = DistributedCacheService.GenerateKey("verified_tokens", "ethereum");
        var cached = await fixture.CacheService.GetAsync<List<VerifiedTokenCacheEntry>>(cacheKey);

        cached.Should().NotBeNull();
        cached!.Should().HaveCount(1);
        cached[0].ContractAddress.Should().Be("0xabcdef1234567890abcdef1234567890abcdef12");
    }

    [Fact]
    public async Task AddAsync_InvalidatesCaches_ForNetwork()
    {
        using var fixture = new RepositoryFixture();

        await fixture.Repository.AddAsync(CreateToken("0xAAAAAA1234567890ABCDEF1234567890ABCDEF12", BlockchainNetwork.Arbitrum, "ARB"));

        var initial = await fixture.Repository.GetVerifiedTokensAsync(BlockchainNetwork.Arbitrum);
        initial.Should().ContainKey("0xaaaaaa1234567890abcdef1234567890abcdef12");

        var cacheKey = DistributedCacheService.GenerateKey("verified_tokens", "arbitrum");
        (await fixture.CacheService.GetAsync<List<VerifiedTokenCacheEntry>>(cacheKey)).Should().NotBeNull();

        await fixture.Repository.AddAsync(CreateToken("0xCCCCCC1234567890ABCDEF1234567890ABCDEF12", BlockchainNetwork.Arbitrum, "ARB2"));

        var afterAdd = await fixture.Repository.GetVerifiedTokensAsync(BlockchainNetwork.Arbitrum);
        afterAdd.Should().HaveCount(2);
        afterAdd.Keys.Should().Contain("0xcccccc1234567890abcdef1234567890abcdef12");
    }

    private static VerifiedToken CreateToken(string address, BlockchainNetwork network, string symbol)
        => new(address, network, symbol, $"{symbol} Token");

    private static void ClearStaticCaches()
    {
        ClearConcurrentDictionary("MemoryCache");
        ClearConcurrentDictionary("Locks");
    }

    private static void ClearConcurrentDictionary(string fieldName)
    {
        var field = typeof(VerifiedTokenRepository).GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Static);
        if (field?.GetValue(null) is { } dictionary)
        {
            var clearMethod = dictionary.GetType().GetMethod("Clear");
            clearMethod?.Invoke(dictionary, null);
        }
    }

    private sealed class RepositoryFixture : IDisposable
    {
        private readonly SqliteConnection _connection;
        private readonly CacheOptions _cacheOptions = new() { Enabled = true };
        private readonly ServiceProvider _serviceProvider;

        public RepositoryFixture()
        {
            _connection = new SqliteConnection("DataSource=TrackFiTests;Mode=Memory;Cache=Shared");
            _connection.Open();

            var services = new ServiceCollection();
            services.AddDbContext<TrackFiDbContext>(builder => builder.UseSqlite(_connection));
            _serviceProvider = services.BuildServiceProvider();

            using (var scope = _serviceProvider.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<TrackFiDbContext>();
                context.Database.EnsureCreated();
            }

            CacheService = new DistributedCacheService(
                new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions())),
                NullLogger<DistributedCacheService>.Instance,
                Options.Create(_cacheOptions));

            var scopeFactory = _serviceProvider.GetRequiredService<IServiceScopeFactory>();

            Repository = new VerifiedTokenRepository(
                scopeFactory,
                CacheService,
                Options.Create(_cacheOptions),
                NullLogger<VerifiedTokenRepository>.Instance);
        }

        public DistributedCacheService CacheService { get; }

        public VerifiedTokenRepository Repository { get; }

        public void Dispose()
        {
            _serviceProvider.Dispose();
            _connection.Dispose();
        }
    }
}
