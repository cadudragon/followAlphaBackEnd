using TrackFi.Domain.Enums;

namespace TrackFi.Domain.Interfaces;

/// <summary>
/// Contract for fetching DeFi positions from external data providers.
/// Implemented by Infrastructure layer (e.g., MoralisService, ZerionService).
/// </summary>
public interface IDeFiDataProvider
{
    /// <summary>
    /// Fetches DeFi positions for a wallet on a specific network.
    /// </summary>
    /// <param name="walletAddress">Wallet address.</param>
    /// <param name="network">Blockchain network.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of DeFi positions.</returns>
    Task<List<DeFiPositionData>> GetPositionsAsync(
        string walletAddress,
        BlockchainNetwork network,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Fetches DeFi positions across multiple networks in parallel.
    /// </summary>
    /// <param name="walletAddress">Wallet address.</param>
    /// <param name="networks">List of networks to query.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Dictionary mapping network to positions.</returns>
    Task<Dictionary<BlockchainNetwork, List<DeFiPositionData>>> GetMultiNetworkPositionsAsync(
        string walletAddress,
        IEnumerable<BlockchainNetwork> networks,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Represents a DeFi position from external data providers (lending, borrowing, liquidity pool, etc.).
/// </summary>
public class DeFiPositionData
{
    public required string Id { get; init; }
    public required string ProtocolName { get; init; }
    public required string ProtocolId { get; init; }
    public string? ProtocolUrl { get; init; }
    public string? ProtocolLogo { get; init; }

    /// <summary>
    /// Protocol module (e.g., "lending", "farming", "staking").
    /// Used to categorize positions.
    /// </summary>
    public string? ProtocolModule { get; init; }

    /// <summary>
    /// Pool/contract address for this position.
    /// </summary>
    public string? PoolAddress { get; init; }

    /// <summary>
    /// Group ID to link related positions together (e.g., staked + rewards in farming).
    /// </summary>
    public string? GroupId { get; init; }

    /// <summary>
    /// Human-readable name for this position.
    /// </summary>
    public string? Name { get; init; }

    public required DeFiPositionDataType PositionType { get; init; }
    public required string Label { get; init; }
    public decimal TotalValueUsd { get; init; }
    public decimal? UnclaimedValueUsd { get; init; }
    public decimal? Apy { get; init; }
    public List<DeFiToken> Tokens { get; init; } = new();
    public DeFiPositionDetails? Details { get; init; }
    public DeFiAccountData? AccountData { get; init; }
    public ProjectedEarnings? ProjectedEarnings { get; init; }

    /// <summary>
    /// Indicates if this position contains any unverified tokens.
    /// When true, position uses provider's original pricing instead of global pricing layer.
    /// </summary>
    public bool HasUnverifiedTokens { get; init; }

    /// <summary>
    /// Indicates if this position is disconnected from our global pricing layer (Alchemy).
    /// Same as HasUnverifiedTokens - provided for clarity in frontend usage.
    /// </summary>
    public bool IsDisconnectedFromGlobalPricing { get; init; }
}

/// <summary>
/// Type of DeFi position from external data providers.
/// </summary>
public enum DeFiPositionDataType
{
    Supplied,
    Borrowed,
    Liquidity,
    Staked,
    Farming,
    Yield,
    Reward,
    Vested,
    Locked,
    Other
}

/// <summary>
/// Represents a token within a DeFi position.
/// </summary>
public class DeFiToken
{
    public required string Name { get; init; }
    public required string Symbol { get; init; }
    public required string ContractAddress { get; init; }
    public required int Decimals { get; init; }
    public required DeFiTokenType TokenType { get; init; }
    public required decimal Balance { get; init; }
    public required string BalanceFormatted { get; init; }
    public decimal? UsdPrice { get; init; }
    public decimal? UsdValue { get; init; }
    public string? Logo { get; init; }

    /// <summary>
    /// Indicates if token is verified in our verification layer (CMC + whitelist).
    /// Verified tokens use Alchemy pricing, unverified tokens use provider's original pricing.
    /// </summary>
    public bool IsVerified { get; init; }

    /// <summary>
    /// Indicates if token is in our unlisted/scam token database.
    /// </summary>
    public bool IsUnlisted { get; init; }

    /// <summary>
    /// Source of the USD price: "Alchemy" (global pricing layer) or "Zerion" (provider fallback).
    /// </summary>
    public string? PriceSource { get; init; }
}

/// <summary>
/// Type of token within a DeFi position.
/// </summary>
public enum DeFiTokenType
{
    Supplied,
    Borrowed,
    Reward,
    DeFiToken,
    Underlying
}

/// <summary>
/// Position-specific details (lending, borrowing, etc.).
/// Includes aggregation metadata for provider-aggregated positions.
/// </summary>
public class DeFiPositionDetails
{
    public string? Market { get; init; }
    public bool IsDebt { get; init; }
    public bool IsVariableDebt { get; init; }
    public bool IsStableDebt { get; init; }
    public decimal? Apy { get; init; }
    public bool? IsEnabledAsCollateral { get; init; }
    public ProjectedEarnings? ProjectedEarnings { get; init; }

    // Aggregation metadata for farming positions
    public int? StakedCount { get; init; }
    public int? RewardsCount { get; init; }
    public decimal? StakedValueUsd { get; init; }
    public decimal? RewardsValueUsd { get; init; }

    // Aggregation metadata for lending positions
    public decimal? SuppliedValueUsd { get; init; }
    public decimal? BorrowedValueUsd { get; init; }
    public decimal? NetValueUsd { get; init; }
}

/// <summary>
/// Account-level data for lending protocols (health factor, etc.).
/// </summary>
public class DeFiAccountData
{
    public decimal? NetApy { get; init; }
    public decimal? HealthFactor { get; init; }
}

/// <summary>
/// Projected earnings from a position.
/// </summary>
public class ProjectedEarnings
{
    public decimal? Daily { get; init; }
    public decimal? Weekly { get; init; }
    public decimal? Monthly { get; init; }
    public decimal? Yearly { get; init; }
}
