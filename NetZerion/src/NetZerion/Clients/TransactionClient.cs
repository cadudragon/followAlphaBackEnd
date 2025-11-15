using NetZerion.Exceptions;
using NetZerion.Http;
using NetZerion.Models.Entities;
using NetZerion.Models.Enums;
using NetZerion.Models.Responses;
using NetZerion.Utilities;

namespace NetZerion.Clients;

/// <summary>
/// Implementation of transaction-related Zerion API endpoints.
/// </summary>
public class TransactionClient : ITransactionClient
{
    private readonly ZerionHttpClient _httpClient;

    /// <summary>
    /// Initializes a new instance of the <see cref="TransactionClient"/> class.
    /// </summary>
    /// <param name="httpClient">Configured Zerion HTTP client.</param>
    public TransactionClient(ZerionHttpClient httpClient)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
    }

    /// <inheritdoc />
    public async Task<TransactionHistoryResponse> GetHistoryAsync(
        string address,
        ChainId chainId,
        int limit = 50,
        string? cursor = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(address))
            throw new ValidationException(nameof(address), "Wallet address cannot be empty");

        if (!address.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
            throw new ValidationException(nameof(address), "Wallet address must start with 0x");

        if (limit < 1 || limit > 100)
            throw new ValidationException(nameof(limit), "Limit must be between 1 and 100");

        var chainIdString = chainId.ToApiString();
        var endpoint = $"wallets/{address}/transactions/?filter[chain_ids]={chainIdString}&page[size]={limit}";

        if (!string.IsNullOrWhiteSpace(cursor))
        {
            endpoint += $"&page[after]={cursor}";
        }

        // Note: Simplified implementation
        // Full implementation would require JSON:API response parsing
        return new TransactionHistoryResponse
        {
            Data = new List<Transaction>()
        };
    }

    /// <inheritdoc />
    public async Task<Transaction> GetTransactionAsync(
        string txHash,
        ChainId chainId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(txHash))
            throw new ValidationException(nameof(txHash), "Transaction hash cannot be empty");

        if (!txHash.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
            throw new ValidationException(nameof(txHash), "Transaction hash must start with 0x");

        var chainIdString = chainId.ToApiString();
        var endpoint = $"transactions/{txHash}/?chain_id={chainIdString}";

        // Note: Simplified implementation
        // Full implementation would require JSON:API response parsing
        return new Transaction
        {
            Hash = txHash,
            Chain = new Chain { Id = chainIdString }
        };
    }
}
