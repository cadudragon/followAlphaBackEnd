using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TrackFi.Domain.Enums;
using TrackFi.Domain.Interfaces;
using TrackFi.Infrastructure.DeFi;

namespace TrackFi.Infrastructure.Portfolio;

/// <summary>
/// Generic service for fetching DeFi positions from any data provider.
/// Transforms provider-aggregated positions into standardized DTOs.
/// Provider-specific aggregation logic (e.g., Zerion group_id) should be in the provider implementation.
/// Prices are enriched separately from Alchemy for dynamic pricing with different cache strategies.
/// </summary>
public class DeFiPortfolioService
{
    private readonly IDeFiDataProvider _dataProvider;
    private readonly DeFiPriceEnrichmentService _priceEnrichmentService;
    private readonly INetworkMetadataRepository _networkMetadataRepository;
    private readonly DeFiProviderOptions _defiOptions;
    private readonly ILogger<DeFiPortfolioService> _logger;

    public DeFiPortfolioService(
        IDeFiDataProvider dataProvider,
        DeFiPriceEnrichmentService priceEnrichmentService,
        INetworkMetadataRepository networkMetadataRepository,
        IOptions<DeFiProviderOptions> defiOptions,
        ILogger<DeFiPortfolioService> logger)
    {
        _dataProvider = dataProvider ?? throw new ArgumentNullException(nameof(dataProvider));
        _priceEnrichmentService = priceEnrichmentService ?? throw new ArgumentNullException(nameof(priceEnrichmentService));
        _networkMetadataRepository = networkMetadataRepository ?? throw new ArgumentNullException(nameof(networkMetadataRepository));
        _defiOptions = defiOptions?.Value ?? throw new ArgumentNullException(nameof(defiOptions));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Gets DeFi positions for a wallet on a specific network.
    /// Positions are enriched with current prices from Alchemy before transformation.
    /// </summary>
    /// <param name="walletAddress">Wallet address.</param>
    /// <param name="network">Blockchain network.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>DeFi portfolio with positions grouped by category.</returns>
    public async Task<DeFiPortfolioDto> GetDeFiPositionsAsync(
        string walletAddress,
        BlockchainNetwork network,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(walletAddress))
            throw new ArgumentException("Wallet address cannot be empty", nameof(walletAddress));

        // Fetch positions from provider (can be cached for 3 minutes)
        var positions = await _dataProvider.GetPositionsAsync(walletAddress, network, cancellationToken);

        // Enrich with current prices from Alchemy (1-minute cache)
        var enrichedPositions = await _priceEnrichmentService.EnrichWithPricesAsync(
            positions,
            network,
            cancellationToken);

        // Get network logo URL
        var networkMetadata = await _networkMetadataRepository.GetByNetworkAsync(network, cancellationToken);
        var networkLogoUrl = networkMetadata?.LogoUrl;

        return TransformToPortfolioDto(walletAddress, network.ToString(), networkLogoUrl, enrichedPositions);
    }

    /// <summary>
    /// Gets DeFi positions across all configured networks in parallel.
    /// Networks are read from appsettings DeFi:SupportedNetworks configuration.
    /// </summary>
    /// <param name="walletAddress">Wallet address.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Aggregated DeFi portfolio across all configured networks.</returns>
    public Task<MultiNetworkDeFiPortfolioDto> GetMultiNetworkDeFiPositionsAsync(
        string walletAddress,
        CancellationToken cancellationToken = default)
    {
        // Parse configured networks from appsettings
        var networks = _defiOptions.SupportedNetworks
            .Select(n => Enum.TryParse<BlockchainNetwork>(n, ignoreCase: true, out var network) ? network : (BlockchainNetwork?)null)
            .Where(n => n.HasValue)
            .Select(n => n!.Value)
            .ToList();

        if (!networks.Any())
        {
            _logger.LogWarning("No valid networks configured in DeFi:SupportedNetworks");
            throw new InvalidOperationException("No valid networks configured in DeFi:SupportedNetworks");
        }

        _logger.LogInformation(
            "Using {Count} networks from configuration: {Networks}",
            networks.Count,
            string.Join(", ", networks));

        return GetMultiNetworkDeFiPositionsAsync(walletAddress, networks, cancellationToken);
    }

    /// <summary>
    /// Gets DeFi positions across multiple networks in parallel.
    /// </summary>
    /// <param name="walletAddress">Wallet address.</param>
    /// <param name="networks">List of networks to query.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Aggregated DeFi portfolio across all networks.</returns>
    public async Task<MultiNetworkDeFiPortfolioDto> GetMultiNetworkDeFiPositionsAsync(
        string walletAddress,
        IEnumerable<BlockchainNetwork> networks,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(walletAddress))
            throw new ArgumentException("Wallet address cannot be empty", nameof(walletAddress));

        var networkList = networks?.ToList() ?? throw new ArgumentNullException(nameof(networks));
        if (!networkList.Any())
            throw new ArgumentException("At least one network must be provided", nameof(networks));

        _logger.LogInformation(
            "Fetching DeFi positions for wallet {Wallet} across {Count} networks",
            walletAddress,
            networkList.Count);

        // Fetch positions from provider (can be cached for 3 minutes)
        var response = await _dataProvider.GetMultiNetworkPositionsAsync(
            walletAddress,
            networkList,
            cancellationToken);

        // Load network metadata for logos
        var networkMetadata = await _networkMetadataRepository.GetAllAsync(cancellationToken);

        // Enrich positions with prices for each network in parallel
        var enrichmentTasks = response.Select(async kvp =>
        {
            var network = kvp.Key;
            var positions = kvp.Value;

            // Enrich with current prices from Alchemy (1-minute cache)
            var enrichedPositions = await _priceEnrichmentService.EnrichWithPricesAsync(
                positions,
                network,
                cancellationToken);

            // Get network logo URL
            var networkLogoUrl = networkMetadata.TryGetValue(network, out var metadata)
                ? metadata.LogoUrl
                : null;

            var portfolio = TransformToPortfolioDto(walletAddress, network.ToString(), networkLogoUrl, enrichedPositions);

            return new NetworkDeFiPortfolioDto
            {
                Network = network.ToString(),
                NetworkLogoUrl = networkLogoUrl,
                TotalValueUsd = portfolio.TotalValueUsd,
                Farming = portfolio.Farming,
                Lending = portfolio.Lending,
                LiquidityPools = portfolio.LiquidityPools,
                Staking = portfolio.Staking,
                Yield = portfolio.Yield,
                Rewards = portfolio.Rewards,
                Vaults = portfolio.Vaults
            };
        });

        // Await all enrichment tasks in parallel
        var allNetworkPortfolios = (await Task.WhenAll(enrichmentTasks)).ToList();

        // Filter out networks with no positions (zero value)
        var networkPortfolios = allNetworkPortfolios
            .Where(n => n.TotalValueUsd > 0)
            .ToList();

        var totalValue = networkPortfolios.Sum(n => n.TotalValueUsd);

        _logger.LogInformation(
            "Aggregated DeFi portfolio for {Wallet} with total value ${Value:N2} across {NetworkCount} networks (filtered from {TotalNetworks})",
            walletAddress,
            totalValue,
            networkPortfolios.Count,
            allNetworkPortfolios.Count);

        return new MultiNetworkDeFiPortfolioDto
        {
            WalletAddress = walletAddress,
            TotalValueUsd = totalValue,
            Networks = networkPortfolios
        };
    }

    /// <summary>
    /// Transforms provider-aggregated positions into category-based DTO structure.
    /// Provider-specific aggregation (e.g., Zerion group_id) should be done in the provider.
    /// </summary>
    private DeFiPortfolioDto TransformToPortfolioDto(
        string walletAddress,
        string network,
        string? networkLogoUrl,
        List<DeFiPositionData> positions)
    {
        _logger.LogInformation(
            "Transforming {Count} positions to DTO for wallet {Wallet} on {Network}",
            positions.Count,
            walletAddress,
            network);

        var portfolio = new DeFiPortfolioDto
        {
            WalletAddress = walletAddress,
            Network = network,
            NetworkLogoUrl = networkLogoUrl,
            TotalValueUsd = positions.Sum(p => p.TotalValueUsd)
        };

        // Transform positions to DTOs based on PositionType
        foreach (var position in positions)
        {
            switch (position.PositionType)
            {
                case DeFiPositionDataType.Farming:
                    portfolio.Farming.Add(TransformToFarmingDto(position));
                    break;

                case DeFiPositionDataType.Supplied:
                    // Check if this is an aggregated lending position
                    if (position.Label == "Lending")
                    {
                        portfolio.Lending.Add(TransformToLendingDto(position));
                    }
                    break;

                case DeFiPositionDataType.Yield:
                    portfolio.Yield.Add(TransformToYieldDto(position));
                    break;

                case DeFiPositionDataType.Staked:
                    // Check if this is an aggregated staking position
                    if (position.Label == "Staking")
                    {
                        portfolio.Staking.Add(TransformToStakingDto(position));
                    }
                    break;

                case DeFiPositionDataType.Liquidity:
                    // TODO: Transform to LiquidityPoolPositionDto
                    break;

                // Add other position types as needed
            }
        }

        _logger.LogInformation(
            "Portfolio transformed: {Farming} farming, {Lending} lending, {Staking} staking, {Yield} yield",
            portfolio.Farming.Count,
            portfolio.Lending.Count,
            portfolio.Staking.Count,
            portfolio.Yield.Count);

        return portfolio;
    }

    /// <summary>
    /// Transforms an aggregated farming position to FarmingPositionDto.
    /// Expects tokens to be separated by type (staked vs reward) using Details dictionary.
    /// </summary>
    private FarmingPositionDto TransformToFarmingDto(DeFiPositionData position)
    {
        // Extract staked and reward counts from Details
        var stakedCount = position.Details?.StakedCount ?? 0;
        var rewardsCount = position.Details?.RewardsCount ?? 0;
        var stakedValueUsd = position.Details?.StakedValueUsd ?? 0m;
        var rewardsValueUsd = position.Details?.RewardsValueUsd ?? 0m;

        // Split tokens into staked and reward assets
        var allTokens = position.Tokens.Select(MapToTokenDto).ToList();
        var stakedAssets = allTokens.Take(stakedCount).ToList();
        var rewardAssets = allTokens.Skip(stakedCount).Take(rewardsCount).ToList();

        return new FarmingPositionDto
        {
            Id = position.Id,
            Protocol = position.ProtocolName,
            ProtocolId = position.ProtocolId,
            ProtocolUrl = position.ProtocolUrl,
            ProtocolLogo = position.ProtocolLogo,
            PoolName = position.Name ?? position.Label,
            PoolAddress = position.PoolAddress ?? string.Empty,
            TotalValueUsd = position.TotalValueUsd,
            StakedValueUsd = stakedValueUsd,
            RewardsValueUsd = rewardsValueUsd,
            StakedAssets = stakedAssets,
            RewardAssets = rewardAssets,

            // Verification flags
            HasUnverifiedTokens = position.HasUnverifiedTokens,
            IsDisconnectedFromGlobalPricing = position.IsDisconnectedFromGlobalPricing
        };
    }

    /// <summary>
    /// Transforms an aggregated lending position to LendingPositionDto.
    /// </summary>
    private LendingPositionDto TransformToLendingDto(DeFiPositionData position)
    {
        var suppliedValue = position.Details?.SuppliedValueUsd ?? 0m;
        var borrowedValue = position.Details?.BorrowedValueUsd ?? 0m;

        // Provider should separate supplied vs borrowed tokens
        // For now, we'll use TokenType to differentiate
        var suppliedAssets = position.Tokens
            .Where(t => t.TokenType == DeFiTokenType.Supplied || t.TokenType == DeFiTokenType.DeFiToken)
            .Select(MapToLendingAssetDto)
            .ToList();

        var borrowedAssets = position.Tokens
            .Where(t => t.TokenType == DeFiTokenType.Borrowed)
            .Select(t =>
            {
                var asset = MapToLendingAssetDto(t);
                asset.IsDebt = true;
                return asset;
            })
            .ToList();

        return new LendingPositionDto
        {
            Id = position.Id,
            Protocol = position.ProtocolName,
            ProtocolId = position.ProtocolId,
            ProtocolUrl = position.ProtocolUrl,
            ProtocolLogo = position.ProtocolLogo,
            PoolName = position.Name ?? position.Label,
            PoolAddress = position.PoolAddress ?? string.Empty,
            NetValueUsd = position.TotalValueUsd, // Now correctly calculated as supplied - borrowed
            SuppliedValueUsd = suppliedValue,
            BorrowedValueUsd = borrowedValue,
            HealthFactor = position.AccountData?.HealthFactor,
            NetApy = position.AccountData?.NetApy,
            SuppliedAssets = suppliedAssets,
            BorrowedAssets = borrowedAssets,
            ProjectedEarnings = position.ProjectedEarnings != null ? new ProjectedEarningsDto
            {
                Daily = position.ProjectedEarnings.Daily,
                Weekly = position.ProjectedEarnings.Weekly,
                Monthly = position.ProjectedEarnings.Monthly,
                Yearly = position.ProjectedEarnings.Yearly
            } : null,

            // Verification flags
            HasUnverifiedTokens = position.HasUnverifiedTokens,
            IsDisconnectedFromGlobalPricing = position.IsDisconnectedFromGlobalPricing
        };
    }

    /// <summary>
    /// Transforms a yield position to YieldPositionDto.
    /// </summary>
    private YieldPositionDto TransformToYieldDto(DeFiPositionData position)
    {
        return new YieldPositionDto
        {
            Id = position.Id,
            Protocol = position.ProtocolName,
            ProtocolId = position.ProtocolId,
            ProtocolUrl = position.ProtocolUrl,
            ProtocolLogo = position.ProtocolLogo,
            PoolName = position.Name ?? position.Label,
            PoolAddress = position.PoolAddress ?? string.Empty,
            TotalValueUsd = position.TotalValueUsd,
            Apy = position.Apy,
            DepositedAssets = position.Tokens.Select(MapToTokenDto).ToList(),

            // Verification flags
            HasUnverifiedTokens = position.HasUnverifiedTokens,
            IsDisconnectedFromGlobalPricing = position.IsDisconnectedFromGlobalPricing
        };
    }

    /// <summary>
    /// Transforms an aggregated staking position to StakingPositionDto.
    /// Expects tokens to be separated by type (staked vs reward) using Details dictionary.
    /// </summary>
    private StakingPositionDto TransformToStakingDto(DeFiPositionData position)
    {
        // Extract staked and reward counts from Details
        var stakedCount = position.Details?.StakedCount ?? 0;
        var rewardsCount = position.Details?.RewardsCount ?? 0;

        // Split tokens into staked and reward assets
        var allTokens = position.Tokens.Select(MapToTokenDto).ToList();
        var stakedAssets = allTokens.Take(stakedCount).ToList();
        var rewardAssets = allTokens.Skip(stakedCount).Take(rewardsCount).ToList();

        return new StakingPositionDto
        {
            Id = position.Id,
            Protocol = position.ProtocolName,
            ProtocolId = position.ProtocolId,
            ProtocolUrl = position.ProtocolUrl,
            ProtocolLogo = position.ProtocolLogo,
            TotalValueUsd = position.TotalValueUsd,
            Apy = position.Apy,
            StakedAssets = stakedAssets,
            Rewards = rewardAssets,

            // Verification flags
            HasUnverifiedTokens = position.HasUnverifiedTokens,
            IsDisconnectedFromGlobalPricing = position.IsDisconnectedFromGlobalPricing
        };
    }

    /// <summary>
    /// Maps DeFiToken to TokenDto.
    /// </summary>
    private TokenDto MapToTokenDto(DeFiToken token)
    {
        return new TokenDto
        {
            Symbol = token.Symbol,
            Name = token.Name,
            ContractAddress = token.ContractAddress,
            Balance = token.BalanceFormatted,
            UsdPrice = token.UsdPrice,
            UsdValue = token.UsdValue,
            Logo = token.Logo,

            // Verification metadata
            IsVerified = token.IsVerified,
            IsUnlisted = token.IsUnlisted,
            PriceSource = token.PriceSource
        };
    }

    /// <summary>
    /// Maps DeFiToken to LendingAssetDto.
    /// </summary>
    private LendingAssetDto MapToLendingAssetDto(DeFiToken token)
    {
        return new LendingAssetDto
        {
            Symbol = token.Symbol,
            Name = token.Name,
            ContractAddress = token.ContractAddress,
            Balance = token.BalanceFormatted,
            UsdPrice = token.UsdPrice,
            UsdValue = token.UsdValue,
            Logo = token.Logo,
            Apy = null, // TODO: Extract from position details
            IsCollateral = null, // TODO: Extract from position details
            IsDebt = token.TokenType == DeFiTokenType.Borrowed,
            IsVariableDebt = false // TODO: Extract from position details
        };
    }
}
