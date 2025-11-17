using TrackFi.Application.Portfolio.DTOs;

namespace TrackFi.Application.Interfaces;

/// <summary>
/// Contract for fetching portfolio data from external providers (Zerion, Moralis, etc.).
/// Provider is responsible for:
/// 1. Fetching data from external API
/// 2. Aggregating positions (e.g., grouping by group_id for farming)
/// 3. Categorizing positions (Farming, Lending, Staking, etc.)
/// 4. Transforming to standardized DTOs
/// This abstraction allows swapping providers without changing business logic (plug & play).
/// </summary>
public interface IPortfolioProvider
{
    /// <summary>
    /// Get wallet positions only (tokens in wallet).
    /// Provider filter: only_simple (Zerion) or equivalent
    /// </summary>
    /// <param name="walletAddress">The wallet address to fetch positions for</param>
    /// <param name="networks">List of network names to fetch from (empty = ALL)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Wallet portfolio with tokens grouped by network</returns>
    Task<MultiNetworkWalletDto> GetWalletPositionsAsync(
        string walletAddress,
        List<string> networks,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get DeFi positions only (lending, staking, farming, liquidity pools, etc.).
    /// Provider filter: only_complex (Zerion) or equivalent
    /// Provider is responsible for aggregation and categorization.
    /// </summary>
    /// <param name="walletAddress">The wallet address to fetch positions for</param>
    /// <param name="networks">List of network names to fetch from (empty = ALL)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>DeFi portfolio with positions categorized by type (Farming, Lending, etc.)</returns>
    Task<MultiNetworkDeFiPortfolioDto> GetDeFiPositionsAsync(
        string walletAddress,
        List<string> networks,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get full portfolio (wallet + DeFi positions combined).
    /// Provider filter: no_filter (Zerion) or equivalent
    /// </summary>
    /// <param name="walletAddress">The wallet address to fetch positions for</param>
    /// <param name="networks">List of network names to fetch from (empty = ALL)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Combined portfolio with wallet tokens + categorized DeFi positions</returns>
    Task<FullPortfolioDto> GetFullPortfolioAsync(
        string walletAddress,
        List<string> networks,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Full portfolio DTO combining wallet tokens + DeFi positions.
/// </summary>
public class FullPortfolioDto
{
    public string WalletAddress { get; set; } = string.Empty;
    public decimal TotalValueUsd { get; set; }
    public List<NetworkFullPortfolioDto> Networks { get; set; } = [];
}

/// <summary>
/// Full portfolio for a specific network.
/// </summary>
public class NetworkFullPortfolioDto
{
    public string Network { get; set; } = string.Empty;
    public string? NetworkLogoUrl { get; set; }
    public decimal TotalValueUsd { get; set; }

    // Wallet tokens
    public List<TokenBalanceDto> WalletTokens { get; set; } = [];

    // DeFi positions (categorized)
    public List<FarmingPositionDto> Farming { get; set; } = [];
    public List<LendingPositionDto> Lending { get; set; } = [];
    public List<LiquidityPoolPositionDto> LiquidityPools { get; set; } = [];
    public List<StakingPositionDto> Staking { get; set; } = [];
    public List<YieldPositionDto> Yield { get; set; } = [];
    public List<RewardsPositionDto> Rewards { get; set; } = [];
    public List<VaultPositionDto> Vaults { get; set; } = [];
}
