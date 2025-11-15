namespace TrackFi.Application.Portfolio.DTOs;

/// <summary>
/// DTO representing wallet balances (tokens only) across multiple networks.
/// </summary>
public class MultiNetworkWalletDto
{
    public string WalletAddress { get; set; } = string.Empty;
    public bool IsAnonymous { get; set; } = true;
    public WalletSummaryDto Summary { get; set; } = new();
    public List<NetworkWalletDto> Networks { get; set; } = new();
    public DateTime? CacheExpiresAt { get; set; }
}

/// <summary>
/// DTO representing NFTs across multiple networks.
/// </summary>
public class MultiNetworkNftDto
{
    public string WalletAddress { get; set; } = string.Empty;
    public bool IsAnonymous { get; set; } = true;
    public NftSummaryDto Summary { get; set; } = new();
    public List<NetworkNftDto> Networks { get; set; } = new();
    public DateTime? CacheExpiresAt { get; set; }
}

/// <summary>
/// Wallet data (tokens only) for a specific network.
/// </summary>
public class NetworkWalletDto
{
    public string Network { get; set; } = string.Empty;
    public string? NetworkLogoUrl { get; set; }
    public decimal TotalValueUsd { get; set; }
    public List<TokenBalanceDto> Tokens { get; set; } = new();
    public int TokenCount { get; set; }
}

/// <summary>
/// NFT data for a specific network.
/// </summary>
public class NetworkNftDto
{
    public string Network { get; set; } = string.Empty;
    public string? NetworkLogoUrl { get; set; }
    public List<NftDto> Nfts { get; set; } = new();
    public int NftCount { get; set; }
}

/// <summary>
/// Summary for wallet balances (tokens only).
/// </summary>
public class WalletSummaryDto
{
    public decimal TotalValueUsd { get; set; }
    public int TotalTokens { get; set; }
    public DateTime LastUpdated { get; set; }
}

/// <summary>
/// Summary for NFTs.
/// </summary>
public class NftSummaryDto
{
    public int TotalNfts { get; set; }
    public int TotalCollections { get; set; }
    public DateTime LastUpdated { get; set; }
}
