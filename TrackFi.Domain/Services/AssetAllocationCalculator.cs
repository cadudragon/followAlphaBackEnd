using TrackFi.Domain.Entities;
using TrackFi.Domain.Enums;
using TrackFi.Domain.ValueObjects;

namespace TrackFi.Domain.Services;

/// <summary>
/// Domain service for calculating asset allocation metrics.
/// </summary>
public class AssetAllocationCalculator
{
    public Dictionary<string, decimal> CalculatePercentageByType(AssetAllocation allocation)
    {
        if (allocation == null)
            throw new ArgumentNullException(nameof(allocation));

        var result = new Dictionary<string, decimal>();

        foreach (var type in Enum.GetValues<AssetType>())
        {
            var percentage = allocation.GetPercentageByType(type);
            if (percentage > 0)
            {
                result[type.ToString()] = percentage;
            }
        }

        return result;
    }

    public Dictionary<string, decimal> CalculatePercentageByNetwork(AssetAllocation allocation)
    {
        if (allocation == null)
            throw new ArgumentNullException(nameof(allocation));

        var result = new Dictionary<string, decimal>();

        foreach (var network in Enum.GetValues<BlockchainNetwork>())
        {
            var percentage = allocation.GetPercentageByNetwork(network);
            if (percentage > 0)
            {
                result[network.ToString()] = percentage;
            }
        }

        return result;
    }

    public bool IsDiversified(AssetAllocation allocation, decimal maxPercentageThreshold = 50m)
    {
        if (allocation == null)
            throw new ArgumentNullException(nameof(allocation));

        // Check if any single asset type exceeds the threshold
        foreach (var type in Enum.GetValues<AssetType>())
        {
            if (allocation.GetPercentageByType(type) > maxPercentageThreshold)
                return false;
        }

        return true;
    }

    public Money GetLargestHolding(Portfolio portfolio)
    {
        if (portfolio == null)
            throw new ArgumentNullException(nameof(portfolio));

        var assets = portfolio.GetAllAssets();
        if (assets.Count == 0)
            return Money.Zero(portfolio.BaseCurrency);

        return assets
            .Max(a => a.CalculateValue(portfolio.BaseCurrency).Amount)
            is decimal maxValue
            ? Money.Create(maxValue, portfolio.BaseCurrency)
            : Money.Zero(portfolio.BaseCurrency);
    }
}
