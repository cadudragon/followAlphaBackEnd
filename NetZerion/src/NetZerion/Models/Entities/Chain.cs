using NetZerion.Models.Enums;

namespace NetZerion.Models.Entities;

/// <summary>
/// Represents a blockchain network.
/// </summary>
public class Chain
{
    /// <summary>
    /// Chain identifier (matches ChainId enum)
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Chain display name (e.g., "Ethereum", "Polygon")
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Chain icon/logo URL
    /// </summary>
    public string? IconUrl { get; set; }

    /// <summary>
    /// Native token symbol (e.g., "ETH", "MATIC")
    /// </summary>
    public string? NativeTokenSymbol { get; set; }

    /// <summary>
    /// Converts string chain ID to ChainId enum
    /// </summary>
    public ChainId? ToChainId()
    {
        return Id.ToLowerInvariant() switch
        {
            "ethereum" => ChainId.Ethereum,
            "polygon" => ChainId.Polygon,
            "arbitrum" => ChainId.Arbitrum,
            "optimism" => ChainId.Optimism,
            "base" => ChainId.Base,
            "binance-smart-chain" => ChainId.BinanceSmartChain,
            "avalanche" => ChainId.Avalanche,
            "fantom" => ChainId.Fantom,
            "zksync-era" => ChainId.ZkSyncEra,
            "scroll" => ChainId.Scroll,
            "linea" => ChainId.Linea,
            "blast" => ChainId.Blast,
            "unichain" => ChainId.Unichain,
            "gnosis" => ChainId.Gnosis,
            "celo" => ChainId.Celo,
            _ => null
        };
    }
}
