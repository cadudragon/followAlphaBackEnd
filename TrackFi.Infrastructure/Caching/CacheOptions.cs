namespace TrackFi.Infrastructure.Caching;

/// <summary>
/// Configuration options for caching behavior with specific TTLs for different cache types.
/// All durations are configurable via appsettings.json.
/// </summary>
public class CacheOptions
{
    /// <summary>
    /// Gets or sets whether caching is enabled.
    /// When false, all cache operations will bypass the cache and return null/do nothing.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the default TTL (Time To Live) in minutes for cached items.
    /// </summary>
    public int DefaultTtlMinutes { get; set; } = 15;

    // Specific cache durations (in minutes) for different data types
    // These can be tuned per environment without code changes

    /// <summary>
    /// TTL for individual token prices from Alchemy API (default: 1 minute).
    /// Short duration ensures prices stay relatively fresh.
    /// </summary>
    public int TokenPriceTtlMinutes { get; set; } = 1;

    /// <summary>
    /// TTL for token balances (without prices) (default: 3 minutes).
    /// Balance data changes less frequently than prices.
    /// </summary>
    public int TokenBalanceTtlMinutes { get; set; } = 3;

    /// <summary>
    /// TTL for NFT data (default: 30 minutes).
    /// NFTs change very infrequently.
    /// </summary>
    public int NftTtlMinutes { get; set; } = 30;

    /// <summary>
    /// TTL for verified token whitelist cache (default: 15 minutes).
    /// </summary>
    public int VerifiedTokensTtlMinutes { get; set; } = 15;

    /// <summary>
    /// TTL for unlisted token cache (default: 1440 minutes = 24 hours).
    /// Long duration to avoid repeated CoinMarketCap API calls for scam/unknown tokens.
    /// </summary>
    public int UnlistedTokensTtlMinutes { get; set; } = 1440;

    /// <summary>
    /// TTL for token metadata cache (default: 43200 minutes = 30 days).
    /// Very long duration since token metadata (symbol, name, decimals, logo) rarely changes.
    /// </summary>
    public int TokenMetadataTtlMinutes { get; set; } = 43200;

    // Helper properties for easy access as TimeSpan objects

    /// <summary>
    /// Token price cache duration as TimeSpan.
    /// </summary>
    public TimeSpan TokenPriceTtl => TimeSpan.FromMinutes(TokenPriceTtlMinutes);

    /// <summary>
    /// Token balance cache duration as TimeSpan.
    /// </summary>
    public TimeSpan TokenBalanceTtl => TimeSpan.FromMinutes(TokenBalanceTtlMinutes);

    /// <summary>
    /// NFT cache duration as TimeSpan.
    /// </summary>
    public TimeSpan NftTtl => TimeSpan.FromMinutes(NftTtlMinutes);

    /// <summary>
    /// Verified tokens cache duration as TimeSpan.
    /// </summary>
    public TimeSpan VerifiedTokensTtl => TimeSpan.FromMinutes(VerifiedTokensTtlMinutes);

    /// <summary>
    /// Unlisted tokens cache duration as TimeSpan.
    /// </summary>
    public TimeSpan UnlistedTokensTtl => TimeSpan.FromMinutes(UnlistedTokensTtlMinutes);

    /// <summary>
    /// Token metadata cache duration as TimeSpan.
    /// </summary>
    public TimeSpan TokenMetadataTtl => TimeSpan.FromMinutes(TokenMetadataTtlMinutes);

    /// <summary>
    /// Default cache duration as TimeSpan.
    /// </summary>
    public TimeSpan DefaultTtl => TimeSpan.FromMinutes(DefaultTtlMinutes);
}
