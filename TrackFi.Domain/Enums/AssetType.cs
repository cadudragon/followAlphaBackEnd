namespace TrackFi.Domain.Enums;

/// <summary>
/// Specific types of crypto assets.
/// </summary>
public enum AssetType
{
    Token = 1,        // Fungible tokens (ERC-20, SPL)
    Nft = 2,          // Non-fungible tokens (ERC-721, ERC-1155)
    DeFiPosition = 3  // Staking, lending, liquidity positions
}
