using Microsoft.Extensions.Logging;
using NetZerion.Clients;
using NetZerion.Models.Enums;
using TrackFi.Domain.Enums;
using TrackFi.Domain.Interfaces;

namespace TrackFi.Infrastructure.DeFi;

/// <summary>
/// Zerion DeFi data provider implementation using NetZerion wrapper.
/// </summary>
public class ZerionService(
    IWalletClient walletClient,
    ILogger<ZerionService> logger) : IDeFiDataProvider
{
    private readonly IWalletClient _walletClient = walletClient ?? throw new ArgumentNullException(nameof(walletClient));
    private readonly ILogger<ZerionService> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    /// <inheritdoc />
    public async Task<List<DeFiPositionData>> GetPositionsAsync(
        string walletAddress,
        BlockchainNetwork network,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(walletAddress))
            throw new ArgumentException("Wallet address is required", nameof(walletAddress));

        var chainId = MapNetworkToChainId(network);
        var response = await _walletClient.GetPositionsAsync(walletAddress, new[] { chainId }, cancellationToken);

        var positions = response.Data.Select(MapToPositionData).ToList();

        // Aggregate Zerion-specific grouped positions (farming with group_id, etc.)
        return AggregateZerionPositions(positions);
    }

    /// <inheritdoc />
    public async Task<Dictionary<BlockchainNetwork, List<DeFiPositionData>>> GetMultiNetworkPositionsAsync(
        string walletAddress,
        IEnumerable<BlockchainNetwork> networks,
        CancellationToken cancellationToken = default)
    {
        var networkList = networks?.ToList() ?? throw new ArgumentNullException(nameof(networks));

        _logger.LogInformation(
            "Fetching DeFi positions from Zerion for {Wallet} across {Count} networks",
            walletAddress,
            networkList.Count);

        var chainIds = networkList.Select(MapNetworkToChainId).ToList();
        var response = await _walletClient.GetMultiChainPositionsAsync(
            walletAddress,
            chainIds,
            cancellationToken);

        var result = new Dictionary<BlockchainNetwork, List<DeFiPositionData>>();

        foreach (var kvp in response)
        {
            var network = MapChainIdToNetwork(kvp.Key);
            var positions = kvp.Value.Data.Select(MapToPositionData).ToList();

            // Aggregate Zerion-specific grouped positions (farming with group_id, etc.)
            var aggregatedPositions = AggregateZerionPositions(positions);

            result[network] = aggregatedPositions;
        }

        return result;
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
            TotalValueUsd = 0, // Calculated after price enrichment
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
        // Check if we have fungible info with a name that indicates the protocol
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

        // Default to Unknown
        return ("Unknown", "unknown", "Other");
    }

    /// <summary>
    /// Maps NetZerion Fungible to DeFiToken with position type context.
    /// Note: UsdPrice and UsdValue are set to null - prices will be enriched from Alchemy.
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
            Balance = fungible.Balance, // Use formatted balance, not raw wei/satoshis
            BalanceFormatted = fungible.Balance.ToString(),
            UsdPrice = fungible.PriceUsd, //Prices enriched from Alchemy later
            UsdValue = fungible.ValueUsd, //fCalculated after price enrichment
            Logo = fungible.IconUrl
        };
    }

    /// <summary>
    /// Determines token type from position type context.
    /// This is more reliable than symbol-based heuristics.
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
        // Check for yield module first - yield deposits should be mapped to Yield type
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

    /// <summary>
    /// Maps protocol type string to DeFiPositionDataType (deprecated - use MapNetZerionPositionType instead).
    /// </summary>
    private static DeFiPositionDataType MapPositionType(string type)
    {
        return type?.ToLowerInvariant() switch
        {
            "supplied" => DeFiPositionDataType.Supplied,
            "borrowed" => DeFiPositionDataType.Borrowed,
            "liquidity" => DeFiPositionDataType.Liquidity,
            "staked" => DeFiPositionDataType.Staked,
            "farming" => DeFiPositionDataType.Farming,
            _ => DeFiPositionDataType.Other
        };
    }

    /// <summary>
    /// Maps TrackFi BlockchainNetwork to NetZerion ChainId.
    /// Supports all Zerion DeFi-enabled networks that exist in BlockchainNetwork enum.
    /// </summary>
    private ChainId MapNetworkToChainId(BlockchainNetwork network) => network switch
    {
        // Major DeFi networks
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

        // L2 networks with DeFi support
        BlockchainNetwork.ZkSync => ChainId.ZkSyncEra,
        BlockchainNetwork.Scroll => ChainId.Scroll,
        BlockchainNetwork.Linea => ChainId.Linea,
        BlockchainNetwork.Blast => ChainId.Blast,
        BlockchainNetwork.Mantle => ChainId.Mantle,
        BlockchainNetwork.Unichain => ChainId.Unichain,

        // Additional networks with DeFi
        BlockchainNetwork.Aurora => ChainId.Aurora,
        BlockchainNetwork.DegenChain => ChainId.DegenChain,

        _ => throw new NotSupportedException($"Network {network} is not supported by Zerion DeFi")
    };

    /// <summary>
    /// Maps NetZerion ChainId to TrackFi BlockchainNetwork.
    /// Supports all Zerion DeFi-enabled networks that exist in BlockchainNetwork enum.
    /// </summary>
    private static BlockchainNetwork MapChainIdToNetwork(ChainId chainId)
    {
        return chainId switch
        {
            // Major DeFi networks
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

            // L2 networks with DeFi support
            ChainId.ZkSyncEra => BlockchainNetwork.ZkSync,
            ChainId.Scroll => BlockchainNetwork.Scroll,
            ChainId.Linea => BlockchainNetwork.Linea,
            ChainId.Blast => BlockchainNetwork.Blast,
            ChainId.Mantle => BlockchainNetwork.Mantle,
            ChainId.Unichain => BlockchainNetwork.Unichain,

            // Additional networks with DeFi
            ChainId.Aurora => BlockchainNetwork.Aurora,
            ChainId.DegenChain => BlockchainNetwork.DegenChain,

            // Unsupported networks (exist in NetZerion but not in BlockchainNetwork enum)
            // Abstract, ApeChain, Berachain, GravityAlpha, HyperEVM, Ink, Katana, Lens,
            // Soneium, Sonic, Wonder, XDC, Zero, ZKcandy
            _ => throw new NotSupportedException($"ChainId {chainId} is not mapped to BlockchainNetwork enum")
        };
    }

    /// <summary>
    /// Aggregates Zerion-specific grouped positions (e.g., farming positions with group_id).
    /// This is Zerion-specific logic that shouldn't be in the generic service layer.
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
    /// Combines multiple staked positions (WETH, USDC) and rewards into single logical positions.
    /// </summary>
    private List<DeFiPositionData> AggregateFarmingByGroupId(List<DeFiPositionData> positions)
    {
        return positions
            .Where(p => !string.IsNullOrEmpty(p.GroupId))
            .GroupBy(p => p.GroupId!)
            .Select(group =>
            {
                // Get all staked and reward positions in this group
                var stakedPositions = group.Where(p => p.PositionType == DeFiPositionDataType.Staked).ToList();
                var rewardPositions = group.Where(p => p.PositionType == DeFiPositionDataType.Reward).ToList();

                if (stakedPositions.Count == 0)
                {
                    _logger.LogWarning("Farming group {GroupId} has no staked positions, skipping", group.Key);
                    return null;
                }

                var firstStaked = stakedPositions.First();

                // Create aggregated position combining all staked tokens
                // Note: TotalValueUsd and other USD values will be calculated after price enrichment
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
                    PositionType = DeFiPositionDataType.Farming, // Mark as aggregated farming
                    Label = "Farming",
                    TotalValueUsd = 0, // Calculated after price enrichment
                    UnclaimedValueUsd = null, // Calculated after price enrichment
                    Apy = firstStaked.Apy,
                    // Combine all tokens from staked and reward positions
                    Tokens = stakedPositions.SelectMany(p => p.Tokens)
                        .Concat(rewardPositions.SelectMany(p => p.Tokens))
                        .ToList(),
                    Details = new DeFiPositionDetails
                    {
                        StakedValueUsd = null, // Calculated after price enrichment
                        RewardsValueUsd = null, // Calculated after price enrichment
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
    /// Combines supplied (deposit) and borrowed (loan) positions from the same group.
    /// </summary>
    private List<DeFiPositionData> AggregateLendingByGroupId(List<DeFiPositionData> positions)
    {
        return positions
            .Where(p => !string.IsNullOrEmpty(p.GroupId))
            .GroupBy(p => p.GroupId!)
            .Select(group =>
            {
                // Get all supplied and borrowed positions in this group
                var suppliedPositions = group.Where(p => p.PositionType == DeFiPositionDataType.Supplied).ToList();
                var borrowedPositions = group.Where(p => p.PositionType == DeFiPositionDataType.Borrowed).ToList();

                if (!suppliedPositions.Any() && !borrowedPositions.Any())
                {
                    _logger.LogWarning("Lending group {GroupId} has no positions, skipping", group.Key);
                    return null;
                }

                var firstPosition = suppliedPositions.Count != 0 ? suppliedPositions.First() : borrowedPositions.First();

                _logger.LogDebug(
                    "Aggregating lending group {GroupId}: Protocol={Protocol}, TokenCount={TokenCount}",
                    group.Key,
                    firstPosition.ProtocolName,
                    suppliedPositions.Sum(p => p.Tokens.Count) + borrowedPositions.Sum(p => p.Tokens.Count));

                // Create aggregated lending position
                // Note: USD values will be calculated after price enrichment
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
                    PositionType = DeFiPositionDataType.Supplied, // Mark as lending
                    Label = "Lending",
                    TotalValueUsd = 0, // Calculated after price enrichment
                    Apy = firstPosition.Apy,
                    // Combine all tokens from supplied and borrowed
                    Tokens = suppliedPositions.SelectMany(p => p.Tokens)
                        .Concat(borrowedPositions.SelectMany(p => p.Tokens))
                        .ToList(),
                    Details = new DeFiPositionDetails
                    {
                        SuppliedValueUsd = null, // Calculated after price enrichment
                        BorrowedValueUsd = null, // Calculated after price enrichment
                        NetValueUsd = null // Calculated after price enrichment
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
                // Get all staked and reward positions in this group
                var stakedPositions = group.Where(p => p.PositionType == DeFiPositionDataType.Staked).ToList();
                var rewardPositions = group.Where(p => p.PositionType == DeFiPositionDataType.Reward).ToList();

                if (stakedPositions.Count == 0)
                {
                    _logger.LogWarning("Staking group {GroupId} has no staked positions, skipping", group.Key);
                    return null;
                }

                var firstStaked = stakedPositions.First();

                _logger.LogDebug(
                    "Aggregating staking group {GroupId}: Protocol={Protocol}, TokenCount={TokenCount}",
                    group.Key,
                    firstStaked.ProtocolName,
                    stakedPositions.Sum(p => p.Tokens.Count) + rewardPositions.Sum(p => p.Tokens.Count));

                // Create aggregated staking position
                // Note: TotalValueUsd and other USD values will be calculated after price enrichment
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
                    PositionType = DeFiPositionDataType.Staked, // Keep as staked
                    Label = "Staking",
                    TotalValueUsd = 0, // Calculated after price enrichment
                    UnclaimedValueUsd = null, // Calculated after price enrichment
                    Apy = firstStaked.Apy,
                    // Combine all tokens from staked and reward positions
                    Tokens = stakedPositions.SelectMany(p => p.Tokens)
                        .Concat(rewardPositions.SelectMany(p => p.Tokens))
                        .ToList(),
                    Details = new DeFiPositionDetails
                    {
                        StakedValueUsd = null, // Calculated after price enrichment
                        RewardsValueUsd = null, // Calculated after price enrichment
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
}
