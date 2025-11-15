using TrackFi.Domain.Enums;
using TrackFi.Domain.ValueObjects;

namespace TrackFi.Domain.Entities;

/// <summary>
/// Abstract base class for all asset types.
/// Represents any holding with monetary value.
/// </summary>
public abstract class Asset
{
    public Guid Id { get; protected set; }
    public AssetType Type { get; protected set; }
    public AssetCategory Category { get; protected set; }
    public BlockchainNetwork Network { get; protected set; }
    public AssetMetadata Metadata { get; protected set; }
    public PriceInfo? CurrentPrice { get; protected set; }

    protected Asset(
        AssetType type,
        AssetCategory category,
        BlockchainNetwork network,
        AssetMetadata metadata)
    {
        Id = Guid.NewGuid();
        Type = type;
        Category = category;
        Network = network;
        Metadata = metadata ?? throw new ArgumentNullException(nameof(metadata));
    }

    public void UpdatePrice(PriceInfo priceInfo)
    {
        CurrentPrice = priceInfo ?? throw new ArgumentNullException(nameof(priceInfo));
    }

    /// <summary>
    /// Calculates the total value of this asset in the specified currency.
    /// </summary>
    public abstract Money CalculateValue(Currency currency);
}
