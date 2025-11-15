using TrackFi.Domain.Enums;
using TrackFi.Domain.ValueObjects;

namespace TrackFi.Domain.Entities;

/// <summary>
/// Represents the breakdown of assets by various dimensions.
/// </summary>
public class AssetAllocation
{
    public Dictionary<AssetType, Money> ByType { get; private set; }
    public Dictionary<AssetCategory, Money> ByCategory { get; private set; }
    public Dictionary<BlockchainNetwork, Money> ByNetwork { get; private set; }
    public Money TotalValue { get; private set; }
    public Currency Currency { get; private set; }

    private AssetAllocation(
        Dictionary<AssetType, Money> byType,
        Dictionary<AssetCategory, Money> byCategory,
        Dictionary<BlockchainNetwork, Money> byNetwork,
        Money totalValue,
        Currency currency)
    {
        ByType = byType;
        ByCategory = byCategory;
        ByNetwork = byNetwork;
        TotalValue = totalValue;
        Currency = currency;
    }

    public static AssetAllocation Create(Portfolio portfolio)
    {
        if (portfolio == null)
            throw new ArgumentNullException(nameof(portfolio));

        var currency = portfolio.BaseCurrency;
        var byType = new Dictionary<AssetType, Money>();
        var byCategory = new Dictionary<AssetCategory, Money>();
        var byNetwork = new Dictionary<BlockchainNetwork, Money>();

        foreach (var asset in portfolio.GetAllAssets())
        {
            var value = asset.CalculateValue(currency);

            // By Type
            if (!byType.ContainsKey(asset.Type))
                byType[asset.Type] = Money.Zero(currency);
            byType[asset.Type] = byType[asset.Type].Add(value);

            // By Category
            if (!byCategory.ContainsKey(asset.Category))
                byCategory[asset.Category] = Money.Zero(currency);
            byCategory[asset.Category] = byCategory[asset.Category].Add(value);

            // By Network
            if (!byNetwork.ContainsKey(asset.Network))
                byNetwork[asset.Network] = Money.Zero(currency);
            byNetwork[asset.Network] = byNetwork[asset.Network].Add(value);
        }

        var totalValue = portfolio.CalculateTotalNetWorth();

        return new AssetAllocation(byType, byCategory, byNetwork, totalValue, currency);
    }

    public decimal GetPercentageByType(AssetType type)
    {
        if (TotalValue.Amount == 0)
            return 0;

        if (!ByType.TryGetValue(type, out var value))
            return 0;

        return (value.Amount / TotalValue.Amount) * 100;
    }

    public decimal GetPercentageByCategory(AssetCategory category)
    {
        if (TotalValue.Amount == 0)
            return 0;

        if (!ByCategory.TryGetValue(category, out var value))
            return 0;

        return (value.Amount / TotalValue.Amount) * 100;
    }

    public decimal GetPercentageByNetwork(BlockchainNetwork network)
    {
        if (TotalValue.Amount == 0)
            return 0;

        if (!ByNetwork.TryGetValue(network, out var value))
            return 0;

        return (value.Amount / TotalValue.Amount) * 100;
    }
}
