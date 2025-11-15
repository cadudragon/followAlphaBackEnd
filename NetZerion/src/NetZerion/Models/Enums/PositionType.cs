namespace NetZerion.Models.Enums;

/// <summary>
/// Types of DeFi positions that can be held in protocols.
/// </summary>
public enum PositionType
{
    /// <summary>
    /// Liquidity pool position (e.g., Uniswap LP, Curve LP)
    /// </summary>
    LiquidityPool,

    /// <summary>
    /// Staked tokens earning rewards (e.g., stETH, staking in validators)
    /// </summary>
    Staking,

    /// <summary>
    /// Staked position (same as Staking, used by Zerion API for farming pools)
    /// </summary>
    Staked,

    /// <summary>
    /// Reward position (unclaimed rewards from farming, staking, etc.)
    /// </summary>
    Reward,

    /// <summary>
    /// Lending position - assets supplied to lending protocol (e.g., Aave deposits)
    /// </summary>
    Lending,

    /// <summary>
    /// Borrowing position - debt in lending protocol (e.g., Aave borrows)
    /// </summary>
    Borrowing,

    /// <summary>
    /// Locked tokens (e.g., vested tokens, time-locked positions)
    /// </summary>
    Locked,

    /// <summary>
    /// Vesting schedule position
    /// </summary>
    Vesting,

    /// <summary>
    /// Claimable rewards or tokens
    /// </summary>
    Claimable,

    /// <summary>
    /// Farming position (yield farming)
    /// </summary>
    Farming,

    /// <summary>
    /// Insurance or protection position
    /// </summary>
    Insurance,

    /// <summary>
    /// Derivative position (options, futures, etc.)
    /// </summary>
    Derivative,

    /// <summary>
    /// Generic deposit position
    /// </summary>
    Deposit,

    /// <summary>
    /// Other position type not categorized above
    /// </summary>
    Other
}
