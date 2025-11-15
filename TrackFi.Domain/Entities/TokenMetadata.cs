namespace TrackFi.Domain.Entities;

using TrackFi.Domain.Enums;

/// <summary>
/// Token metadata cache entity. Stores metadata for ALL tokens we've encountered (verified, unlisted, or unknown).
/// Serves as a persistent cache to avoid repeated Alchemy API calls.
/// This is separate from verification status - a token can have metadata without being verified.
/// </summary>
public class TokenMetadata
{
    /// <summary>
    /// Unique identifier.
    /// </summary>
    public Guid Id { get; private set; }

    /// <summary>
    /// Token contract address (lowercase).
    /// </summary>
    public string ContractAddress { get; private set; }

    /// <summary>
    /// Blockchain network where this token exists.
    /// </summary>
    public BlockchainNetwork Network { get; private set; }

    /// <summary>
    /// Token symbol (e.g., "USDC", "ETH").
    /// </summary>
    public string Symbol { get; private set; }

    /// <summary>
    /// Token name (e.g., "USD Coin").
    /// </summary>
    public string Name { get; private set; }

    /// <summary>
    /// Number of decimals (e.g., 18 for most ERC20 tokens).
    /// </summary>
    public int Decimals { get; private set; }

    /// <summary>
    /// Token logo URL.
    /// </summary>
    public string? LogoUrl { get; private set; }

    /// <summary>
    /// When we first fetched this token's metadata.
    /// </summary>
    public DateTime FirstSeenAt { get; private set; }

    /// <summary>
    /// When metadata was last updated from the source.
    /// </summary>
    public DateTime LastUpdatedAt { get; private set; }

    /// <summary>
    /// How many times this token has been encountered across all wallets.
    /// Useful for analytics and identifying popular tokens.
    /// </summary>
    public int EncounterCount { get; private set; }

    /// <summary>
    /// Source of metadata (e.g., "Alchemy", "Manual").
    /// </summary>
    public string Source { get; private set; }

    // Private constructor for EF Core
    private TokenMetadata()
    {
        ContractAddress = string.Empty;
        Symbol = string.Empty;
        Name = string.Empty;
        Source = string.Empty;
    }

    /// <summary>
    /// Creates a new TokenMetadata entry.
    /// </summary>
    public TokenMetadata(
        string contractAddress,
        BlockchainNetwork network,
        string symbol,
        string name,
        int decimals,
        string? logoUrl,
        string source = "Alchemy")
    {
        if (string.IsNullOrWhiteSpace(contractAddress))
            throw new ArgumentException("Contract address cannot be empty", nameof(contractAddress));

        if (string.IsNullOrWhiteSpace(symbol))
            throw new ArgumentException("Symbol cannot be empty", nameof(symbol));

        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name cannot be empty", nameof(name));

        if (decimals < 0 || decimals > 255)
            throw new ArgumentException("Decimals must be between 0 and 255", nameof(decimals));

        Id = Guid.NewGuid();
        ContractAddress = contractAddress.ToLowerInvariant();
        Network = network;
        Symbol = symbol;
        Name = name;
        Decimals = decimals;
        LogoUrl = logoUrl;
        Source = source;
        FirstSeenAt = DateTime.UtcNow;
        LastUpdatedAt = DateTime.UtcNow;
        EncounterCount = 1;
    }

    /// <summary>
    /// Updates metadata from a fresh fetch.
    /// Call this when re-fetching from Alchemy to update stale data.
    /// </summary>
    public void UpdateMetadata(string symbol, string name, int decimals, string? logoUrl)
    {
        Symbol = symbol;
        Name = name;
        Decimals = decimals;
        LogoUrl = logoUrl;
        LastUpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Increments encounter count when this token is seen in another wallet.
    /// </summary>
    public void IncrementEncounterCount()
    {
        EncounterCount++;
    }
}
