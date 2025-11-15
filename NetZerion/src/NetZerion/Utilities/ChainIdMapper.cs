using NetZerion.Models.Enums;
using System.ComponentModel;
using System.Reflection;

namespace NetZerion.Utilities;

/// <summary>
/// Utility class for mapping between ChainId enum and Zerion API chain ID strings.
/// </summary>
public static class ChainIdMapper
{
    /// <summary>
    /// Converts a ChainId enum to its corresponding Zerion API chain ID string.
    /// </summary>
    /// <param name="chainId">The ChainId enum value.</param>
    /// <returns>Zerion API chain ID string (e.g., "ethereum", "polygon").</returns>
    public static string ToApiString(this ChainId chainId)
    {
        var field = chainId.GetType().GetField(chainId.ToString());
        var attribute = field?.GetCustomAttribute<DescriptionAttribute>();
        return attribute?.Description ?? chainId.ToString().ToLowerInvariant();
    }

    /// <summary>
    /// Converts a Zerion API chain ID string to a ChainId enum.
    /// </summary>
    /// <param name="apiString">Zerion API chain ID string.</param>
    /// <returns>Corresponding ChainId enum value, or null if not found.</returns>
    public static ChainId? FromApiString(string apiString)
    {
        if (string.IsNullOrWhiteSpace(apiString))
            return null;

        var normalized = apiString.ToLowerInvariant();

        return normalized switch
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

    /// <summary>
    /// Gets the display name for a ChainId.
    /// </summary>
    /// <param name="chainId">The ChainId enum value.</param>
    /// <returns>Display name (e.g., "Ethereum", "Polygon").</returns>
    public static string ToDisplayName(this ChainId chainId)
    {
        return chainId switch
        {
            ChainId.BinanceSmartChain => "Binance Smart Chain",
            ChainId.ZkSyncEra => "zkSync Era",
            _ => chainId.ToString()
        };
    }
}
