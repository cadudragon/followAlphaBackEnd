using TrackFi.Domain.Enums;
using TrackFi.Domain.ValueObjects;

namespace TrackFi.Domain.Entities;

/// <summary>
/// Represents a non-fungible token (NFT).
/// </summary>
public class Nft : Asset
{
    public NftIdentifier Identifier { get; private set; }
    public TokenStandard Standard { get; private set; }
    public string? ImageUrl { get; private set; }
    public string? ExternalUrl { get; private set; }
    public Dictionary<string, string> Attributes { get; private set; }

    private Nft(
        BlockchainNetwork network,
        AssetMetadata metadata,
        NftIdentifier identifier,
        TokenStandard standard,
        string? imageUrl = null,
        string? externalUrl = null)
        : base(AssetType.Nft, AssetCategory.Crypto, network, metadata)
    {
        Identifier = identifier ?? throw new ArgumentNullException(nameof(identifier));
        Standard = standard;
        ImageUrl = imageUrl;
        ExternalUrl = externalUrl;
        Attributes = new Dictionary<string, string>();
    }

    public static Nft CreateErc721(
        BlockchainNetwork network,
        AssetMetadata metadata,
        NftIdentifier identifier,
        string? imageUrl = null,
        string? externalUrl = null)
    {
        return new Nft(network, metadata, identifier, TokenStandard.ERC721, imageUrl, externalUrl);
    }

    public static Nft CreateErc1155(
        BlockchainNetwork network,
        AssetMetadata metadata,
        NftIdentifier identifier,
        string? imageUrl = null,
        string? externalUrl = null)
    {
        return new Nft(network, metadata, identifier, TokenStandard.ERC1155, imageUrl, externalUrl);
    }

    public void AddAttribute(string key, string value)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Attribute key cannot be empty", nameof(key));

        Attributes[key] = value;
    }

    public void UpdateMetadata(string? imageUrl, string? externalUrl)
    {
        ImageUrl = imageUrl;
        ExternalUrl = externalUrl;
    }

    public override Money CalculateValue(Currency currency)
    {
        // NFT valuation is complex - for V1, use floor price from CurrentPrice
        if (CurrentPrice == null)
            return Money.Zero(currency);

        if (CurrentPrice.Price.Currency != currency)
            throw new InvalidOperationException($"Price currency {CurrentPrice.Price.Currency} does not match requested currency {currency}");

        return CurrentPrice.Price;
    }

    public override string ToString() => $"NFT: {Metadata.Name} #{Identifier.TokenId}";
}
