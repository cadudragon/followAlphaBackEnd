namespace NetZerion.Models.Enums;

/// <summary>
/// Types of blockchain transactions that can be decoded by Zerion.
/// </summary>
public enum TransactionType
{
    /// <summary>
    /// Token or native currency sent from the wallet
    /// </summary>
    Send,

    /// <summary>
    /// Token or native currency received by the wallet
    /// </summary>
    Receive,

    /// <summary>
    /// Token swap on a DEX (e.g., Uniswap, Curve)
    /// </summary>
    Swap,

    /// <summary>
    /// Token approval for a contract
    /// </summary>
    Approve,

    /// <summary>
    /// Deposit tokens into a DeFi protocol
    /// </summary>
    Deposit,

    /// <summary>
    /// Withdraw tokens from a DeFi protocol
    /// </summary>
    Withdraw,

    /// <summary>
    /// Stake tokens in a staking contract
    /// </summary>
    Stake,

    /// <summary>
    /// Unstake tokens from a staking contract
    /// </summary>
    Unstake,

    /// <summary>
    /// Claim rewards from a protocol
    /// </summary>
    ClaimRewards,

    /// <summary>
    /// Mint new tokens or NFTs
    /// </summary>
    Mint,

    /// <summary>
    /// Burn tokens
    /// </summary>
    Burn,

    /// <summary>
    /// Cross-chain bridge transaction
    /// </summary>
    Bridge,

    /// <summary>
    /// Borrow assets from a lending protocol
    /// </summary>
    Borrow,

    /// <summary>
    /// Repay borrowed assets to a lending protocol
    /// </summary>
    Repay,

    /// <summary>
    /// Smart contract deployment
    /// </summary>
    Deployment,

    /// <summary>
    /// Generic contract execution
    /// </summary>
    Execution,

    /// <summary>
    /// Cancel a pending transaction
    /// </summary>
    Cancel,

    /// <summary>
    /// Trade on a DEX (generic)
    /// </summary>
    Trade,

    /// <summary>
    /// Unknown or uncategorized transaction type
    /// </summary>
    Unknown
}
