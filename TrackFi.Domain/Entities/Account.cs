using TrackFi.Domain.Enums;
using TrackFi.Domain.ValueObjects;

namespace TrackFi.Domain.Entities;

/// <summary>
/// Abstract base class for all account types.
/// An account is a container for assets.
/// </summary>
public abstract class Account
{
    public Guid Id { get; protected set; }
    public string Name { get; protected set; }
    public List<Asset> Holdings { get; protected set; }
    public DateTime LastUpdated { get; protected set; }

    protected Account(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Account name cannot be empty", nameof(name));

        Id = Guid.NewGuid();
        Name = name.Trim();
        Holdings = new List<Asset>();
        LastUpdated = DateTime.UtcNow;
    }

    public void AddHolding(Asset asset)
    {
        if (asset == null)
            throw new ArgumentNullException(nameof(asset));

        Holdings.Add(asset);
        LastUpdated = DateTime.UtcNow;
    }

    public void RemoveHolding(Asset asset)
    {
        if (asset == null)
            throw new ArgumentNullException(nameof(asset));

        Holdings.Remove(asset);
        LastUpdated = DateTime.UtcNow;
    }

    public void ClearHoldings()
    {
        Holdings.Clear();
        LastUpdated = DateTime.UtcNow;
    }

    public Money CalculateTotalValue(Currency currency)
    {
        var total = Money.Zero(currency);

        foreach (var asset in Holdings)
        {
            total = total.Add(asset.CalculateValue(currency));
        }

        return total;
    }

    public int GetAssetCount() => Holdings.Count;
}
