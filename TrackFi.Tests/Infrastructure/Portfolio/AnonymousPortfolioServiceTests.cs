using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using TrackFi.Application.Portfolio.DTOs;
using TrackFi.Domain.Entities;
using TrackFi.Domain.Enums;
using TrackFi.Domain.Interfaces;
using TrackFi.Infrastructure.Blockchain;
using TrackFi.Infrastructure.Caching;
using TrackFi.Infrastructure.Persistence;
using TrackFi.Infrastructure.Persistence.Repositories;
using TrackFi.Infrastructure.Portfolio;

namespace TrackFi.Tests.Infrastructure.Portfolio;

public class AnonymousPortfolioServiceTests : IDisposable
{
    private readonly TestHttpMessageHandler _handler;
    private readonly DistributedCacheService _cacheService;
    private readonly AnonymousPortfolioService _service;
    private readonly TrackFiDbContext _dbContext;

    public AnonymousPortfolioServiceTests()
    {
        _handler = new TestHttpMessageHandler();

        var httpClient = new HttpClient(_handler)
        {
            BaseAddress = new Uri("https://eth-mainnet.g.alchemy.com")
        };

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Alchemy:ApiKey"] = "test-api-key",
                ["CoinMarketCap:ApiKey"] = "test-cmc-key"
            })
            .Build();

        var cacheOptions = Options.Create(new CacheOptions { Enabled = true });
        var alchemyOptions = Options.Create(new AlchemyOptions
        {
            ApiKey = "test-api-key",
            MultiNetworkPriorityNetworks = new List<string> { "Ethereum", "Polygon", "Arbitrum", "Base", "Optimism" }
        });
        _cacheService = new DistributedCacheService(
            new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions())),
            NullLogger<DistributedCacheService>.Instance,
            cacheOptions);

        // Create in-memory database options
        var dbOptions = new DbContextOptionsBuilder<TrackFiDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        // Create service provider for IServiceScopeFactory with DbContext properly registered
        var services = new ServiceCollection();
        services.AddScoped(_ => new TrackFiDbContext(dbOptions));
        var serviceProvider = services.BuildServiceProvider();
        var scopeFactory = serviceProvider.GetRequiredService<IServiceScopeFactory>();

        // Create a DbContext instance for seeding and direct access
        _dbContext = new TrackFiDbContext(dbOptions);
        SeedTestData();

        // Create real repository instances (all use IServiceScopeFactory for thread-safe DbContext access)
        var tokenMetadataRepository = new TokenMetadataRepository(scopeFactory, _cacheService, cacheOptions, NullLogger<TokenMetadataRepository>.Instance);
        var verifiedTokenRepository = new VerifiedTokenRepository(scopeFactory, _cacheService, cacheOptions, NullLogger<VerifiedTokenRepository>.Instance);
        var unlistedTokenRepository = new UnlistedTokenRepository(scopeFactory, _cacheService, cacheOptions, NullLogger<UnlistedTokenRepository>.Instance);

        // Create CoinMarketCap service (with stub HTTP client)
        var cmcHttpClient = new HttpClient(new StubCmcHttpHandler());
        var cmcOptions = Options.Create(new CoinMarketCapOptions { ApiKey = "test-key" });
        var cmcService = new CoinMarketCapService(cmcHttpClient, cmcOptions, NullLogger<CoinMarketCapService>.Instance);
        var tokenVerificationService = new TokenVerificationService(
            verifiedTokenRepository,
            unlistedTokenRepository,
            cmcService,
            scopeFactory,
            NullLogger<TokenVerificationService>.Instance);

        var alchemyService = new AlchemyService(
            httpClient,
            alchemyOptions,
            NullLogger<AlchemyService>.Instance,
            _cacheService,
            tokenMetadataRepository,
            cacheOptions);

        var mockNetworkMetadataRepo = CreateMockNetworkMetadataRepository();

        _service = new AnonymousPortfolioService(
            alchemyService,
            _cacheService,
            verifiedTokenRepository,
            unlistedTokenRepository,
            tokenVerificationService,
            mockNetworkMetadataRepo,
            cacheOptions,
            alchemyOptions,
            NullLogger<AnonymousPortfolioService>.Instance);
    }

    private void SeedTestData()
    {
        // Seed verified token for testing
        var verifiedToken = new VerifiedToken(
            "0xtoken1",
            BlockchainNetwork.Ethereum,
            "TKN",
            "Token One",
            18,
            "https://logo.example/token.png",
            null, null, null, null,
            false);
        _dbContext.VerifiedTokens.Add(verifiedToken);
        _dbContext.SaveChanges();
    }

    public void Dispose()
    {
        _handler.Reset();
        _dbContext?.Dispose();
    }

    [Fact]
    public async Task GetTokenBalancesAsync_ReturnsNativeAndVerifiedTokensWithPrices()
    {
        var result = await _service.GetTokenBalancesAsync(
            "0xa3660aBb49644876714611122b1618faA07e0281",
            BlockchainNetwork.Ethereum);

        result.Should().HaveCount(2);

        var native = result.Single(t => t.ContractAddress is null);
        native.Symbol.Should().Be("ETH");
        native.BalanceFormatted.Should().BeGreaterThan(0);
        native.Price.Should().NotBeNull();
        native.ValueUsd.Should().BeGreaterThan(0);

        var token = result.Single(t => t.ContractAddress != null);
        token.ContractAddress.Should().NotBeNull();
        token.ContractAddress!.Equals("0xtoken1", StringComparison.OrdinalIgnoreCase).Should().BeTrue();
        token.Symbol.Should().Be("TKN");
        token.BalanceFormatted.Should().Be(1m);
        token.Price.Should().NotBeNull();
        token.Price!.Usd.Should().Be(2.5m);
        token.ValueUsd.Should().Be(2.5m);
    }

    [Fact]
    public async Task GetTokenBalancesAsync_UsesCachedBalancesAndPrices_OnSubsequentCall()
    {
        await _service.GetTokenBalancesAsync(
            "0xa3660aBb49644876714611122b1618faA07e0281",
            BlockchainNetwork.Ethereum);

        await _service.GetTokenBalancesAsync(
            "0xa3660aBb49644876714611122b1618faA07e0281",
            BlockchainNetwork.Ethereum);

        _handler.NativeBalanceCalls.Should().Be(1);
        _handler.TokenBalancesCalls.Should().Be(1);
        _handler.TokenMetadataCalls.Should().Be(1);
        _handler.PriceCalls.Should().Be(1);
    }

    /// <summary>
    /// Stub HTTP handler for CoinMarketCap API calls.
    /// Returns verified=true for "0xtoken1" and false for all others.
    /// </summary>
    private sealed class StubCmcHttpHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            // Return a successful CMC response
            // For simplicity, always return verified for 0xtoken1
            var response = new
            {
                data = new Dictionary<string, object>
                {
                    ["0xtoken1"] = new
                    {
                        id = 1,
                        name = "Token One",
                        symbol = "TKN",
                        is_active = 1
                    }
                }
            };

            var json = JsonSerializer.Serialize(response);
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            });
        }
    }

    private sealed class TestHttpMessageHandler : HttpMessageHandler
    {
        private readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web);

        public int NativeBalanceCalls { get; private set; }
        public int TokenBalancesCalls { get; private set; }
        public int TokenMetadataCalls { get; private set; }
        public int PriceCalls { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (request.RequestUri is null)
            {
                return new HttpResponseMessage(HttpStatusCode.BadRequest);
            }

            if (request.RequestUri.AbsoluteUri.Contains("/prices/v1/", StringComparison.OrdinalIgnoreCase))
            {
                PriceCalls++;
                var response = new AlchemyPricesResponse
                {
                    Data =
                    [
                        new AlchemyTokenPrice
                        {
                            Address = "0xtoken1",
                            Prices =
                            [
                                new AlchemyPrice { Currency = "USD", Value = "2.5" }
                            ]
                        },
                        new AlchemyTokenPrice
                        {
                            Address = "0xc02aaa39b223fe8d0a0e5c4f27ead9083c756cc2",
                            Prices =
                            [
                                new AlchemyPrice { Currency = "USD", Value = "3200" }
                            ]
                        }
                    ]
                };

                return CreateJsonResponse(response);
            }

            var payload = request.Content is null
                ? null
                : JsonDocument.Parse(await request.Content.ReadAsStringAsync(cancellationToken));

            var method = payload?.RootElement.GetProperty("method").GetString();

            return method switch
            {
                "eth_getBalance" => HandleNativeBalance(),
                "alchemy_getTokenBalances" => HandleTokenBalances(),
                "alchemy_getTokenMetadata" => HandleTokenMetadata(),
                _ => new HttpResponseMessage(HttpStatusCode.NotFound)
            };
        }

        public void Reset()
        {
            NativeBalanceCalls = 0;
            TokenBalancesCalls = 0;
            TokenMetadataCalls = 0;
            PriceCalls = 0;
        }

        private HttpResponseMessage HandleNativeBalance()
        {
            NativeBalanceCalls++;
            var response = new AlchemyBalanceResponse
            {
                Result = "0x4563918244F40000" // 5 ETH
            };
            return CreateJsonResponse(response);
        }

        private HttpResponseMessage HandleTokenBalances()
        {
            TokenBalancesCalls++;
            var response = new AlchemyTokenBalanceResponse
            {
                Result = new TokenBalanceResult
                {
                    TokenBalances =
                    [
                        new AlchemyTokenBalance
                        {
                            ContractAddress = "0xToken1",
                            TokenBalance = "0x0de0b6b3a7640000" // 1 token at 18 decimals
                        },
                        new AlchemyTokenBalance
                        {
                            ContractAddress = "0xUnverified",
                            TokenBalance = "0x0de0b6b3a7640000"
                        }
                    ]
                }
            };
            return CreateJsonResponse(response);
        }

        private HttpResponseMessage HandleTokenMetadata()
        {
            TokenMetadataCalls++;
            var response = new AlchemyTokenMetadataResponse
            {
                Result = new AlchemyTokenMetadata
                {
                    Decimals = 18,
                    Logo = "https://logo.example/token.png",
                    Name = "Token One",
                    Symbol = "TKN"
                }
            };
            return CreateJsonResponse(response);
        }

        private HttpResponseMessage CreateJsonResponse<T>(T payload)
        {
            var json = JsonSerializer.Serialize(payload, _jsonOptions);
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };
        }
    }

    private static INetworkMetadataRepository CreateMockNetworkMetadataRepository()
    {
        var mock = new Moq.Mock<INetworkMetadataRepository>();

        // Setup to return network metadata with logo URLs
        var networkMetadata = new Dictionary<BlockchainNetwork, NetworkMetadata>
        {
            { BlockchainNetwork.Base, new NetworkMetadata
                {
                    Network = BlockchainNetwork.Base,
                    Name = "Base",
                    LogoUrl = "/images/networks/Base.svg"
                }
            },
            { BlockchainNetwork.Ethereum, new NetworkMetadata
                {
                    Network = BlockchainNetwork.Ethereum,
                    Name = "Ethereum",
                    LogoUrl = "/images/networks/Ethereum.svg"
                }
            }
        };

        mock.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(networkMetadata);

        mock.Setup(x => x.GetByNetworkAsync(It.IsAny<BlockchainNetwork>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((BlockchainNetwork network, CancellationToken _) =>
                networkMetadata.TryGetValue(network, out var metadata) ? metadata : null);

        return mock.Object;
    }
}
