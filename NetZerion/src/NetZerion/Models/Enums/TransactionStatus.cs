namespace NetZerion.Models.Enums;

/// <summary>
/// Status of a blockchain transaction.
/// </summary>
public enum TransactionStatus
{
    /// <summary>
    /// Transaction is pending and not yet confirmed on-chain
    /// </summary>
    Pending,

    /// <summary>
    /// Transaction has been confirmed and included in a block
    /// </summary>
    Confirmed,

    /// <summary>
    /// Transaction failed during execution
    /// </summary>
    Failed,

    /// <summary>
    /// Transaction was dropped from the mempool
    /// </summary>
    Dropped,

    /// <summary>
    /// Transaction was replaced by another transaction (same nonce)
    /// </summary>
    Replaced
}
