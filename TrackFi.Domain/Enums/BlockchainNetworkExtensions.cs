namespace TrackFi.Domain.Enums;

/// <summary>
/// Extension methods for BlockchainNetwork enum.
/// Provides mapping to Alchemy network identifiers and other utilities.
/// </summary>
public static class BlockchainNetworkExtensions
{
    /// <summary>
    /// Maps BlockchainNetwork enum to Alchemy's network identifier string.
    /// Used for Alchemy API calls that require network parameter.
    /// </summary>
    /// <param name="network">The blockchain network enum value</param>
    /// <returns>Alchemy network identifier (e.g., "eth-mainnet", "polygon-mainnet")</returns>
    /// <exception cref="ArgumentException">Thrown when network is not supported by Alchemy</exception>
    public static string ToAlchemyNetwork(this BlockchainNetwork network)
    {
        return network switch
        {
            // Layer 1 chains
            BlockchainNetwork.Ethereum => "eth-mainnet",
            BlockchainNetwork.BNBChain => "bnb-mainnet",
            BlockchainNetwork.Avalanche => "avax-mainnet",
            BlockchainNetwork.Fantom => "ftm-mainnet",
            BlockchainNetwork.Polygon => "polygon-mainnet",
            BlockchainNetwork.Gnosis => "gnosis-mainnet",
            BlockchainNetwork.Celo => "celo-mainnet",
            BlockchainNetwork.Moonbeam => "moonbeam-mainnet",
            BlockchainNetwork.Moonriver => "moonriver-mainnet",
            BlockchainNetwork.Astar => "astar-mainnet",

            // Layer 2 chains (Ethereum rollups)
            BlockchainNetwork.Arbitrum => "arb-mainnet",
            BlockchainNetwork.ArbitrumNova => "arbnova-mainnet",
            BlockchainNetwork.Optimism => "opt-mainnet",
            BlockchainNetwork.Base => "base-mainnet",
            BlockchainNetwork.PolygonZkEVM => "polygonzkevm-mainnet",
            BlockchainNetwork.ZkSync => "zksync-mainnet",
            BlockchainNetwork.Linea => "linea-mainnet",
            BlockchainNetwork.Scroll => "scroll-mainnet",
            BlockchainNetwork.Mantle => "mantle-mainnet",
            BlockchainNetwork.Blast => "blast-mainnet",
            BlockchainNetwork.Metis => "metis-mainnet",
            BlockchainNetwork.Zora => "zora-mainnet",
            BlockchainNetwork.Mode => "mode-mainnet",
            BlockchainNetwork.Fraxtal => "fraxtal-mainnet",
            BlockchainNetwork.Unichain => "unichain-mainnet",

            // Additional EVM chains
            BlockchainNetwork.Harmony => "harmony-mainnet",
            BlockchainNetwork.Aurora => "aurora-mainnet",
            BlockchainNetwork.Cronos => "cronos-mainnet",
            BlockchainNetwork.Boba => "boba-mainnet",
            BlockchainNetwork.Evmos => "evmos-mainnet",
            BlockchainNetwork.Kava => "kava-mainnet",
            BlockchainNetwork.Fuse => "fuse-mainnet",
            BlockchainNetwork.Klaytn => "klaytn-mainnet",
            BlockchainNetwork.OKXChain => "okx-mainnet",
            BlockchainNetwork.Heco => "heco-mainnet",
            BlockchainNetwork.OptimismGoerli => "opt-goerli",
            BlockchainNetwork.Palm => "palm-mainnet",
            BlockchainNetwork.ShimmerEVM => "shimmer-mainnet",
            BlockchainNetwork.Rootstock => "rsk-mainnet",
            BlockchainNetwork.Velas => "velas-mainnet",
            BlockchainNetwork.IoTeX => "iotex-mainnet",
            BlockchainNetwork.Syscoin => "syscoin-mainnet",
            BlockchainNetwork.TelosEVM => "telos-mainnet",
            BlockchainNetwork.ThunderCore => "thundercore-mainnet",
            BlockchainNetwork.Wanchain => "wanchain-mainnet",
            BlockchainNetwork.Redstone => "redstone-mainnet",
            BlockchainNetwork.OasisSapphire => "oasis-sapphire-mainnet",
            BlockchainNetwork.OasisEmerald => "oasis-emerald-mainnet",
            BlockchainNetwork.Cyber => "cyber-mainnet",
            BlockchainNetwork.DegenChain => "degen-mainnet",

            _ => throw new ArgumentException($"Unsupported network for Alchemy: {network}", nameof(network))
        };
    }

    /// <summary>
    /// Maps Alchemy network identifier string back to BlockchainNetwork enum.
    /// Used for parsing Alchemy API responses.
    /// </summary>
    /// <param name="alchemyNetwork">Alchemy network identifier (e.g., "eth-mainnet")</param>
    /// <returns>BlockchainNetwork enum value</returns>
    /// <exception cref="ArgumentException">Thrown when network identifier is not recognized</exception>
    public static BlockchainNetwork FromAlchemyNetwork(string alchemyNetwork)
    {
        return alchemyNetwork.ToLowerInvariant() switch
        {
            // Layer 1 chains
            "eth-mainnet" => BlockchainNetwork.Ethereum,
            "bnb-mainnet" => BlockchainNetwork.BNBChain,
            "avax-mainnet" => BlockchainNetwork.Avalanche,
            "ftm-mainnet" => BlockchainNetwork.Fantom,
            "polygon-mainnet" => BlockchainNetwork.Polygon,
            "matic-mainnet" => BlockchainNetwork.Polygon,  // Alchemy returns "matic-mainnet" in responses (alias for polygon-mainnet)
            "gnosis-mainnet" => BlockchainNetwork.Gnosis,
            "celo-mainnet" => BlockchainNetwork.Celo,
            "moonbeam-mainnet" => BlockchainNetwork.Moonbeam,
            "moonriver-mainnet" => BlockchainNetwork.Moonriver,
            "astar-mainnet" => BlockchainNetwork.Astar,

            // Layer 2 chains
            "arb-mainnet" => BlockchainNetwork.Arbitrum,
            "arbnova-mainnet" => BlockchainNetwork.ArbitrumNova,
            "opt-mainnet" => BlockchainNetwork.Optimism,
            "base-mainnet" => BlockchainNetwork.Base,
            "polygonzkevm-mainnet" => BlockchainNetwork.PolygonZkEVM,
            "zksync-mainnet" => BlockchainNetwork.ZkSync,
            "linea-mainnet" => BlockchainNetwork.Linea,
            "scroll-mainnet" => BlockchainNetwork.Scroll,
            "mantle-mainnet" => BlockchainNetwork.Mantle,
            "blast-mainnet" => BlockchainNetwork.Blast,
            "metis-mainnet" => BlockchainNetwork.Metis,
            "zora-mainnet" => BlockchainNetwork.Zora,
            "mode-mainnet" => BlockchainNetwork.Mode,
            "fraxtal-mainnet" => BlockchainNetwork.Fraxtal,
            "unichain-mainnet" => BlockchainNetwork.Unichain,

            // Additional EVM chains
            "harmony-mainnet" => BlockchainNetwork.Harmony,
            "aurora-mainnet" => BlockchainNetwork.Aurora,
            "cronos-mainnet" => BlockchainNetwork.Cronos,
            "boba-mainnet" => BlockchainNetwork.Boba,
            "evmos-mainnet" => BlockchainNetwork.Evmos,
            "kava-mainnet" => BlockchainNetwork.Kava,
            "fuse-mainnet" => BlockchainNetwork.Fuse,
            "klaytn-mainnet" => BlockchainNetwork.Klaytn,
            "okx-mainnet" => BlockchainNetwork.OKXChain,
            "heco-mainnet" => BlockchainNetwork.Heco,
            "opt-goerli" => BlockchainNetwork.OptimismGoerli,
            "palm-mainnet" => BlockchainNetwork.Palm,
            "shimmer-mainnet" => BlockchainNetwork.ShimmerEVM,
            "rsk-mainnet" => BlockchainNetwork.Rootstock,
            "velas-mainnet" => BlockchainNetwork.Velas,
            "iotex-mainnet" => BlockchainNetwork.IoTeX,
            "syscoin-mainnet" => BlockchainNetwork.Syscoin,
            "telos-mainnet" => BlockchainNetwork.TelosEVM,
            "thundercore-mainnet" => BlockchainNetwork.ThunderCore,
            "wanchain-mainnet" => BlockchainNetwork.Wanchain,
            "redstone-mainnet" => BlockchainNetwork.Redstone,
            "oasis-sapphire-mainnet" => BlockchainNetwork.OasisSapphire,
            "oasis-emerald-mainnet" => BlockchainNetwork.OasisEmerald,
            "cyber-mainnet" => BlockchainNetwork.Cyber,
            "degen-mainnet" => BlockchainNetwork.DegenChain,

            _ => throw new ArgumentException($"Unsupported Alchemy network identifier: {alchemyNetwork}", nameof(alchemyNetwork))
        };
    }

    /// <summary>
    /// Gets all blockchain networks that are known to support DeFi protocols.
    /// Used to filter chains for DeFi-specific endpoints (Zerion/Moralis support).
    /// Returns all 18 networks supported by Zerion DeFi that exist in BlockchainNetwork enum.
    /// </summary>
    /// <returns>Array of networks with known DeFi protocol support</returns>
    public static BlockchainNetwork[] GetDeFiSupportedNetworks()
    {
        return
        [
            // Major DeFi chains with extensive protocol support
            BlockchainNetwork.Ethereum,
            BlockchainNetwork.Polygon,
            BlockchainNetwork.Arbitrum,
            BlockchainNetwork.Optimism,
            BlockchainNetwork.Base,
            BlockchainNetwork.Avalanche,
            BlockchainNetwork.BNBChain,
            BlockchainNetwork.Fantom,
            BlockchainNetwork.Gnosis,
            BlockchainNetwork.Celo,

            // L2s with growing DeFi ecosystems
            BlockchainNetwork.Blast,
            BlockchainNetwork.Linea,
            BlockchainNetwork.ZkSync,
            BlockchainNetwork.Scroll,
            BlockchainNetwork.Mantle,
            BlockchainNetwork.Unichain,

            // Additional networks with DeFi support
            BlockchainNetwork.Aurora,
            BlockchainNetwork.DegenChain
        ];
    }

    /// <summary>
    /// Gets all available EVM blockchain networks.
    /// Used for multi-network wallet balance queries.
    /// </summary>
    /// <returns>Array of all BlockchainNetwork enum values</returns>
    public static BlockchainNetwork[] GetAllNetworks()
    {
        return Enum.GetValues<BlockchainNetwork>();
    }

    /// <summary>
    /// Checks if a network is a Layer 2 rollup.
    /// </summary>
    public static bool IsLayer2(this BlockchainNetwork network)
    {
        return network switch
        {
            BlockchainNetwork.Arbitrum or
            BlockchainNetwork.ArbitrumNova or
            BlockchainNetwork.Optimism or
            BlockchainNetwork.Base or
            BlockchainNetwork.PolygonZkEVM or
            BlockchainNetwork.ZkSync or
            BlockchainNetwork.Linea or
            BlockchainNetwork.Scroll or
            BlockchainNetwork.Mantle or
            BlockchainNetwork.Blast or
            BlockchainNetwork.Metis or
            BlockchainNetwork.Zora or
            BlockchainNetwork.Mode or
            BlockchainNetwork.Fraxtal or
            BlockchainNetwork.Unichain or
            BlockchainNetwork.Boba or
            BlockchainNetwork.Redstone or
            BlockchainNetwork.Cyber or
            BlockchainNetwork.DegenChain => true,
            _ => false
        };
    }
}
