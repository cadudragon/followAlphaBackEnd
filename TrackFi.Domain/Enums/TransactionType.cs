namespace TrackFi.Domain.Enums;

/// <summary>
/// Types of blockchain transactions.
/// </summary>
public enum TransactionType
{
    Send = 1,
    Receive = 2,
    Swap = 3,
    Stake = 4,
    Unstake = 5,
    Mint = 6,
    Burn = 7,
    Approve = 8,
    ContractInteraction = 9
}
