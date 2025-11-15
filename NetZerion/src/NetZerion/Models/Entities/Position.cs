using NetZerion.Models.Enums;

namespace NetZerion.Models.Entities;

/// <summary>
/// Represents a DeFi position (LP, Staking, Lending, Borrowing, etc.)
/// </summary>
public class Position
{
    /// <summary>
    /// Unique position identifier
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Protocol where this position is held
    /// </summary>
    public Protocol Protocol { get; set; } = new();

    /// <summary>
    /// Position display name (e.g., "ETH/USDC Pool", "AAVE Deposit")
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Type of position
    /// </summary>
    public PositionType Type { get; set; }

    /// <summary>
    /// Protocol module (e.g., "lending", "farming", "staking")
    /// </summary>
    public string? ProtocolModule { get; set; }

    /// <summary>
    /// Pool/contract address
    /// </summary>
    public string? PoolAddress { get; set; }

    /// <summary>
    /// Group ID to link related positions (e.g., staked + rewards in farming)
    /// </summary>
    public string? GroupId { get; set; }

    /// <summary>
    /// Assets included in this position
    /// </summary>
    public List<Fungible> Assets { get; set; } = new();

    /// <summary>
    /// Total value of the position in USD
    /// </summary>
    public decimal ValueUsd { get; set; }

    /// <summary>
    /// Quantity of the position token (for LP tokens, staked tokens, etc.)
    /// </summary>
    public decimal Quantity { get; set; }

    /// <summary>
    /// Annual Percentage Yield (if applicable)
    /// </summary>
    public decimal? Apy { get; set; }

    /// <summary>
    /// Annual Percentage Rate (if applicable, for borrowing)
    /// </summary>
    public decimal? Apr { get; set; }

    /// <summary>
    /// Chain where this position exists
    /// </summary>
    public Chain? Chain { get; set; }

    /// <summary>
    /// 24-hour value change in USD
    /// </summary>
    public decimal? ValueChange24h { get; set; }

    /// <summary>
    /// 24-hour value change percentage
    /// </summary>
    public decimal? PercentChange24h { get; set; }

    /// <summary>
    /// Claimable rewards (if any)
    /// </summary>
    public List<Fungible> ClaimableRewards { get; set; } = new();

    /// <summary>
    /// Health factor (for lending positions, e.g., Aave).
    /// Value greater than 1.0 is safe, less than 1.0 risks liquidation.
    /// </summary>
    public decimal? HealthFactor { get; set; }

    /// <summary>
    /// Indicates if this is a debt position (borrowing)
    /// </summary>
    public bool IsDebt { get; set; }

    /// <summary>
    /// Additional position metadata from Zerion
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();

    /// <summary>
    /// Raw response data from Zerion API (for debugging)
    /// </summary>
    public object? RawData { get; set; }
}
