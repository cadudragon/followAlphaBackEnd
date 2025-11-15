using NetZerion.Models.Enums;
using NetZerion.Models.Responses;

namespace NetZerion.Clients;

/// <summary>
/// Client for wallet-related Zerion API endpoints.
/// </summary>
public interface IWalletClient
{
    /// <summary>
    /// Get DeFi positions (LP, Staking, Lending, etc.) for a wallet on one or more chains.
    /// Makes a single API call with comma-separated chain IDs for better performance.
    /// </summary>
    /// <param name="address">Wallet address (0x...).</param>
    /// <param name="chainIds">Blockchain networks (one or more).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of DeFi positions across all specified chains.</returns>
    Task<PositionsResponse> GetPositionsAsync(
        string address,
        IEnumerable<ChainId> chainIds,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all fungible tokens (ERC-20, native coins) with prices and metadata.
    /// </summary>
    /// <param name="address">Wallet address.</param>
    /// <param name="chainId">Blockchain network.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Portfolio of fungible tokens.</returns>
    Task<PortfolioResponse> GetPortfolioAsync(
        string address,
        ChainId chainId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get positions across multiple chains in parallel.
    /// </summary>
    /// <param name="address">Wallet address.</param>
    /// <param name="chainIds">List of chains to query.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Dictionary of positions per chain.</returns>
    Task<Dictionary<ChainId, PositionsResponse>> GetMultiChainPositionsAsync(
        string address,
        IEnumerable<ChainId> chainIds,
        CancellationToken cancellationToken = default);
}
