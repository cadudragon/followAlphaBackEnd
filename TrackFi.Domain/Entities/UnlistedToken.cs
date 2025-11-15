using TrackFi.Domain.Enums;

namespace TrackFi.Domain.Entities;

/// <summary>
/// Represents a token that was checked but not found in CoinMarketCap.
/// These tokens are cached to avoid repeated API calls for unknown/scam tokens.
/// </summary>
public class UnlistedToken
{
    public Guid Id { get; private set; }

    /// <summary>
    /// The token contract address (lowercase).
    /// </summary>
    public string ContractAddress { get; private set; }

    /// <summary>
    /// The blockchain network where this token exists.
    /// </summary>
    public BlockchainNetwork Network { get; private set; }

    /// <summary>
    /// Token symbol if available from on-chain data.
    /// </summary>
    public string? Symbol { get; private set; }

    /// <summary>
    /// Token name if available from on-chain data.
    /// </summary>
    public string? Name { get; private set; }

    /// <summary>
    /// When this token was first checked and not found.
    /// </summary>
    public DateTime FirstCheckedAt { get; private set; }

    /// <summary>
    /// Last time we attempted to verify this token.
    /// </summary>
    public DateTime LastCheckedAt { get; private set; }

    /// <summary>
    /// Number of times this token has been checked.
    /// </summary>
    public int CheckCount { get; private set; }

    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    // EF Core constructor
    private UnlistedToken()
    {
        ContractAddress = string.Empty;
    }

    public UnlistedToken(
        string contractAddress,
        BlockchainNetwork network,
        string? symbol = null,
        string? name = null)
    {
        Id = Guid.NewGuid();
        ContractAddress = contractAddress.ToLowerInvariant();
        Network = network;
        Symbol = symbol;
        Name = name;
        FirstCheckedAt = DateTime.UtcNow;
        LastCheckedAt = DateTime.UtcNow;
        CheckCount = 1;
        CreatedAt = DateTime.UtcNow;
    }

    public void RecordCheck()
    {
        LastCheckedAt = DateTime.UtcNow;
        CheckCount++;
        UpdatedAt = DateTime.UtcNow;
    }
}
