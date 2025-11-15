using TrackFi.Domain.Enums;

namespace TrackFi.Domain.Entities;

/// <summary>
/// Stores static metadata for blockchain networks including logos and display information.
/// </summary>
public class NetworkMetadata
{
    public int Id { get; set; }

    /// <summary>
    /// The blockchain network this metadata belongs to.
    /// </summary>
    public BlockchainNetwork Network { get; set; }

    /// <summary>
    /// Display name of the network.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// URL path to the network logo (e.g., "/images/networks/ethereum.svg").
    /// </summary>
    public string LogoUrl { get; set; } = string.Empty;

    /// <summary>
    /// Hex color code for the network (e.g., "#627EEA" for Ethereum).
    /// Optional, can be used for UI theming.
    /// </summary>
    public string? Color { get; set; }

    /// <summary>
    /// Official website URL for the network.
    /// </summary>
    public string? WebsiteUrl { get; set; }

    /// <summary>
    /// Block explorer URL (e.g., "https://etherscan.io").
    /// </summary>
    public string? ExplorerUrl { get; set; }
}
