namespace TrackFi.Domain.Enums;

/// <summary>
/// Supported non-EVM blockchain networks.
/// These chains use different RPC interfaces, address formats, and transaction models.
/// </summary>
public enum NonEvmBlockchainNetwork
{
    /// <summary>
    /// Solana - High-performance blockchain using Proof of History.
    /// Address format: Base58 encoded (32-44 characters, no prefix)
    /// </summary>
    Solana = 1,

    /// <summary>
    /// Bitcoin - Original cryptocurrency using UTXO model.
    /// Address format: Base58Check or Bech32 (1..., 3..., bc1...)
    /// </summary>
    Bitcoin = 2,

    /// <summary>
    /// Tron - EVM-compatible but uses TRC-20/TRC-721 token standards.
    /// Address format: Base58 starting with 'T' (34 characters)
    /// </summary>
    Tron = 3
}
