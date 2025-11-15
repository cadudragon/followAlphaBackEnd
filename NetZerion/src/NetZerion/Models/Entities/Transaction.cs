using NetZerion.Models.Enums;

namespace NetZerion.Models.Entities;

/// <summary>
/// Represents a blockchain transaction with decoded information.
/// </summary>
public class Transaction
{
    /// <summary>
    /// Transaction hash (unique identifier on-chain)
    /// </summary>
    public string Hash { get; set; } = string.Empty;

    /// <summary>
    /// Block number where the transaction was included
    /// </summary>
    public long BlockNumber { get; set; }

    /// <summary>
    /// Transaction timestamp (when it was mined)
    /// </summary>
    public DateTimeOffset Timestamp { get; set; }

    /// <summary>
    /// Type of transaction (Send, Swap, Stake, etc.)
    /// </summary>
    public TransactionType Type { get; set; }

    /// <summary>
    /// Transaction status
    /// </summary>
    public TransactionStatus Status { get; set; }

    /// <summary>
    /// Sender address
    /// </summary>
    public string From { get; set; } = string.Empty;

    /// <summary>
    /// Recipient address (contract or wallet)
    /// </summary>
    public string To { get; set; } = string.Empty;

    /// <summary>
    /// Human-readable description of the transaction
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Total value transferred in USD (if applicable)
    /// </summary>
    public decimal? ValueUsd { get; set; }

    /// <summary>
    /// Gas fee paid in native currency (e.g., ETH)
    /// </summary>
    public decimal GasFee { get; set; }

    /// <summary>
    /// Gas fee in USD
    /// </summary>
    public decimal? GasFeeUsd { get; set; }

    /// <summary>
    /// Transaction nonce
    /// </summary>
    public int Nonce { get; set; }

    /// <summary>
    /// Protocol involved in the transaction (if applicable)
    /// </summary>
    public Protocol? Protocol { get; set; }

    /// <summary>
    /// Chain where this transaction occurred
    /// </summary>
    public Chain? Chain { get; set; }

    /// <summary>
    /// Assets transferred in this transaction
    /// </summary>
    public List<TransactionTransfer> Transfers { get; set; } = new();

    /// <summary>
    /// Token approvals granted in this transaction
    /// </summary>
    public List<TransactionApproval> Approvals { get; set; } = new();

    /// <summary>
    /// Additional transaction metadata
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();

    /// <summary>
    /// Raw response data from Zerion API
    /// </summary>
    public object? RawData { get; set; }
}

/// <summary>
/// Represents an asset transfer within a transaction.
/// </summary>
public class TransactionTransfer
{
    /// <summary>
    /// Token being transferred
    /// </summary>
    public Fungible Token { get; set; } = new();

    /// <summary>
    /// Transfer direction relative to the wallet ("in" or "out")
    /// </summary>
    public string Direction { get; set; } = string.Empty;

    /// <summary>
    /// Amount transferred
    /// </summary>
    public decimal Quantity { get; set; }

    /// <summary>
    /// Value in USD at the time of transfer
    /// </summary>
    public decimal? ValueUsd { get; set; }

    /// <summary>
    /// Sender address
    /// </summary>
    public string Sender { get; set; } = string.Empty;

    /// <summary>
    /// Recipient address
    /// </summary>
    public string Recipient { get; set; } = string.Empty;
}

/// <summary>
/// Represents a token approval within a transaction.
/// </summary>
public class TransactionApproval
{
    /// <summary>
    /// Token being approved
    /// </summary>
    public Fungible Token { get; set; } = new();

    /// <summary>
    /// Spender address (contract allowed to spend tokens)
    /// </summary>
    public string Spender { get; set; } = string.Empty;

    /// <summary>
    /// Approved amount (or "unlimited" for max uint256)
    /// </summary>
    public string Amount { get; set; } = string.Empty;

    /// <summary>
    /// Indicates if this is an unlimited approval
    /// </summary>
    public bool IsUnlimited { get; set; }
}
