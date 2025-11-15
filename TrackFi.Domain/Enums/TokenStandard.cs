namespace TrackFi.Domain.Enums;

/// <summary>
/// Token standards for different blockchain networks.
/// </summary>
public enum TokenStandard
{
    // Ethereum standards
    ERC20 = 1,      // Fungible tokens
    ERC721 = 2,     // NFTs
    ERC1155 = 3,    // Multi-token standard

    // Solana standards
    SPL = 10,       // Solana Program Library token

    // Native
    Native = 99     // ETH, SOL, MATIC, etc.
}
