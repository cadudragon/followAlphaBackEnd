namespace TrackFi.Infrastructure.Caching;

/// <summary>
/// Generates standardized cache keys for different data types.
/// Ensures consistent key naming across the application.
/// </summary>
public static class CacheKeyGenerator
{
    private const string PortfolioPrefix = "portfolio";
    private const string PricePrefix = "price";
    private const string NftMetadataPrefix = "nft:metadata";
    private const string TokenMetadataPrefix = "token:metadata";
    private const string TransactionsPrefix = "transactions";

    /// <summary>
    /// Generates cache key for portfolio data.
    /// Format: portfolio:{walletAddress}:{hash}
    /// </summary>
    public static string ForPortfolio(string walletAddress, string? additionalKey = null)
    {
        return additionalKey != null
            ? $"{PortfolioPrefix}:{walletAddress}:{additionalKey}"
            : $"{PortfolioPrefix}:{walletAddress}";
    }

    /// <summary>
    /// Generates cache key for token price.
    /// Format: price:{tokenSymbol}
    /// </summary>
    public static string ForPrice(string tokenSymbol)
    {
        return $"{PricePrefix}:{tokenSymbol}";
    }

    /// <summary>
    /// Generates cache key for NFT metadata.
    /// Format: nft:metadata:{contractAddress}:{tokenId}
    /// </summary>
    public static string ForNftMetadata(string contractAddress, string tokenId)
    {
        return $"{NftMetadataPrefix}:{contractAddress}:{tokenId}";
    }

    /// <summary>
    /// Generates cache key for token metadata.
    /// Format: token:metadata:{contractAddress}
    /// </summary>
    public static string ForTokenMetadata(string contractAddress)
    {
        return $"{TokenMetadataPrefix}:{contractAddress}";
    }

    /// <summary>
    /// Generates cache key for recent transactions.
    /// Format: transactions:{walletAddress}:recent
    /// </summary>
    public static string ForRecentTransactions(string walletAddress)
    {
        return $"{TransactionsPrefix}:{walletAddress}:recent";
    }
}
