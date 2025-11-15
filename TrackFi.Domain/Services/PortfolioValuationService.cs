using TrackFi.Domain.Entities;
using TrackFi.Domain.Enums;
using TrackFi.Domain.Interfaces;
using TrackFi.Domain.ValueObjects;

namespace TrackFi.Domain.Services;

/// <summary>
/// Domain service for portfolio valuation and aggregation.
/// Pure business logic with no infrastructure dependencies.
/// </summary>
public class PortfolioValuationService
{
    public Portfolio CreatePortfolio(List<Wallet> wallets, Currency baseCurrency = Currency.USD)
    {
        if (wallets == null)
            throw new ArgumentNullException(nameof(wallets));

        var portfolio = Portfolio.Create(baseCurrency);

        foreach (var wallet in wallets)
        {
            portfolio.AddAccount(wallet);
        }

        return portfolio;
    }

    public async Task<Portfolio> CreatePortfolioWithPricesAsync(
        List<Wallet> wallets,
        IPriceProvider priceProvider,
        Currency baseCurrency = Currency.USD,
        CancellationToken cancellationToken = default)
    {
        if (wallets == null)
            throw new ArgumentNullException(nameof(wallets));

        if (priceProvider == null)
            throw new ArgumentNullException(nameof(priceProvider));

        var portfolio = CreatePortfolio(wallets, baseCurrency);

        // Get all assets from all wallets
        var allAssets = portfolio.GetAllAssets();

        if (allAssets.Count == 0)
            return portfolio;

        // Fetch prices in batch
        var prices = await priceProvider.GetPricesAsync(allAssets, baseCurrency, cancellationToken);

        // Update each asset with its price
        foreach (var asset in allAssets)
        {
            if (prices.TryGetValue(asset.Id, out var priceInfo))
            {
                asset.UpdatePrice(priceInfo);
            }
        }

        return portfolio;
    }

    public Money CalculateNetWorth(Portfolio portfolio)
    {
        if (portfolio == null)
            throw new ArgumentNullException(nameof(portfolio));

        return portfolio.CalculateTotalNetWorth();
    }

    public AssetAllocation CalculateAllocation(Portfolio portfolio)
    {
        if (portfolio == null)
            throw new ArgumentNullException(nameof(portfolio));

        return portfolio.CalculateAllocation();
    }

    public Dictionary<BlockchainNetwork, Money> GetNetWorthByNetwork(Portfolio portfolio)
    {
        if (portfolio == null)
            throw new ArgumentNullException(nameof(portfolio));

        var allocation = portfolio.CalculateAllocation();
        return allocation.ByNetwork;
    }

    public List<Asset> GetTopAssets(Portfolio portfolio, int count = 10)
    {
        if (portfolio == null)
            throw new ArgumentNullException(nameof(portfolio));

        return portfolio.GetAllAssets()
            .OrderByDescending(a => a.CalculateValue(portfolio.BaseCurrency).Amount)
            .Take(count)
            .ToList();
    }
}
