using NetZerion.Models.Entities;
using NetZerion.Models.Enums;
using NetZerion.Models.Responses;

namespace NetZerion.Clients;

/// <summary>
/// Client for transaction-related Zerion API endpoints.
/// </summary>
public interface ITransactionClient
{
    /// <summary>
    /// Get transaction history for a wallet with decoded information.
    /// </summary>
    /// <param name="address">Wallet address.</param>
    /// <param name="chainId">Blockchain network.</param>
    /// <param name="limit">Number of transactions to return (default: 50, max: 100).</param>
    /// <param name="cursor">Pagination cursor from previous response.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Transaction history with pagination.</returns>
    Task<TransactionHistoryResponse> GetHistoryAsync(
        string address,
        ChainId chainId,
        int limit = 50,
        string? cursor = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get detailed information about a specific transaction.
    /// </summary>
    /// <param name="txHash">Transaction hash.</param>
    /// <param name="chainId">Blockchain network.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Transaction details.</returns>
    Task<Transaction> GetTransactionAsync(
        string txHash,
        ChainId chainId,
        CancellationToken cancellationToken = default);
}
