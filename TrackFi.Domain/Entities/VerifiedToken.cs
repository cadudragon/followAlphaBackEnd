using TrackFi.Domain.Enums;

namespace TrackFi.Domain.Entities;

/// <summary>
/// Represents a verified/whitelisted token that is approved for display.
/// Only verified tokens will be shown to users (except for Premium users who can add custom tokens).
/// </summary>
public class VerifiedToken
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
    /// Token symbol (e.g., "ETH", "USDC", "WETH").
    /// </summary>
    public string Symbol { get; private set; }

    /// <summary>
    /// Token name (e.g., "Ethereum", "USD Coin").
    /// </summary>
    public string Name { get; private set; }

    /// <summary>
    /// Token decimals (usually 18 for ERC20, but can vary).
    /// </summary>
    public int Decimals { get; private set; }

    /// <summary>
    /// Logo URL for the token.
    /// </summary>
    public string? LogoUrl { get; private set; }

    /// <summary>
    /// CoinGecko coin ID for price fetching (e.g., "ethereum", "usd-coin").
    /// </summary>
    public string? CoinGeckoId { get; private set; }

    /// <summary>
    /// Token standard (ERC20, ERC721, ERC1155, etc.).
    /// </summary>
    public TokenStandard? Standard { get; private set; }

    /// <summary>
    /// Official website URL.
    /// </summary>
    public string? WebsiteUrl { get; private set; }

    /// <summary>
    /// Brief description of the token/project.
    /// </summary>
    public string? Description { get; private set; }

    /// <summary>
    /// Verification status.
    /// </summary>
    public VerificationStatus Status { get; private set; }

    /// <summary>
    /// Who verified this token (admin username, "system", etc.).
    /// </summary>
    public string? VerifiedBy { get; private set; }

    /// <summary>
    /// When this token was verified.
    /// </summary>
    public DateTime VerifiedAt { get; private set; }

    /// <summary>
    /// Is this a native token (ETH, MATIC, etc.).
    /// </summary>
    public bool IsNative { get; private set; }

    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    // EF Core constructor
    private VerifiedToken()
    {
        ContractAddress = string.Empty;
        Symbol = string.Empty;
        Name = string.Empty;
    }

    public VerifiedToken(
        string contractAddress,
        BlockchainNetwork network,
        string symbol,
        string name,
        int decimals = 18,
        string? logoUrl = null,
        string? coinGeckoId = null,
        TokenStandard? standard = null,
        string? websiteUrl = null,
        string? description = null,
        bool isNative = false,
        string? verifiedBy = "system")
    {
        Id = Guid.NewGuid();
        ContractAddress = contractAddress.ToLowerInvariant();
        Network = network;
        Symbol = symbol;
        Name = name;
        Decimals = decimals;
        LogoUrl = logoUrl;
        CoinGeckoId = coinGeckoId;
        Standard = standard;
        WebsiteUrl = websiteUrl;
        Description = description;
        IsNative = isNative;
        Status = VerificationStatus.Verified;
        VerifiedBy = verifiedBy;
        VerifiedAt = DateTime.UtcNow;
        CreatedAt = DateTime.UtcNow;
    }

    public void Update(
        string? symbol = null,
        string? name = null,
        string? logoUrl = null,
        string? coinGeckoId = null,
        string? websiteUrl = null,
        string? description = null)
    {
        if (symbol != null) Symbol = symbol;
        if (name != null) Name = name;
        if (logoUrl != null) LogoUrl = logoUrl;
        if (coinGeckoId != null) CoinGeckoId = coinGeckoId;
        if (websiteUrl != null) WebsiteUrl = websiteUrl;
        if (description != null) Description = description;
        UpdatedAt = DateTime.UtcNow;
    }

    public void ChangeStatus(VerificationStatus status, string? verifiedBy = null)
    {
        Status = status;
        if (verifiedBy != null) VerifiedBy = verifiedBy;
        UpdatedAt = DateTime.UtcNow;
    }
}
