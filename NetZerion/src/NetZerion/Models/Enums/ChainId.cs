using System.ComponentModel;

namespace NetZerion.Models.Enums;

/// <summary>
/// Supported blockchain networks in the Zerion API.
/// </summary>
public enum ChainId
{
    /// <summary>
    /// Ethereum mainnet
    /// </summary>
    [Description("ethereum")]
    Ethereum,

    /// <summary>
    /// Polygon (formerly Matic)
    /// </summary>
    [Description("polygon")]
    Polygon,

    /// <summary>
    /// Arbitrum One
    /// </summary>
    [Description("arbitrum")]
    Arbitrum,

    /// <summary>
    /// Optimism
    /// </summary>
    [Description("optimism")]
    Optimism,

    /// <summary>
    /// Base (Coinbase L2)
    /// </summary>
    [Description("base")]
    Base,

    /// <summary>
    /// Binance Smart Chain
    /// </summary>
    [Description("binance-smart-chain")]
    BinanceSmartChain,

    /// <summary>
    /// Avalanche C-Chain
    /// </summary>
    [Description("avalanche")]
    Avalanche,

    /// <summary>
    /// Fantom Opera
    /// </summary>
    [Description("fantom")]
    Fantom,

    /// <summary>
    /// zkSync Era
    /// </summary>
    [Description("zksync-era")]
    ZkSyncEra,

    /// <summary>
    /// Scroll
    /// </summary>
    [Description("scroll")]
    Scroll,

    /// <summary>
    /// Linea
    /// </summary>
    [Description("linea")]
    Linea,

    /// <summary>
    /// Blast
    /// </summary>
    [Description("blast")]
    Blast,

    /// <summary>
    /// Unichain
    /// </summary>
    [Description("unichain")]
    Unichain,

    /// <summary>
    /// Gnosis Chain (formerly xDai)
    /// </summary>
    [Description("xdai")]
    Gnosis,

    /// <summary>
    /// Celo
    /// </summary>
    [Description("celo")]
    Celo,

    /// <summary>
    /// Abstract
    /// </summary>
    [Description("abstract")]
    Abstract,

    /// <summary>
    /// Ape Chain
    /// </summary>
    [Description("ape")]
    ApeChain,

    /// <summary>
    /// Aurora
    /// </summary>
    [Description("aurora")]
    Aurora,

    /// <summary>
    /// Berachain
    /// </summary>
    [Description("berachain")]
    Berachain,

    /// <summary>
    /// Degen Chain
    /// </summary>
    [Description("degen")]
    DegenChain,

    /// <summary>
    /// Gravity Alpha
    /// </summary>
    [Description("gravity-alpha")]
    GravityAlpha,

    /// <summary>
    /// HyperEVM
    /// </summary>
    [Description("hyperevm")]
    HyperEVM,

    /// <summary>
    /// Ink
    /// </summary>
    [Description("ink")]
    Ink,

    /// <summary>
    /// Katana
    /// </summary>
    [Description("katana")]
    Katana,

    /// <summary>
    /// Lens Network
    /// </summary>
    [Description("lens")]
    Lens,

    /// <summary>
    /// Mantle
    /// </summary>
    [Description("mantle")]
    Mantle,

    /// <summary>
    /// Soneium
    /// </summary>
    [Description("soneium")]
    Soneium,

    /// <summary>
    /// Sonic
    /// </summary>
    [Description("sonic")]
    Sonic,

    /// <summary>
    /// Wonder
    /// </summary>
    [Description("wonder")]
    Wonder,

    /// <summary>
    /// XDC Network
    /// </summary>
    [Description("xinfin-xdc")]
    XDC,

    /// <summary>
    /// Zero Network
    /// </summary>
    [Description("zero")]
    Zero,

    /// <summary>
    /// ZKcandy
    /// </summary>
    [Description("zkcandy")]
    ZKcandy
}
