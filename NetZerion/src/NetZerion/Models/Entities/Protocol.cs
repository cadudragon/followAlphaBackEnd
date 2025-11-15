namespace NetZerion.Models.Entities;

/// <summary>
/// Represents a DeFi protocol (e.g., Uniswap, Aave, Curve).
/// </summary>
public class Protocol
{
    /// <summary>
    /// Protocol identifier
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Protocol display name (e.g., "Uniswap V3", "Aave V2")
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Protocol icon/logo URL
    /// </summary>
    public string? IconUrl { get; set; }

    /// <summary>
    /// Protocol website URL
    /// </summary>
    public string? WebsiteUrl { get; set; }

    /// <summary>
    /// Additional protocol metadata
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();
}
