namespace TrackFi.Domain.Enums;

/// <summary>
/// Supported EVM-compatible blockchain networks.
/// All chains use Ethereum JSON-RPC interface and 0x-prefixed addresses.
/// Organized by: Layer 1, Layer 2, Sidechains, and Emerging chains.
/// </summary>
public enum BlockchainNetwork
{
    // ===== LAYER 1 CHAINS =====

    /// <summary>Ethereum - Original smart contract platform</summary>
    Ethereum = 1,

    /// <summary>BNB Smart Chain (formerly Binance Smart Chain)</summary>
    BNBChain = 2,

    /// <summary>Avalanche C-Chain - High-throughput EVM chain</summary>
    Avalanche = 3,

    /// <summary>Fantom Opera - DAG-based EVM chain</summary>
    Fantom = 4,

    /// <summary>Polygon - Ethereum sidechain (formerly Matic)</summary>
    Polygon = 5,

    /// <summary>Gnosis Chain (formerly xDai Chain)</summary>
    Gnosis = 6,

    /// <summary>Celo - Mobile-first blockchain</summary>
    Celo = 7,

    /// <summary>Moonbeam - Polkadot parachain with EVM compatibility</summary>
    Moonbeam = 8,

    /// <summary>Moonriver - Kusama parachain with EVM compatibility</summary>
    Moonriver = 9,

    /// <summary>Astar - Polkadot parachain supporting WASM and EVM</summary>
    Astar = 10,

    // ===== LAYER 2 CHAINS (ETHEREUM ROLLUPS) =====

    /// <summary>Arbitrum One - Optimistic rollup</summary>
    Arbitrum = 11,

    /// <summary>Arbitrum Nova - Optimistic rollup optimized for gaming/social</summary>
    ArbitrumNova = 12,

    /// <summary>Optimism - Optimistic rollup</summary>
    Optimism = 13,

    /// <summary>Base - Coinbase's L2 built on OP Stack</summary>
    Base = 14,

    /// <summary>Polygon zkEVM - Zero-knowledge rollup</summary>
    PolygonZkEVM = 15,

    /// <summary>zkSync Era - Zero-knowledge rollup</summary>
    ZkSync = 16,

    /// <summary>Linea - ConsenSys zero-knowledge rollup</summary>
    Linea = 17,

    /// <summary>Scroll - Zero-knowledge rollup</summary>
    Scroll = 18,

    /// <summary>Mantle - Modular L2 by BitDAO</summary>
    Mantle = 19,

    /// <summary>Blast - L2 with native yield</summary>
    Blast = 20,

    /// <summary>Metis Andromeda - Optimistic rollup with decentralized sequencer</summary>
    Metis = 21,

    /// <summary>Zora - L2 optimized for NFTs and creators</summary>
    Zora = 22,

    /// <summary>Mode - OP Stack L2 focused on DeFi</summary>
    Mode = 23,

    /// <summary>Fraxtal - Frax Finance L2 on OP Stack</summary>
    Fraxtal = 24,

    /// <summary>Unichain - Uniswap's L2 (emerging)</summary>
    Unichain = 25,

    // ===== ADDITIONAL EVM CHAINS =====

    /// <summary>Harmony - Sharded blockchain</summary>
    Harmony = 26,

    /// <summary>Aurora - NEAR Protocol's EVM layer</summary>
    Aurora = 27,

    /// <summary>Cronos - Crypto.com chain</summary>
    Cronos = 28,

    /// <summary>Boba Network - Optimistic rollup with Hybrid Compute</summary>
    Boba = 29,

    /// <summary>Evmos - Cosmos SDK chain with EVM compatibility</summary>
    Evmos = 30,

    /// <summary>Kava - Cosmos-Ethereum co-chain</summary>
    Kava = 31,

    /// <summary>Fuse - Fast, low-cost payments</summary>
    Fuse = 32,

    /// <summary>Klaytn - Kakao's blockchain</summary>
    Klaytn = 33,

    /// <summary>OKX Chain (formerly OKExChain)</summary>
    OKXChain = 34,

    /// <summary>Heco (Huobi Eco Chain)</summary>
    Heco = 35,

    /// <summary>Optimism Goerli - Optimism testnet (keeping for backwards compatibility)</summary>
    OptimismGoerli = 36,

    /// <summary>Palm - NFT-focused sidechain</summary>
    Palm = 37,

    /// <summary>Shimmer EVM - IOTA's EVM layer</summary>
    ShimmerEVM = 38,

    /// <summary>Rootstock (RSK) - Bitcoin sidechain</summary>
    Rootstock = 39,

    /// <summary>Velas - Solana-based EVM chain</summary>
    Velas = 40,

    /// <summary>IoTeX - IoT-focused blockchain</summary>
    IoTeX = 41,

    /// <summary>Syscoin NEVM - Bitcoin merge-mined EVM</summary>
    Syscoin = 42,

    /// <summary>Telos EVM - High-performance EVM</summary>
    TelosEVM = 43,

    /// <summary>ThunderCore - High-performance EVM</summary>
    ThunderCore = 44,

    /// <summary>Wanchain - Cross-chain platform</summary>
    Wanchain = 45,

    /// <summary>Redstone - EVM chain focused on data availability</summary>
    Redstone = 46,

    /// <summary>Oasis Sapphire - Privacy-focused EVM ParaTime</summary>
    OasisSapphire = 47,

    /// <summary>Oasis Emerald - EVM-compatible ParaTime</summary>
    OasisEmerald = 48,

    /// <summary>Cyber - SocialFi L2 on OP Stack</summary>
    Cyber = 49,

    /// <summary>Degen Chain - Farcaster ecosystem L3</summary>
    DegenChain = 50
}
