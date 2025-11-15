using TrackFi.Domain.Enums;

namespace TrackFi.Domain.Entities;

/// <summary>
/// Lightweight read model for verified token data used by high-frequency portfolio reads.
/// </summary>
public record VerifiedTokenCacheEntry(
    string ContractAddress,
    BlockchainNetwork Network,
    string Symbol,
    string Name,
    int Decimals,
    string? LogoUrl,
    string? CoinGeckoId,
    TokenStandard? Standard,
    string? WebsiteUrl,
    string? Description,
    bool IsNative);
