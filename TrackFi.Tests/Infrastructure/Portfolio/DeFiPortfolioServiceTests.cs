using FluentAssertions;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using TrackFi.Domain.Entities;
using TrackFi.Domain.Enums;
using TrackFi.Domain.Interfaces;
using TrackFi.Infrastructure.Blockchain;
using TrackFi.Infrastructure.Caching;
using TrackFi.Infrastructure.DeFi;
using TrackFi.Infrastructure.Persistence;
using TrackFi.Infrastructure.Persistence.Repositories;
using TrackFi.Infrastructure.Portfolio;

namespace TrackFi.Tests.Infrastructure.Portfolio;

public class DeFiPortfolioServiceTests
{
    [Fact]
    public async Task GetDeFiPositionsAsync_AggregatsFarmingPositions_ByGroupId()
    {
        // Arrange
        var mockProvider = new MockDeFiDataProvider();
        var mockPriceEnrichment = new MockDeFiPriceEnrichmentService();
        var mockNetworkMetadataRepo = CreateMockNetworkMetadataRepository();
        var mockDefiOptions = CreateMockDeFiOptions();
        var service = new DeFiPortfolioService(
            mockProvider,
            mockPriceEnrichment,
            mockNetworkMetadataRepo,
            mockDefiOptions,
            NullLogger<DeFiPortfolioService>.Instance);

        // Act
        var result = await service.GetDeFiPositionsAsync(
            "0x1234567890123456789012345678901234567890",
            BlockchainNetwork.Base);

        // Assert
        result.Should().NotBeNull();
        result.WalletAddress.Should().Be("0x1234567890123456789012345678901234567890");
        result.Network.Should().Be("Base");
        // Total includes farming (110) + lending (700) = 810
        result.TotalValueUsd.Should().Be(810m);

        // Verify farming positions are aggregated
        result.Farming.Should().HaveCount(1);
        var farmingPosition = result.Farming.First();

        // Verify basic info
        farmingPosition.Protocol.Should().Be("PancakeSwap V3");
        farmingPosition.ProtocolId.Should().Be("pancakeswap-v3");
        farmingPosition.PoolName.Should().Be("WETH/USDC Pool");
        farmingPosition.PoolAddress.Should().Be("0xpool123");

        // Verify values
        farmingPosition.TotalValueUsd.Should().Be(110m);
        farmingPosition.StakedValueUsd.Should().Be(100m); // 60 (WETH) + 40 (USDC)
        farmingPosition.RewardsValueUsd.Should().Be(10m);

        // Verify staked assets (both WETH and USDC)
        farmingPosition.StakedAssets.Should().HaveCount(2);
        farmingPosition.StakedAssets.Should().Contain(a => a.Symbol == "WETH" && a.UsdValue == 60m);
        farmingPosition.StakedAssets.Should().Contain(a => a.Symbol == "USDC" && a.UsdValue == 40m);

        // Verify reward assets (CAKE + fees)
        farmingPosition.RewardAssets.Should().HaveCount(3);
        farmingPosition.RewardAssets.Should().Contain(r => r.Symbol == "CAKE" && r.UsdValue == 5m);
        farmingPosition.RewardAssets.Should().Contain(r => r.Symbol == "USDC" && r.UsdValue == 2m);
        farmingPosition.RewardAssets.Should().Contain(r => r.Symbol == "WETH" && r.UsdValue == 3m);
    }

    [Fact]
    public async Task GetDeFiPortfolioAsync_AggregatesLendingPositions_ByProtocol()
    {
        // Arrange
        var mockProvider = new MockDeFiDataProvider();
        var mockPriceEnrichment = new MockDeFiPriceEnrichmentService();
        var mockNetworkMetadataRepo = CreateMockNetworkMetadataRepository();
        var mockDefiOptions = CreateMockDeFiOptions();
        var service = new DeFiPortfolioService(
            mockProvider,
            mockPriceEnrichment,
            mockNetworkMetadataRepo,
            mockDefiOptions,
            NullLogger<DeFiPortfolioService>.Instance);

        // Act
        var result = await service.GetDeFiPositionsAsync(
            "0x1234567890123456789012345678901234567890",
            BlockchainNetwork.Base);

        // Assert
        result.Lending.Should().HaveCount(1);
        var lendingPosition = result.Lending.First();

        // Verify basic info
        lendingPosition.Protocol.Should().Be("Aave V3");
        lendingPosition.ProtocolId.Should().Be("aave-v3");

        // Verify values
        lendingPosition.SuppliedValueUsd.Should().Be(1000m);
        lendingPosition.BorrowedValueUsd.Should().Be(300m);
        lendingPosition.NetValueUsd.Should().Be(700m); // 1000 - 300

        // Verify health factor
        lendingPosition.HealthFactor.Should().Be(2.5m);

        // Verify supplied assets
        lendingPosition.SuppliedAssets.Should().HaveCount(1);
        lendingPosition.SuppliedAssets[0].Symbol.Should().Be("USDC");
        lendingPosition.SuppliedAssets[0].UsdValue.Should().Be(1000m);

        // Verify borrowed assets
        lendingPosition.BorrowedAssets.Should().HaveCount(1);
        lendingPosition.BorrowedAssets[0].Symbol.Should().Be("ETH");
        lendingPosition.BorrowedAssets[0].UsdValue.Should().Be(300m);
        lendingPosition.BorrowedAssets[0].IsDebt.Should().BeTrue();
    }

    [Fact]
    public async Task GetMultiNetworkDeFiPositionsAsync_AggregatesAcrossNetworks()
    {
        // Arrange
        var mockProvider = new MockDeFiDataProvider();
        var mockPriceEnrichment = new MockDeFiPriceEnrichmentService();
        var mockNetworkMetadataRepo = CreateMockNetworkMetadataRepository();
        var mockDefiOptions = CreateMockDeFiOptions();
        var service = new DeFiPortfolioService(
            mockProvider,
            mockPriceEnrichment,
            mockNetworkMetadataRepo,
            mockDefiOptions,
            NullLogger<DeFiPortfolioService>.Instance);

        // Act
        var result = await service.GetMultiNetworkDeFiPositionsAsync(
            "0x1234567890123456789012345678901234567890",
            new[] { BlockchainNetwork.Base, BlockchainNetwork.Ethereum });

        // Assert
        result.Should().NotBeNull();
        result.WalletAddress.Should().Be("0x1234567890123456789012345678901234567890");
        result.TotalValueUsd.Should().Be(1620m); // Base: 810, Ethereum: 810
        result.Networks.Should().HaveCount(2);

        // Verify Base network
        var baseNetwork = result.Networks.First(n => n.Network == "Base");
        baseNetwork.TotalValueUsd.Should().Be(810m);
        baseNetwork.Farming.Should().HaveCount(1);
        baseNetwork.Lending.Should().HaveCount(1);

        // Verify Ethereum network
        var ethNetwork = result.Networks.First(n => n.Network == "Ethereum");
        ethNetwork.TotalValueUsd.Should().Be(810m);
        ethNetwork.Farming.Should().HaveCount(1);
    }

    /// <summary>
    /// Mock DeFi data provider that returns PRE-AGGREGATED positions.
    /// This simulates what ZerionService returns after provider-specific aggregation.
    /// </summary>
    private class MockDeFiDataProvider : IDeFiDataProvider
    {
        public Task<List<DeFiPositionData>> GetPositionsAsync(
            string walletAddress,
            BlockchainNetwork network,
            CancellationToken cancellationToken = default)
        {
            var positions = new List<DeFiPositionData>();

            if (network == BlockchainNetwork.Base)
            {
                // PRE-AGGREGATED Farming Position: PancakeSwap V3 WETH/USDC Pool
                // Provider has already combined 2 staked positions + 3 reward positions
                positions.Add(new DeFiPositionData
                {
                    Id = "pos-1",
                    ProtocolName = "PancakeSwap V3",
                    ProtocolId = "pancakeswap-v3",
                    ProtocolModule = "farming",
                    PoolAddress = "0xpool123",
                    GroupId = "group-123",
                    Name = "WETH/USDC Pool",
                    PositionType = DeFiPositionDataType.Farming, // Already aggregated!
                    Label = "Farming",
                    TotalValueUsd = 110m, // 60 + 40 + 5 + 2 + 3
                    UnclaimedValueUsd = 10m, // Rewards value
                    // All tokens combined: staked first, then rewards
                    Tokens = new List<DeFiToken>
                    {
                        // Staked tokens (2)
                        new DeFiToken
                        {
                            Name = "Wrapped Ether",
                            Symbol = "WETH",
                            ContractAddress = "0xweth",
                            Decimals = 18,
                            TokenType = DeFiTokenType.DeFiToken,
                            Balance = 0.02m,
                            BalanceFormatted = "0.02",
                            UsdPrice = 3000m,
                            UsdValue = 60m
                        },
                        new DeFiToken
                        {
                            Name = "USD Coin",
                            Symbol = "USDC",
                            ContractAddress = "0xusdc",
                            Decimals = 6,
                            TokenType = DeFiTokenType.DeFiToken,
                            Balance = 40m,
                            BalanceFormatted = "40.0",
                            UsdPrice = 1m,
                            UsdValue = 40m
                        },
                        // Reward tokens (3)
                        new DeFiToken
                        {
                            Name = "PancakeSwap Token",
                            Symbol = "CAKE",
                            ContractAddress = "0xcake123",
                            Decimals = 18,
                            TokenType = DeFiTokenType.Reward,
                            Balance = 1m,
                            BalanceFormatted = "1.0",
                            UsdPrice = 5m,
                            UsdValue = 5m
                        },
                        new DeFiToken
                        {
                            Name = "USD Coin",
                            Symbol = "USDC",
                            ContractAddress = "0xusdc123",
                            Decimals = 6,
                            TokenType = DeFiTokenType.Reward,
                            Balance = 2m,
                            BalanceFormatted = "2.0",
                            UsdPrice = 1m,
                            UsdValue = 2m
                        },
                        new DeFiToken
                        {
                            Name = "Wrapped Ether",
                            Symbol = "WETH",
                            ContractAddress = "0xweth123",
                            Decimals = 18,
                            TokenType = DeFiTokenType.Reward,
                            Balance = 0.001m,
                            BalanceFormatted = "0.001",
                            UsdPrice = 3000m,
                            UsdValue = 3m
                        }
                    },
                    // Aggregation metadata from provider
                    Details = new DeFiPositionDetails
                    {
                        StakedCount = 2, // WETH + USDC tokens
                        RewardsCount = 3, // CAKE + USDC + WETH tokens
                        StakedValueUsd = 100m, // 60 + 40
                        RewardsValueUsd = 10m // 5 + 2 + 3
                    }
                });

                // PRE-AGGREGATED Lending Position: Aave V3
                // Provider has already combined supplied + borrowed positions
                positions.Add(new DeFiPositionData
                {
                    Id = "pos-2",
                    ProtocolName = "Aave V3",
                    ProtocolId = "aave-v3",
                    ProtocolModule = "lending",
                    Name = "Aave V3 Lending",
                    PositionType = DeFiPositionDataType.Supplied, // Mark as lending
                    Label = "Lending",
                    TotalValueUsd = 700m, // Net: 1000 - 300
                    AccountData = new DeFiAccountData
                    {
                        HealthFactor = 2.5m
                    },
                    // All tokens combined: supplied + borrowed
                    Tokens = new List<DeFiToken>
                    {
                        // Supplied token
                        new DeFiToken
                        {
                            Name = "USD Coin",
                            Symbol = "USDC",
                            ContractAddress = "0xusdc123",
                            Decimals = 6,
                            TokenType = DeFiTokenType.Supplied,
                            Balance = 1000m,
                            BalanceFormatted = "1000.0",
                            UsdPrice = 1m,
                            UsdValue = 1000m
                        },
                        // Borrowed token
                        new DeFiToken
                        {
                            Name = "Ethereum",
                            Symbol = "ETH",
                            ContractAddress = "0xeth123",
                            Decimals = 18,
                            TokenType = DeFiTokenType.Borrowed,
                            Balance = 0.1m,
                            BalanceFormatted = "0.1",
                            UsdPrice = 3000m,
                            UsdValue = 300m
                        }
                    },
                    // Aggregation metadata from provider
                    Details = new DeFiPositionDetails
                    {
                        SuppliedValueUsd = 1000m,
                        BorrowedValueUsd = 300m,
                        NetValueUsd = 700m
                    }
                });
            }
            else if (network == BlockchainNetwork.Ethereum)
            {
                // PRE-AGGREGATED Farming: Uniswap V3 (different from Base)
                positions.Add(new DeFiPositionData
                {
                    Id = "pos-eth-1",
                    ProtocolName = "Uniswap V3",
                    ProtocolId = "uniswap-v3",
                    ProtocolModule = "farming",
                    PoolAddress = "0xethpool456",
                    GroupId = "group-eth-456",
                    Name = "ETH/USDC Pool",
                    PositionType = DeFiPositionDataType.Farming,
                    Label = "Farming",
                    TotalValueUsd = 810m, // 800 staked + 10 rewards
                    UnclaimedValueUsd = 10m,
                    Tokens = new List<DeFiToken>
                    {
                        // Staked token
                        new DeFiToken
                        {
                            Name = "UNI-V3 LP",
                            Symbol = "UNI-V3-LP",
                            ContractAddress = "0xunilp456",
                            Decimals = 18,
                            TokenType = DeFiTokenType.DeFiToken,
                            Balance = 1m,
                            BalanceFormatted = "1.0",
                            UsdPrice = 800m,
                            UsdValue = 800m
                        },
                        // Reward token
                        new DeFiToken
                        {
                            Name = "Uniswap",
                            Symbol = "UNI",
                            ContractAddress = "0xuni456",
                            Decimals = 18,
                            TokenType = DeFiTokenType.Reward,
                            Balance = 2m,
                            BalanceFormatted = "2.0",
                            UsdPrice = 5m,
                            UsdValue = 10m
                        }
                    },
                    Details = new DeFiPositionDetails
                    {
                        StakedCount = 1,
                        RewardsCount = 1,
                        StakedValueUsd = 800m,
                        RewardsValueUsd = 10m
                    }
                });
            }

            return Task.FromResult(positions);
        }

        public async Task<Dictionary<BlockchainNetwork, List<DeFiPositionData>>> GetMultiNetworkPositionsAsync(
            string walletAddress,
            IEnumerable<BlockchainNetwork> networks,
            CancellationToken cancellationToken = default)
        {
            var result = new Dictionary<BlockchainNetwork, List<DeFiPositionData>>();

            foreach (var network in networks)
            {
                var positions = await GetPositionsAsync(walletAddress, network, cancellationToken);
                result[network] = positions;
            }

            return result;
        }
    }

    /// <summary>
    /// Mock price enrichment service that returns positions unchanged.
    /// This is for testing purposes where the mock provider already includes prices.
    /// </summary>
    private class MockDeFiPriceEnrichmentService : DeFiPriceEnrichmentService
    {
        public MockDeFiPriceEnrichmentService()
            : base(
                null!, // AlchemyService not needed - we override EnrichWithPricesAsync
                CreateMockTokenVerificationService(),
                CreateMockVerifiedTokenRepository(),
                CreateMockUnlistedTokenRepository(),
                NullLogger<DeFiPriceEnrichmentService>.Instance)
        {
        }

        private static TokenVerificationService CreateMockTokenVerificationService()
        {
            // Create a minimal mock that satisfies constructor requirements
            var mockVerifiedRepo = CreateMockVerifiedTokenRepository();
            var mockUnlistedRepo = CreateMockUnlistedTokenRepository();

            // Create mock CoinMarketCapService - needs HttpClient, IOptions<CoinMarketCapOptions>, ILogger
            var mockHttpClient = new HttpClient();
            var mockCmcOptions = Options.Create(new CoinMarketCapOptions { ApiKey = "test-key" });
            var mockCmcService = new CoinMarketCapService(
                mockHttpClient,
                mockCmcOptions,
                NullLogger<CoinMarketCapService>.Instance);

            var mockScopeFactory = new Mock<IServiceScopeFactory>();

            return new TokenVerificationService(
                mockVerifiedRepo,
                mockUnlistedRepo,
                mockCmcService,
                mockScopeFactory.Object,
                NullLogger<TokenVerificationService>.Instance);
        }

        private static IVerifiedTokenRepository CreateMockVerifiedTokenRepository()
        {
            var mock = new Mock<IVerifiedTokenRepository>();

            // Setup to return empty dictionary for any network
            mock.Setup(x => x.GetVerifiedTokensAsync(It.IsAny<BlockchainNetwork>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Dictionary<string, VerifiedTokenCacheEntry>());

            return mock.Object;
        }

        private static UnlistedTokenRepository CreateMockUnlistedTokenRepository()
        {
            // Create mock dependencies for UnlistedTokenRepository
            var mockScopeFactory = new Mock<IServiceScopeFactory>();
            var mockDistributedCache = new Mock<IDistributedCache>();
            var mockCacheOptions = Options.Create(new CacheOptions());

            // Create actual DistributedCacheService instance with mocks
            var cacheService = new DistributedCacheService(
                mockDistributedCache.Object,
                NullLogger<DistributedCacheService>.Instance,
                mockCacheOptions);

            return new UnlistedTokenRepository(
                mockScopeFactory.Object,
                cacheService,
                mockCacheOptions,
                NullLogger<UnlistedTokenRepository>.Instance);
        }

        public override Task<List<DeFiPositionData>> EnrichWithPricesAsync(
            List<DeFiPositionData> positions,
            BlockchainNetwork network,
            CancellationToken cancellationToken = default)
        {
            // Return positions unchanged - test data already has prices
            return Task.FromResult(positions);
        }
    }

    private static INetworkMetadataRepository CreateMockNetworkMetadataRepository()
    {
        var mock = new Mock<INetworkMetadataRepository>();

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

    private static IOptions<DeFiProviderOptions> CreateMockDeFiOptions()
    {
        var options = new DeFiProviderOptions
        {
            Provider = DeFiProvider.Zerion,
            SupportedNetworks = new[] { "Ethereum", "Base", "Polygon" }
        };

        return Options.Create(options);
    }
}
