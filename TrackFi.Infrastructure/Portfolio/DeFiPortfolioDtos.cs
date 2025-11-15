namespace TrackFi.Infrastructure.Portfolio;

/// <summary>
/// Aggregated DeFi portfolio with positions grouped by category (matching DeBank structure).
/// </summary>
public class DeFiPortfolioDto
{
    public string WalletAddress { get; set; } = string.Empty;
    public string Network { get; set; } = string.Empty;
    public string? NetworkLogoUrl { get; set; }
    public decimal TotalValueUsd { get; set; }

    // Category-based position lists
    public List<FarmingPositionDto> Farming { get; set; } = new();
    public List<LendingPositionDto> Lending { get; set; } = new();
    public List<LiquidityPoolPositionDto> LiquidityPools { get; set; } = new();
    public List<StakingPositionDto> Staking { get; set; } = new();
    public List<YieldPositionDto> Yield { get; set; } = new();
    public List<RewardsPositionDto> Rewards { get; set; } = new();
    public List<VaultPositionDto> Vaults { get; set; } = new();
}

/// <summary>
/// Farming position (e.g., PancakeSwap V3 Farming, Uniswap V3 Farming).
/// Aggregates staked LP tokens and their farming rewards.
/// </summary>
public class FarmingPositionDto
{
    public string Id { get; set; } = string.Empty;
    public string Protocol { get; set; } = string.Empty;
    public string ProtocolId { get; set; } = string.Empty;
    public string? ProtocolUrl { get; set; }
    public string? ProtocolLogo { get; set; }
    public string PoolName { get; set; } = string.Empty;
    public string PoolAddress { get; set; } = string.Empty;

    /// <summary>
    /// Total value of staked assets + unclaimed rewards.
    /// </summary>
    public decimal TotalValueUsd { get; set; }

    /// <summary>
    /// Value of staked LP position.
    /// </summary>
    public decimal StakedValueUsd { get; set; }

    /// <summary>
    /// Value of unclaimed rewards.
    /// </summary>
    public decimal RewardsValueUsd { get; set; }

    /// <summary>
    /// Assets that are staked in the LP.
    /// </summary>
    public List<TokenDto> StakedAssets { get; set; } = new();

    /// <summary>
    /// Farming rewards (can include multiple tokens like CAKE, protocol tokens, fees).
    /// </summary>
    public List<TokenDto> RewardAssets { get; set; } = new();

    /// <summary>
    /// Indicates if this position contains any unverified tokens.
    /// </summary>
    public bool HasUnverifiedTokens { get; set; }

    /// <summary>
    /// Indicates if this position is disconnected from our global pricing layer (Alchemy).
    /// </summary>
    public bool IsDisconnectedFromGlobalPricing { get; set; }
}

/// <summary>
/// Lending position (e.g., Aave, Compound).
/// Includes both supplied (deposits) and borrowed (loans) positions.
/// </summary>
public class LendingPositionDto
{
    public string Id { get; set; } = string.Empty;
    public string Protocol { get; set; } = string.Empty;
    public string ProtocolId { get; set; } = string.Empty;
    public string? ProtocolUrl { get; set; }
    public string? ProtocolLogo { get; set; }
    public string PoolName { get; set; } = string.Empty;
    public string PoolAddress { get; set; } = string.Empty;

    /// <summary>
    /// Net value (supplied - borrowed).
    /// </summary>
    public decimal NetValueUsd { get; set; }

    /// <summary>
    /// Total supplied value.
    /// </summary>
    public decimal SuppliedValueUsd { get; set; }

    /// <summary>
    /// Total borrowed value.
    /// </summary>
    public decimal BorrowedValueUsd { get; set; }

    /// <summary>
    /// Health factor (1.0 = at risk, >1.5 = healthy).
    /// </summary>
    public decimal? HealthFactor { get; set; }

    /// <summary>
    /// Net APY (supply APY - borrow APY).
    /// </summary>
    public decimal? NetApy { get; set; }

    /// <summary>
    /// Supplied assets (deposits).
    /// </summary>
    public List<LendingAssetDto> SuppliedAssets { get; set; } = new();

    /// <summary>
    /// Borrowed assets (loans).
    /// </summary>
    public List<LendingAssetDto> BorrowedAssets { get; set; } = [];

    /// <summary>
    /// Projected earnings.
    /// </summary>
    public ProjectedEarningsDto? ProjectedEarnings { get; set; }

    /// <summary>
    /// Indicates if this position contains any unverified tokens.
    /// </summary>
    public bool HasUnverifiedTokens { get; set; }

    /// <summary>
    /// Indicates if this position is disconnected from our global pricing layer (Alchemy).
    /// </summary>
    public bool IsDisconnectedFromGlobalPricing { get; set; }
}

/// <summary>
/// Lending-specific asset with APY and collateral info.
/// </summary>
public class LendingAssetDto
{
    public string Symbol { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string ContractAddress { get; set; } = string.Empty;
    public string Balance { get; set; } = string.Empty;
    public decimal? UsdPrice { get; set; }
    public decimal? UsdValue { get; set; }
    public string? Logo { get; set; }

    /// <summary>
    /// APY for this specific asset.
    /// </summary>
    public decimal? Apy { get; set; }

    /// <summary>
    /// Whether this asset is enabled as collateral.
    /// </summary>
    public bool? IsCollateral { get; set; }

    /// <summary>
    /// Whether this is a debt position.
    /// </summary>
    public bool IsDebt { get; set; }

    /// <summary>
    /// Whether this is variable debt.
    /// </summary>
    public bool IsVariableDebt { get; set; }
}

/// <summary>
/// Liquidity pool position (e.g., Uniswap V2/V3, Curve).
/// </summary>
public class LiquidityPoolPositionDto
{
    public string Id { get; set; } = string.Empty;
    public string Protocol { get; set; } = string.Empty;
    public string ProtocolId { get; set; } = string.Empty;
    public string? ProtocolUrl { get; set; }
    public string? ProtocolLogo { get; set; }
    public string PoolName { get; set; } = string.Empty;
    public string PoolAddress { get; set; } = string.Empty;
    public decimal TotalValueUsd { get; set; }

    /// <summary>
    /// Pool tokens (e.g., WETH, USDC in a WETH/USDC pool).
    /// </summary>
    public List<TokenDto> PoolTokens { get; set; } = new();

    /// <summary>
    /// LP token representing the position.
    /// </summary>
    public TokenDto? LpToken { get; set; }

    /// <summary>
    /// Indicates if this position contains any unverified tokens.
    /// </summary>
    public bool HasUnverifiedTokens { get; set; }

    /// <summary>
    /// Indicates if this position is disconnected from our global pricing layer (Alchemy).
    /// </summary>
    public bool IsDisconnectedFromGlobalPricing { get; set; }
}

/// <summary>
/// Staking position (e.g., ETH 2.0 staking, protocol token staking).
/// </summary>
public class StakingPositionDto
{
    public string Id { get; set; } = string.Empty;
    public string Protocol { get; set; } = string.Empty;
    public string ProtocolId { get; set; } = string.Empty;
    public string? ProtocolUrl { get; set; }
    public string? ProtocolLogo { get; set; }
    public decimal TotalValueUsd { get; set; }
    public decimal? Apy { get; set; }

    /// <summary>
    /// Staked assets.
    /// </summary>
    public List<TokenDto> StakedAssets { get; set; } = new();

    /// <summary>
    /// Staking rewards.
    /// </summary>
    public List<TokenDto> Rewards { get; set; } = new();

    /// <summary>
    /// Indicates if this position contains any unverified tokens.
    /// </summary>
    public bool HasUnverifiedTokens { get; set; }

    /// <summary>
    /// Indicates if this position is disconnected from our global pricing layer (Alchemy).
    /// </summary>
    public bool IsDisconnectedFromGlobalPricing { get; set; }
}

/// <summary>
/// Standalone rewards position (e.g., claimable governance tokens, airdrops).
/// </summary>
public class RewardsPositionDto
{
    public string Id { get; set; } = string.Empty;
    public string Protocol { get; set; } = string.Empty;
    public string ProtocolId { get; set; } = string.Empty;
    public string? ProtocolUrl { get; set; }
    public string? ProtocolLogo { get; set; }
    public decimal TotalValueUsd { get; set; }

    /// <summary>
    /// Claimable rewards.
    /// </summary>
    public List<TokenDto> Rewards { get; set; } = new();

    /// <summary>
    /// Indicates if this position contains any unverified tokens.
    /// </summary>
    public bool HasUnverifiedTokens { get; set; }

    /// <summary>
    /// Indicates if this position is disconnected from our global pricing layer (Alchemy).
    /// </summary>
    public bool IsDisconnectedFromGlobalPricing { get; set; }
}

/// <summary>
/// Vault position (e.g., Yearn vaults, Beefy vaults).
/// </summary>
public class VaultPositionDto
{
    public string Id { get; set; } = string.Empty;
    public string Protocol { get; set; } = string.Empty;
    public string ProtocolId { get; set; } = string.Empty;
    public string? ProtocolUrl { get; set; }
    public string? ProtocolLogo { get; set; }
    public string VaultName { get; set; } = string.Empty;
    public decimal TotalValueUsd { get; set; }
    public decimal? Apy { get; set; }

    /// <summary>
    /// Deposited assets.
    /// </summary>
    public List<TokenDto> DepositedAssets { get; set; } = new();

    /// <summary>
    /// Indicates if this position contains any unverified tokens.
    /// </summary>
    public bool HasUnverifiedTokens { get; set; }

    /// <summary>
    /// Indicates if this position is disconnected from our global pricing layer (Alchemy).
    /// </summary>
    public bool IsDisconnectedFromGlobalPricing { get; set; }
}

/// <summary>
/// Yield position (e.g., Avantis yield pools, yield-bearing deposits).
/// Simple deposit positions that generate yield without aggregation.
/// </summary>
public class YieldPositionDto
{
    public string Id { get; set; } = string.Empty;
    public string Protocol { get; set; } = string.Empty;
    public string ProtocolId { get; set; } = string.Empty;
    public string? ProtocolUrl { get; set; }
    public string? ProtocolLogo { get; set; }
    public string PoolName { get; set; } = string.Empty;
    public string PoolAddress { get; set; } = string.Empty;
    public decimal TotalValueUsd { get; set; }
    public decimal? Apy { get; set; }

    /// <summary>
    /// Deposited yield-bearing assets.
    /// </summary>
    public List<TokenDto> DepositedAssets { get; set; } = new();

    /// <summary>
    /// Indicates if this position contains any unverified tokens.
    /// </summary>
    public bool HasUnverifiedTokens { get; set; }

    /// <summary>
    /// Indicates if this position is disconnected from our global pricing layer (Alchemy).
    /// </summary>
    public bool IsDisconnectedFromGlobalPricing { get; set; }
}

/// <summary>
/// Token within a DeFi position.
/// </summary>
public class TokenDto
{
    public string Symbol { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string ContractAddress { get; set; } = string.Empty;
    public string Balance { get; set; } = string.Empty;
    public decimal? UsdPrice { get; set; }
    public decimal? UsdValue { get; set; }
    public string? Logo { get; set; }

    /// <summary>
    /// Indicates if token is verified in our verification layer (CMC + whitelist).
    /// </summary>
    public bool IsVerified { get; set; }

    /// <summary>
    /// Indicates if token is in our unlisted/scam token database.
    /// </summary>
    public bool IsUnlisted { get; set; }

    /// <summary>
    /// Source of the USD price: "Alchemy" (global pricing layer) or "Zerion" (provider fallback).
    /// </summary>
    public string? PriceSource { get; set; }
}

/// <summary>
/// Projected earnings.
/// </summary>
public class ProjectedEarningsDto
{
    public decimal? Daily { get; set; }
    public decimal? Weekly { get; set; }
    public decimal? Monthly { get; set; }
    public decimal? Yearly { get; set; }
}

/// <summary>
/// Multi-network DeFi portfolio.
/// </summary>
public class MultiNetworkDeFiPortfolioDto
{
    public string WalletAddress { get; set; } = string.Empty;
    public decimal TotalValueUsd { get; set; }
    public List<NetworkDeFiPortfolioDto> Networks { get; set; } = [];
}

/// <summary>
/// DeFi portfolio for a specific network within multi-network response.
/// </summary>
public class NetworkDeFiPortfolioDto
{
    public string Network { get; set; } = string.Empty;
    public string? NetworkLogoUrl { get; set; }
    public decimal TotalValueUsd { get; set; }

    // Category-based position lists
    public List<FarmingPositionDto> Farming { get; set; } = [];
    public List<LendingPositionDto> Lending { get; set; } = [];
    public List<LiquidityPoolPositionDto> LiquidityPools { get; set; } = [];
    public List<StakingPositionDto> Staking { get; set; } = [];
    public List<YieldPositionDto> Yield { get; set; } = [];
    public List<RewardsPositionDto> Rewards { get; set; } = [];
    public List<VaultPositionDto> Vaults { get; set; } = [];
}
