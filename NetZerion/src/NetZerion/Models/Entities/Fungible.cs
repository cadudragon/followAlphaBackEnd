namespace NetZerion.Models.Entities;

/// <summary>
/// Represents a fungible token (ERC-20 or native coin).
/// </summary>
public class Fungible
{
    /// <summary>
    /// Token contract address (or "native" for ETH, MATIC, etc.)
    /// </summary>
    public string Address { get; set; } = string.Empty;

    /// <summary>
    /// Token symbol (e.g., "USDC", "ETH", "WETH")
    /// </summary>
    public string Symbol { get; set; } = string.Empty;

    /// <summary>
    /// Token full name (e.g., "USD Coin", "Wrapped Ether")
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Number of decimal places for the token
    /// </summary>
    public int Decimals { get; set; }

    /// <summary>
    /// Token balance as raw integer string (wei for ETH, smallest unit for ERC-20)
    /// </summary>
    public string BalanceRaw { get; set; } = "0";

    /// <summary>
    /// Token balance as human-readable decimal (adjusted for decimals)
    /// </summary>
    public decimal Balance { get; set; }

    /// <summary>
    /// Price per token in USD
    /// </summary>
    public decimal? PriceUsd { get; set; }

    /// <summary>
    /// Total value in USD (Balance * PriceUsd)
    /// </summary>
    public decimal? ValueUsd { get; set; }

    /// <summary>
    /// Token logo/icon URL
    /// </summary>
    public string? IconUrl { get; set; }

    /// <summary>
    /// Indicates if the token is verified by Zerion
    /// </summary>
    public bool IsVerified { get; set; }

    /// <summary>
    /// Indicates if the token should be displayed (not dust/spam)
    /// </summary>
    public bool IsDisplayable { get; set; } = true;

    /// <summary>
    /// Chain where this token exists
    /// </summary>
    public Chain? Chain { get; set; }

    /// <summary>
    /// 24-hour price change percentage
    /// </summary>
    public decimal? PriceChange24h { get; set; }

    /// <summary>
    /// Additional token metadata
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();
}
