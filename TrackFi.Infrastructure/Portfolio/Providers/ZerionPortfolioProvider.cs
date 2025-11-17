using Microsoft.Extensions.Logging;
using NetZerion.Clients;
using NetZerion.Models.Enums;
using TrackFi.Application.Interfaces;
using TrackFi.Application.Portfolio.DTOs;
using TrackFi.Domain.Enums;
using TrackFi.Domain.Interfaces;

namespace TrackFi.Infrastructure.Portfolio.Providers;

/// <summary>
/// Zerion portfolio provider implementation using NetZerion wrapper.
/// Provider is responsible for:
/// 1. Fetching data from Zerion API with appropriate filters
/// 2. Aggregating positions (e.g., grouping by group_id for farming)
/// 3. Categorizing positions (Farming, Lending, Staking, etc.)
/// 4. Transforming to standardized DTOs
/// This allows plug-and-play swapping with other providers without changing business logic.
/// </summary>
public class ZerionPortfolioProvider : IPortfolioProvider
{
    private readonly IWalletClient _walletClient;
    private readonly INetworkMetadataRepository _networkMetadataRepository;
    private readonly ILogger<ZerionPortfolioProvider> _logger;

    public ZerionPortfolioProvider(
        IWalletClient walletClient,
        INetworkMetadataRepository networkMetadataRepository,
        ILogger<ZerionPortfolioProvider> logger)
    {
        _walletClient = walletClient ?? throw new ArgumentNullException(nameof(walletClient));
        _networkMetadataRepository = networkMetadataRepository ?? throw new ArgumentNullException(nameof(networkMetadataRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    #region IPortfolioProvider Implementation

    /// <inheritdoc />
    public async Task<MultiNetworkWalletDto> GetWalletPositionsAsync(
        string walletAddress,
        List<string> networks,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(walletAddress))
            throw new ArgumentException("Wallet address is required", nameof(walletAddress));

        var networkEnums = ParseNetworks(networks);

        _logger.LogInformation(
            "Fetching wallet positions from Zerion for {Wallet} Filtered {Count} networks",
            walletAddress,
            networkEnums.Count);

        // Fetch with filter=only_simple (wallet tokens only)
        var chainIds = networkEnums.Select(MapNetworkToChainId).ToList();
        var response = await _walletClient.GetPositionsAsync(
            walletAddress,
            chainIds,
            positionFilter: "only_simple",
            cancellationToken);

        // Load network metadata for logos
        var networkMetadata = await _networkMetadataRepository.GetAllAsync(cancellationToken);

        // Extract fungibles from positions and group by network
        var tokensByNetwork = response.Data
            .SelectMany(p => p.Assets.Select(f => (fungible: f, network: p.Chain?.Id)))
            .Where(item => !string.IsNullOrEmpty(item.network))
            .GroupBy(item => ParseNetworkFromString(item.network!))
            .Where(g => g.Key.HasValue)
            .ToDictionary(g => g.Key!.Value, g => g.Select(item => item.fungible).ToList());

        // Transform to NetworkWalletDto
        var networkWallets = new List<NetworkWalletDto>();
        foreach (var tokenNetWork in tokensByNetwork)
        {
            var network = tokenNetWork.Key;
            if (!tokensByNetwork.TryGetValue(network, out var tokens) || tokens.Count == 0)
                continue;

            var networkLogoUrl = networkMetadata.TryGetValue(network, out var metadata)
                ? metadata.LogoUrl
                : null;

            var tokenDtos = tokens
                .Select(f => MapFungibleToTokenBalanceDto(f, network))
                .Where(t => t.ValueUsd > 0) // Filter out zero-value tokens
                .OrderByDescending(t => t.ValueUsd)
                .ToList();

            if (tokenDtos.Count == 0)
                continue;

            var totalValue = tokenDtos.Sum(t => t.ValueUsd ?? 0);

            networkWallets.Add(new NetworkWalletDto
            {
                Network = network.ToString(),
                NetworkLogoUrl = networkLogoUrl,
                TotalValueUsd = totalValue,
                Tokens = tokenDtos,
                TokenCount = tokenDtos.Count
            });
        }

        var totalValueUsd = networkWallets.Sum(n => n.TotalValueUsd);
        var totalTokens = networkWallets.Sum(n => n.TokenCount);

        _logger.LogInformation(
            "Wallet positions fetched: {Wallet} has ${Value:N2} across {NetworkCount} networks with {TokenCount} tokens",
            walletAddress,
            totalValueUsd,
            networkWallets.Count,
            totalTokens);

        return new MultiNetworkWalletDto
        {
            WalletAddress = walletAddress,
            IsAnonymous = true,
            Summary = new WalletSummaryDto
            {
                TotalValueUsd = totalValueUsd,
                TotalTokens = totalTokens,
                LastUpdated = DateTime.UtcNow
            },
            Networks = networkWallets
        };
    }

    /// <inheritdoc />
    public async Task<MultiNetworkDeFiPortfolioDto> GetDeFiPositionsAsync(
        string walletAddress,
        List<string> networks,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(walletAddress))
            throw new ArgumentException("Wallet address is required", nameof(walletAddress));

        var networkEnums = ParseNetworks(networks);

        _logger.LogInformation(
            "Fetching DeFi positions from Zerion for {Wallet} across {Count} networks",
            walletAddress,
            networkEnums.Count);

        // Fetch with filter=only_complex (DeFi positions only)
        var chainIds = networkEnums.Select(MapNetworkToChainId).ToList();
        var response = await _walletClient.GetMultiChainPositionsAsync(
            walletAddress,
            chainIds,
            cancellationToken);

        // Load network metadata for logos
        var networkMetadata = await _networkMetadataRepository.GetAllAsync(cancellationToken);

        // Process each network: aggregate + categorize + transform
        var networkPortfolios = new List<NetworkDeFiPortfolioDto>();
        foreach (var kvp in response)
        {
            var network = MapChainIdToNetwork(kvp.Key);
            var positions = kvp.Value.Data.Select(MapToPositionData).ToList();

            // STEP 1: Aggregate positions (Zerion-specific group_id aggregation)
            var aggregatedPositions = AggregateZerionPositions(positions);

            if (aggregatedPositions.Count == 0)
                continue;

            // STEP 2: Categorize and transform to DTOs
            var networkLogoUrl = networkMetadata.TryGetValue(network, out var metadata)
                ? metadata.LogoUrl
                : null;

            var portfolioDto = TransformToPortfolioDto(walletAddress, network.ToString(), networkLogoUrl, aggregatedPositions);

            if (portfolioDto.TotalValueUsd == 0)
                continue;

            networkPortfolios.Add(new NetworkDeFiPortfolioDto
            {
                Network = network.ToString(),
                NetworkLogoUrl = networkLogoUrl,
                TotalValueUsd = portfolioDto.TotalValueUsd,
                Farming = portfolioDto.Farming,
                Lending = portfolioDto.Lending,
                LiquidityPools = portfolioDto.LiquidityPools,
                Staking = portfolioDto.Staking,
                Yield = portfolioDto.Yield,
                Rewards = portfolioDto.Rewards,
                Vaults = portfolioDto.Vaults
            });
        }

        var totalValue = networkPortfolios.Sum(n => n.TotalValueUsd);

        _logger.LogInformation(
            "DeFi positions fetched: {Wallet} has ${Value:N2} across {NetworkCount} networks",
            walletAddress,
            totalValue,
            networkPortfolios.Count);

        return new MultiNetworkDeFiPortfolioDto
        {
            WalletAddress = walletAddress,
            TotalValueUsd = totalValue,
            Networks = networkPortfolios
        };
    }

    /// <inheritdoc />
    public async Task<FullPortfolioDto> GetFullPortfolioAsync(
        string walletAddress,
        List<string> networks,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(walletAddress))
            throw new ArgumentException("Wallet address is required", nameof(walletAddress));

        var networkEnums = ParseNetworks(networks);

        _logger.LogInformation(
            "Fetching full portfolio from Zerion for {Wallet} across {Count} networks",
            walletAddress,
            networkEnums.Count);

        // Fetch with filter=no_filter (all positions)
        var chainIds = networkEnums.Select(MapNetworkToChainId).ToList();

        // Fetch both wallet tokens and DeFi positions in parallel
        var walletTask = _walletClient.GetPositionsAsync(walletAddress, chainIds, "only_simple", cancellationToken);
        var defiTask = _walletClient.GetMultiChainPositionsAsync(walletAddress, chainIds, cancellationToken);

        await Task.WhenAll(walletTask, defiTask);

        var walletResponse = await walletTask;
        var defiResponse = await defiTask;

        // Load network metadata for logos
        var networkMetadata = await _networkMetadataRepository.GetAllAsync(cancellationToken);

        // Group wallet tokens by network
        var tokensByNetwork = walletResponse.Data
            .SelectMany(p => p.Assets.Select(f => (fungible: f, network: p.Chain?.Id)))
            .Where(item => !string.IsNullOrEmpty(item.network))
            .GroupBy(item => ParseNetworkFromString(item.network!))
            .Where(g => g.Key.HasValue)
            .ToDictionary(g => g.Key!.Value, g => g.Select(item => item.fungible).ToList());

        // Process each network
        var fullPortfolios = new List<NetworkFullPortfolioDto>();
        foreach (var network in networkEnums)
        {
            var networkLogoUrl = networkMetadata.TryGetValue(network, out var metadata)
                ? metadata.LogoUrl
                : null;

            // Get wallet tokens for this network
            var walletTokens = new List<TokenBalanceDto>();
            if (tokensByNetwork.TryGetValue(network, out var tokens))
            {
                walletTokens = tokens
                    .Select(f => MapFungibleToTokenBalanceDto(f, network))
                    .Where(t => t.ValueUsd > 0)
                    .OrderByDescending(t => t.ValueUsd)
                    .ToList();
            }

            // Get DeFi positions for this network
            var farming = new List<FarmingPositionDto>();
            var lending = new List<LendingPositionDto>();
            var liquidityPools = new List<LiquidityPoolPositionDto>();
            var staking = new List<StakingPositionDto>();
            var yield = new List<YieldPositionDto>();
            var rewards = new List<RewardsPositionDto>();
            var vaults = new List<VaultPositionDto>();

            var chainId = MapNetworkToChainId(network);
            if (defiResponse.TryGetValue(chainId, out var positionsData))
            {
                var positions = positionsData.Data.Select(MapToPositionData).ToList();
                var aggregatedPositions = AggregateZerionPositions(positions);

                if (aggregatedPositions.Count > 0)
                {
                    var portfolioDto = TransformToPortfolioDto(walletAddress, network.ToString(), networkLogoUrl, aggregatedPositions);
                    farming = portfolioDto.Farming;
                    lending = portfolioDto.Lending;
                    liquidityPools = portfolioDto.LiquidityPools;
                    staking = portfolioDto.Staking;
                    yield = portfolioDto.Yield;
                    rewards = portfolioDto.Rewards;
                    vaults = portfolioDto.Vaults;
                }
            }

            // Calculate total value for network
            var totalValue = walletTokens.Sum(t => t.ValueUsd ?? 0) +
                            farming.Sum(p => p.TotalValueUsd) +
                            lending.Sum(p => p.NetValueUsd) +
                            liquidityPools.Sum(p => p.TotalValueUsd) +
                            staking.Sum(p => p.TotalValueUsd) +
                            yield.Sum(p => p.TotalValueUsd) +
                            rewards.Sum(p => p.TotalValueUsd) +
                            vaults.Sum(p => p.TotalValueUsd);

            if (totalValue == 0)
                continue;

            fullPortfolios.Add(new NetworkFullPortfolioDto
            {
                Network = network.ToString(),
                NetworkLogoUrl = networkLogoUrl,
                TotalValueUsd = totalValue,
                WalletTokens = walletTokens,
                Farming = farming,
                Lending = lending,
                LiquidityPools = liquidityPools,
                Staking = staking,
                Yield = yield,
                Rewards = rewards,
                Vaults = vaults
            });
        }

        var totalValueUsd = fullPortfolios.Sum(n => n.TotalValueUsd);

        _logger.LogInformation(
            "Full portfolio fetched: {Wallet} has ${Value:N2} across {NetworkCount} networks",
            walletAddress,
            totalValueUsd,
            fullPortfolios.Count);

        return new FullPortfolioDto
        {
            WalletAddress = walletAddress,
            TotalValueUsd = totalValueUsd,
            Networks = fullPortfolios
        };
    }

    #endregion

    #region Mapping: Zerion → Domain Models

    /// <summary>
    /// Maps NetZerion Fungible to TokenBalanceDto.
    /// </summary>
    private TokenBalanceDto MapFungibleToTokenBalanceDto(NetZerion.Models.Entities.Fungible fungible, BlockchainNetwork network)
    {
        return new TokenBalanceDto
        {
            ContractAddress = fungible.Address,
            Network = network.ToString(),
            Symbol = fungible.Symbol,
            Name = fungible.Name,
            Balance = fungible.Balance.ToString(),
            Decimals = fungible.Decimals,
            BalanceFormatted = fungible.Balance,
            Price = fungible.PriceUsd.HasValue ? new PriceInfoDto { Usd = fungible.PriceUsd.Value } : null,
            ValueUsd = fungible.ValueUsd,
            LogoUrl = fungible.IconUrl
        };
    }

    /// <summary>
    /// Maps NetZerion Position to DeFiPositionData.
    /// </summary>
    private DeFiPositionData MapToPositionData(NetZerion.Models.Entities.Position position)
    {
        // Use actual position type from Zerion API with protocol_module context
        var positionType = MapNetZerionPositionType(position.Type, position.ProtocolModule);

        // Extract protocol info from token metadata as fallback
        var protocolInfo = ExtractProtocolFromMetadata(position);

        return new DeFiPositionData
        {
            Id = position.Id,
            ProtocolName = position.Protocol?.Name ?? protocolInfo.Name,
            ProtocolId = position.Protocol?.Id ?? protocolInfo.Id,
            ProtocolUrl = position.Protocol?.WebsiteUrl,
            ProtocolLogo = position.Protocol?.IconUrl,
            ProtocolModule = position.ProtocolModule,
            PoolAddress = position.PoolAddress,
            GroupId = position.GroupId,
            Name = position.Name,
            PositionType = positionType,
            Label = position.Type.ToString(),
            TotalValueUsd = position.ValueUsd,
            UnclaimedValueUsd = null,
            Apy = position.Apy,
            Tokens = position.Assets.Select(a => MapToToken(a, positionType)).ToList(),
            Details = null,
            AccountData = position.HealthFactor.HasValue ? new DeFiAccountData
            {
                HealthFactor = position.HealthFactor
            } : null,
            ProjectedEarnings = null
        };
    }

    /// <summary>
    /// Extracts protocol information from token metadata.
    /// </summary>
    private (string Name, string Id, string Type) ExtractProtocolFromMetadata(NetZerion.Models.Entities.Position position)
    {
        var fungibleName = position.Assets.FirstOrDefault()?.Name ?? "";
        var fungibleSymbol = position.Assets.FirstOrDefault()?.Symbol ?? "";

        // Aave detection
        if (fungibleName.Contains("Aave", StringComparison.OrdinalIgnoreCase) ||
            fungibleSymbol.StartsWith("aBase", StringComparison.OrdinalIgnoreCase) ||
            fungibleSymbol.StartsWith("aEth", StringComparison.OrdinalIgnoreCase) ||
            fungibleSymbol.Contains("Debt", StringComparison.OrdinalIgnoreCase))
        {
            var isDebt = fungibleSymbol.Contains("Debt", StringComparison.OrdinalIgnoreCase);
            return ("Aave", "aave", isDebt ? "Borrowed" : "Supplied");
        }

        // Compound detection
        if (fungibleName.Contains("Compound", StringComparison.OrdinalIgnoreCase) ||
            fungibleSymbol.StartsWith("c", StringComparison.OrdinalIgnoreCase) && fungibleSymbol.Length > 1)
        {
            return ("Compound", "compound", "Supplied");
        }

        // Uniswap/PancakeSwap LP detection
        if (fungibleSymbol.Contains("LP", StringComparison.OrdinalIgnoreCase) ||
            fungibleSymbol.Contains("UNI-V", StringComparison.OrdinalIgnoreCase))
        {
            if (fungibleName.Contains("PancakeSwap", StringComparison.OrdinalIgnoreCase))
                return ("PancakeSwap", "pancakeswap", "Liquidity");

            return ("Uniswap", "uniswap", "Liquidity");
        }

        return ("Unknown", "unknown", "Other");
    }

    /// <summary>
    /// Maps NetZerion Fungible to DeFiToken with position type context.
    /// </summary>
    private static DeFiToken MapToToken(NetZerion.Models.Entities.Fungible fungible, DeFiPositionDataType positionType)
    {
        return new DeFiToken
        {
            Name = fungible.Name,
            Symbol = fungible.Symbol,
            ContractAddress = fungible.Address,
            Decimals = fungible.Decimals,
            TokenType = DetermineTokenTypeFromPosition(positionType),
            Balance = fungible.Balance,
            BalanceFormatted = fungible.Balance.ToString(),
            UsdPrice = fungible.PriceUsd,
            UsdValue = fungible.ValueUsd,
            Logo = fungible.IconUrl
        };
    }

    /// <summary>
    /// Determines token type from position type context.
    /// </summary>
    private static DeFiTokenType DetermineTokenTypeFromPosition(DeFiPositionDataType positionType)
    {
        return positionType switch
        {
            DeFiPositionDataType.Borrowed => DeFiTokenType.Borrowed,
            DeFiPositionDataType.Supplied => DeFiTokenType.Supplied,
            DeFiPositionDataType.Staked => DeFiTokenType.DeFiToken,
            DeFiPositionDataType.Reward => DeFiTokenType.Reward,
            DeFiPositionDataType.Liquidity => DeFiTokenType.DeFiToken,
            DeFiPositionDataType.Yield => DeFiTokenType.DeFiToken,
            _ => DeFiTokenType.DeFiToken
        };
    }

    /// <summary>
    /// Maps NetZerion PositionType to DeFiPositionDataType with protocol_module context.
    /// </summary>
    private static DeFiPositionDataType MapNetZerionPositionType(
        NetZerion.Models.Enums.PositionType type,
        string? protocolModule = null)
    {
        // Check for yield module first
        if (protocolModule?.Equals("yield", StringComparison.OrdinalIgnoreCase) == true &&
            type == PositionType.Deposit)
        {
            return DeFiPositionDataType.Yield;
        }

        return type switch
        {
            PositionType.Deposit => DeFiPositionDataType.Supplied,
            PositionType.Lending => DeFiPositionDataType.Supplied,
            PositionType.Borrowing => DeFiPositionDataType.Borrowed,
            PositionType.LiquidityPool => DeFiPositionDataType.Liquidity,
            PositionType.Staked => DeFiPositionDataType.Staked,
            PositionType.Staking => DeFiPositionDataType.Staked,
            PositionType.Reward => DeFiPositionDataType.Reward,
            PositionType.Farming => DeFiPositionDataType.Farming,
            PositionType.Claimable => DeFiPositionDataType.Reward,
            PositionType.Vesting => DeFiPositionDataType.Vested,
            PositionType.Locked => DeFiPositionDataType.Locked,
            _ => DeFiPositionDataType.Other
        };
    }

    #endregion

    #region Aggregation Logic (Zerion-Specific)

    /// <summary>
    /// Aggregates Zerion-specific grouped positions (e.g., farming positions with group_id).
    /// </summary>
    private List<DeFiPositionData> AggregateZerionPositions(List<DeFiPositionData> positions)
    {
        var result = new List<DeFiPositionData>();

        // Group positions by protocol_module
        var positionsByModule = positions
            .GroupBy(p => p.ProtocolModule?.ToLowerInvariant() ?? "other")
            .ToDictionary(g => g.Key, g => g.ToList());

        // Handle farming positions - aggregate by group_id
        if (positionsByModule.TryGetValue("farming", out var farmingPositions))
        {
            var aggregated = AggregateFarmingByGroupId(farmingPositions);
            result.AddRange(aggregated);
            positionsByModule.Remove("farming");
        }

        // Handle lending positions - aggregate by group_id
        if (positionsByModule.TryGetValue("lending", out var lendingPositions))
        {
            var aggregated = AggregateLendingByGroupId(lendingPositions);
            result.AddRange(aggregated);
            positionsByModule.Remove("lending");
        }

        // Handle staking positions - aggregate by group_id
        if (positionsByModule.TryGetValue("staked", out var stakingPositions))
        {
            var aggregated = AggregateStakingByGroupId(stakingPositions);
            result.AddRange(aggregated);
            positionsByModule.Remove("staked");
        }

        // Add remaining positions as-is
        foreach (var kvp in positionsByModule)
        {
            result.AddRange(kvp.Value);
        }

        return result;
    }

    /// <summary>
    /// Aggregates farming positions by group_id (Zerion-specific).
    /// Combines multiple staked positions and rewards into single logical positions.
    /// </summary>
    private List<DeFiPositionData> AggregateFarmingByGroupId(List<DeFiPositionData> positions)
    {
        return positions
            .Where(p => !string.IsNullOrEmpty(p.GroupId))
            .GroupBy(p => p.GroupId!)
            .Select(group =>
            {
                var stakedPositions = group.Where(p => p.PositionType == DeFiPositionDataType.Staked).ToList();
                var rewardPositions = group.Where(p => p.PositionType == DeFiPositionDataType.Reward).ToList();

                if (stakedPositions.Count == 0)
                {
                    _logger.LogWarning("Farming group {GroupId} has no staked positions, skipping", group.Key);
                    return null;
                }

                var firstStaked = stakedPositions.First();
                var stakedValueUsd = stakedPositions.Sum(p => p.TotalValueUsd);
                var rewardsValueUsd = rewardPositions.Sum(p => p.TotalValueUsd);

                return new DeFiPositionData
                {
                    Id = firstStaked.Id,
                    ProtocolName = firstStaked.ProtocolName,
                    ProtocolId = firstStaked.ProtocolId,
                    ProtocolUrl = firstStaked.ProtocolUrl,
                    ProtocolLogo = firstStaked.ProtocolLogo,
                    ProtocolModule = firstStaked.ProtocolModule,
                    PoolAddress = firstStaked.PoolAddress,
                    GroupId = firstStaked.GroupId,
                    Name = firstStaked.Name,
                    PositionType = DeFiPositionDataType.Farming,
                    Label = "Farming",
                    TotalValueUsd = stakedValueUsd + rewardsValueUsd,
                    UnclaimedValueUsd = rewardsValueUsd,
                    Apy = firstStaked.Apy,
                    Tokens = stakedPositions.SelectMany(p => p.Tokens)
                        .Concat(rewardPositions.SelectMany(p => p.Tokens))
                        .ToList(),
                    Details = new DeFiPositionDetails
                    {
                        StakedValueUsd = stakedValueUsd,
                        RewardsValueUsd = rewardsValueUsd,
                        StakedCount = stakedPositions.Sum(p => p.Tokens.Count),
                        RewardsCount = rewardPositions.Sum(p => p.Tokens.Count)
                    },
                    AccountData = firstStaked.AccountData,
                    ProjectedEarnings = firstStaked.ProjectedEarnings
                };
            })
            .Where(p => p != null)
            .Cast<DeFiPositionData>()
            .ToList();
    }

    /// <summary>
    /// Aggregates lending positions by group_id (Zerion-specific).
    /// Combines supplied and borrowed positions from the same group.
    /// </summary>
    private List<DeFiPositionData> AggregateLendingByGroupId(List<DeFiPositionData> positions)
    {
        return positions
            .Where(p => !string.IsNullOrEmpty(p.GroupId))
            .GroupBy(p => p.GroupId!)
            .Select(group =>
            {
                var suppliedPositions = group.Where(p => p.PositionType == DeFiPositionDataType.Supplied).ToList();
                var borrowedPositions = group.Where(p => p.PositionType == DeFiPositionDataType.Borrowed).ToList();

                if (!suppliedPositions.Any() && !borrowedPositions.Any())
                {
                    _logger.LogWarning("Lending group {GroupId} has no positions, skipping", group.Key);
                    return null;
                }

                var firstPosition = suppliedPositions.Count != 0 ? suppliedPositions.First() : borrowedPositions.First();
                var suppliedValueUsd = suppliedPositions.Sum(p => p.TotalValueUsd);
                var borrowedValueUsd = borrowedPositions.Sum(p => p.TotalValueUsd);
                var netValueUsd = suppliedValueUsd - borrowedValueUsd;

                return new DeFiPositionData
                {
                    Id = firstPosition.Id,
                    ProtocolName = firstPosition.ProtocolName,
                    ProtocolId = firstPosition.ProtocolId,
                    ProtocolUrl = firstPosition.ProtocolUrl,
                    ProtocolLogo = firstPosition.ProtocolLogo,
                    ProtocolModule = firstPosition.ProtocolModule,
                    GroupId = firstPosition.GroupId,
                    Name = firstPosition.Name,
                    PositionType = DeFiPositionDataType.Supplied,
                    Label = "Lending",
                    TotalValueUsd = netValueUsd,
                    Apy = firstPosition.Apy,
                    Tokens = suppliedPositions.SelectMany(p => p.Tokens)
                        .Concat(borrowedPositions.SelectMany(p => p.Tokens))
                        .ToList(),
                    Details = new DeFiPositionDetails
                    {
                        SuppliedValueUsd = suppliedValueUsd,
                        BorrowedValueUsd = borrowedValueUsd,
                        NetValueUsd = netValueUsd
                    },
                    AccountData = firstPosition.AccountData,
                    ProjectedEarnings = firstPosition.ProjectedEarnings
                };
            })
            .Where(p => p != null)
            .Cast<DeFiPositionData>()
            .ToList();
    }

    /// <summary>
    /// Aggregates staking positions by group_id (Zerion-specific).
    /// Combines staked positions and their rewards into single logical positions.
    /// </summary>
    private List<DeFiPositionData> AggregateStakingByGroupId(List<DeFiPositionData> positions)
    {
        return positions
            .Where(p => !string.IsNullOrEmpty(p.GroupId))
            .GroupBy(p => p.GroupId!)
            .Select(group =>
            {
                var stakedPositions = group.Where(p => p.PositionType == DeFiPositionDataType.Staked).ToList();
                var rewardPositions = group.Where(p => p.PositionType == DeFiPositionDataType.Reward).ToList();

                if (stakedPositions.Count == 0)
                {
                    _logger.LogWarning("Staking group {GroupId} has no staked positions, skipping", group.Key);
                    return null;
                }

                var firstStaked = stakedPositions.First();
                var stakedValueUsd = stakedPositions.Sum(p => p.TotalValueUsd);
                var rewardsValueUsd = rewardPositions.Sum(p => p.TotalValueUsd);

                return new DeFiPositionData
                {
                    Id = firstStaked.Id,
                    ProtocolName = firstStaked.ProtocolName,
                    ProtocolId = firstStaked.ProtocolId,
                    ProtocolUrl = firstStaked.ProtocolUrl,
                    ProtocolLogo = firstStaked.ProtocolLogo,
                    ProtocolModule = firstStaked.ProtocolModule,
                    PoolAddress = firstStaked.PoolAddress,
                    GroupId = firstStaked.GroupId,
                    Name = firstStaked.Name,
                    PositionType = DeFiPositionDataType.Staked,
                    Label = "Staking",
                    TotalValueUsd = stakedValueUsd + rewardsValueUsd,
                    UnclaimedValueUsd = rewardsValueUsd,
                    Apy = firstStaked.Apy,
                    Tokens = stakedPositions.SelectMany(p => p.Tokens)
                        .Concat(rewardPositions.SelectMany(p => p.Tokens))
                        .ToList(),
                    Details = new DeFiPositionDetails
                    {
                        StakedValueUsd = stakedValueUsd,
                        RewardsValueUsd = rewardsValueUsd,
                        StakedCount = stakedPositions.Sum(p => p.Tokens.Count),
                        RewardsCount = rewardPositions.Sum(p => p.Tokens.Count)
                    },
                    AccountData = firstStaked.AccountData,
                    ProjectedEarnings = firstStaked.ProjectedEarnings
                };
            })
            .Where(p => p != null)
            .Cast<DeFiPositionData>()
            .ToList();
    }

    #endregion

    #region Transformation Logic (Domain → DTOs)

    /// <summary>
    /// Transforms provider-aggregated positions into category-based DTO structure.
    /// </summary>
    private DeFiPortfolioDto TransformToPortfolioDto(
        string walletAddress,
        string network,
        string? networkLogoUrl,
        List<DeFiPositionData> positions)
    {
        _logger.LogDebug(
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
                    if (position.Label == "Lending")
                    {
                        portfolio.Lending.Add(TransformToLendingDto(position));
                    }
                    break;

                case DeFiPositionDataType.Yield:
                    portfolio.Yield.Add(TransformToYieldDto(position));
                    break;

                case DeFiPositionDataType.Staked:
                    if (position.Label == "Staking")
                    {
                        portfolio.Staking.Add(TransformToStakingDto(position));
                    }
                    break;

                case DeFiPositionDataType.Liquidity:
                    // TODO: Transform to LiquidityPoolPositionDto
                    break;
            }
        }

        _logger.LogDebug(
            "Portfolio transformed: {Farming} farming, {Lending} lending, {Staking} staking, {Yield} yield",
            portfolio.Farming.Count,
            portfolio.Lending.Count,
            portfolio.Staking.Count,
            portfolio.Yield.Count);

        return portfolio;
    }

    /// <summary>
    /// Transforms an aggregated farming position to FarmingPositionDto.
    /// </summary>
    private FarmingPositionDto TransformToFarmingDto(DeFiPositionData position)
    {
        var stakedCount = position.Details?.StakedCount ?? 0;
        var rewardsCount = position.Details?.RewardsCount ?? 0;
        var stakedValueUsd = position.Details?.StakedValueUsd ?? 0m;
        var rewardsValueUsd = position.Details?.RewardsValueUsd ?? 0m;

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
            NetValueUsd = position.TotalValueUsd,
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
            HasUnverifiedTokens = position.HasUnverifiedTokens,
            IsDisconnectedFromGlobalPricing = position.IsDisconnectedFromGlobalPricing
        };
    }

    /// <summary>
    /// Transforms an aggregated staking position to StakingPositionDto.
    /// </summary>
    private StakingPositionDto TransformToStakingDto(DeFiPositionData position)
    {
        var stakedCount = position.Details?.StakedCount ?? 0;
        var rewardsCount = position.Details?.RewardsCount ?? 0;

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
            Apy = null,
            IsCollateral = null,
            IsDebt = token.TokenType == DeFiTokenType.Borrowed,
            IsVariableDebt = false
        };
    }

    #endregion

    #region Network Mapping

    /// <summary>
    /// Maps TrackFi BlockchainNetwork to NetZerion ChainId.
    /// </summary>
    private ChainId MapNetworkToChainId(BlockchainNetwork network) => network switch
    {
        BlockchainNetwork.Ethereum => ChainId.Ethereum,
        BlockchainNetwork.Polygon => ChainId.Polygon,
        BlockchainNetwork.Arbitrum => ChainId.Arbitrum,
        BlockchainNetwork.Optimism => ChainId.Optimism,
        BlockchainNetwork.Base => ChainId.Base,
        BlockchainNetwork.BNBChain => ChainId.BinanceSmartChain,
        BlockchainNetwork.Avalanche => ChainId.Avalanche,
        BlockchainNetwork.Fantom => ChainId.Fantom,
        BlockchainNetwork.Gnosis => ChainId.Gnosis,
        BlockchainNetwork.Celo => ChainId.Celo,
        BlockchainNetwork.ZkSync => ChainId.ZkSyncEra,
        BlockchainNetwork.Scroll => ChainId.Scroll,
        BlockchainNetwork.Linea => ChainId.Linea,
        BlockchainNetwork.Blast => ChainId.Blast,
        BlockchainNetwork.Mantle => ChainId.Mantle,
        BlockchainNetwork.Unichain => ChainId.Unichain,
        BlockchainNetwork.Aurora => ChainId.Aurora,
        BlockchainNetwork.DegenChain => ChainId.DegenChain,
        _ => throw new NotSupportedException($"Network {network} is not supported by Zerion")
    };

    /// <summary>
    /// Maps NetZerion ChainId to TrackFi BlockchainNetwork.
    /// </summary>
    private static BlockchainNetwork MapChainIdToNetwork(ChainId chainId)
    {
        return chainId switch
        {
            ChainId.Ethereum => BlockchainNetwork.Ethereum,
            ChainId.Polygon => BlockchainNetwork.Polygon,
            ChainId.Arbitrum => BlockchainNetwork.Arbitrum,
            ChainId.Optimism => BlockchainNetwork.Optimism,
            ChainId.Base => BlockchainNetwork.Base,
            ChainId.BinanceSmartChain => BlockchainNetwork.BNBChain,
            ChainId.Avalanche => BlockchainNetwork.Avalanche,
            ChainId.Fantom => BlockchainNetwork.Fantom,
            ChainId.Gnosis => BlockchainNetwork.Gnosis,
            ChainId.Celo => BlockchainNetwork.Celo,
            ChainId.ZkSyncEra => BlockchainNetwork.ZkSync,
            ChainId.Scroll => BlockchainNetwork.Scroll,
            ChainId.Linea => BlockchainNetwork.Linea,
            ChainId.Blast => BlockchainNetwork.Blast,
            ChainId.Mantle => BlockchainNetwork.Mantle,
            ChainId.Unichain => BlockchainNetwork.Unichain,
            ChainId.Aurora => BlockchainNetwork.Aurora,
            ChainId.DegenChain => BlockchainNetwork.DegenChain,
            _ => throw new NotSupportedException($"ChainId {chainId} is not mapped to BlockchainNetwork enum")
        };
    }

    /// <summary>
    /// Parses network names to BlockchainNetwork enums.
    /// If empty list provided, returns default supported networks.
    /// </summary>
    private static List<BlockchainNetwork> ParseNetworks(List<string> networks)
    {
        // If empty, use default supported networks for Zerion
        if (networks == null || networks.Count == 0)
        {
           return [];
        }

        var result = networks
            .Select(n => Enum.TryParse<BlockchainNetwork>(n, ignoreCase: true, out var network) ? network : (BlockchainNetwork?)null)
            .Where(n => n.HasValue)
            .Select(n => n!.Value)
            .ToList();

        if (result.Count == 0)
            throw new ArgumentException("No valid networks found in provided list", nameof(networks));

        return result;
    }

    /// <summary>
    /// Parses network string from Chain.Id to BlockchainNetwork enum.
    /// </summary>
    private BlockchainNetwork? ParseNetworkFromString(string networkId)
    {
        if (string.IsNullOrWhiteSpace(networkId))
            return null;

        // Try parsing as ChainId first
        if (Enum.TryParse<ChainId>(networkId, ignoreCase: true, out var chainId))
        {
            try
            {
                return MapChainIdToNetwork(chainId);
            }
            catch
            {
                return null;
            }
        }

        // Try parsing directly as BlockchainNetwork
        if (Enum.TryParse<BlockchainNetwork>(networkId, ignoreCase: true, out var network))
        {
            return network;
        }

        return null;
    }
    #endregion
}
