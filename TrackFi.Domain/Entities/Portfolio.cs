using TrackFi.Domain.Enums;
using TrackFi.Domain.ValueObjects;

namespace TrackFi.Domain.Entities;

/// <summary>
/// Represents an aggregated view of all assets across multiple accounts.
/// </summary>
public class Portfolio
{
    public Guid Id { get; private set; }
    public List<Account> Accounts { get; private set; }
    public Currency BaseCurrency { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime LastUpdated { get; private set; }

    private Portfolio(Currency baseCurrency)
    {
        Id = Guid.NewGuid();
        Accounts = new List<Account>();
        BaseCurrency = baseCurrency;
        CreatedAt = DateTime.UtcNow;
        LastUpdated = DateTime.UtcNow;
    }

    public static Portfolio Create(Currency baseCurrency = Currency.USD)
    {
        return new Portfolio(baseCurrency);
    }

    public void AddAccount(Account account)
    {
        if (account == null)
            throw new ArgumentNullException(nameof(account));

        Accounts.Add(account);
        LastUpdated = DateTime.UtcNow;
    }

    public void RemoveAccount(Account account)
    {
        if (account == null)
            throw new ArgumentNullException(nameof(account));

        Accounts.Remove(account);
        LastUpdated = DateTime.UtcNow;
    }

    public Money CalculateTotalNetWorth()
    {
        var total = Money.Zero(BaseCurrency);

        foreach (var account in Accounts)
        {
            total = total.Add(account.CalculateTotalValue(BaseCurrency));
        }

        return total;
    }

    public List<Asset> GetAllAssets()
    {
        return Accounts.SelectMany(a => a.Holdings).ToList();
    }

    public List<Token> GetAllTokens()
    {
        return GetAllAssets().OfType<Token>().ToList();
    }

    public List<Nft> GetAllNfts()
    {
        return GetAllAssets().OfType<Nft>().ToList();
    }

    public List<DeFiPosition> GetAllDeFiPositions()
    {
        return GetAllAssets().OfType<DeFiPosition>().ToList();
    }

    public AssetAllocation CalculateAllocation()
    {
        return AssetAllocation.Create(this);
    }

    public int GetTotalAssetCount() => GetAllAssets().Count;

    public override string ToString() => $"Portfolio: {CalculateTotalNetWorth()} ({Accounts.Count} accounts)";
}
