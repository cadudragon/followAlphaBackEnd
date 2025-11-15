namespace TrackFi.Domain.ValueObjects;

/// <summary>
/// Represents metadata about an asset (name, symbol, logo, etc.).
/// </summary>
public sealed class AssetMetadata : IEquatable<AssetMetadata>
{
    public string Name { get; }
    public string Symbol { get; }
    public string? LogoUrl { get; }
    public string? Description { get; }
    public int? Decimals { get; }

    private AssetMetadata(
        string name,
        string symbol,
        string? logoUrl = null,
        string? description = null,
        int? decimals = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name cannot be empty", nameof(name));

        if (string.IsNullOrWhiteSpace(symbol))
            throw new ArgumentException("Symbol cannot be empty", nameof(symbol));

        if (decimals.HasValue && decimals.Value < 0)
            throw new ArgumentException("Decimals cannot be negative", nameof(decimals));

        Name = name.Trim();
        Symbol = symbol.Trim().ToUpperInvariant();
        LogoUrl = logoUrl?.Trim();
        Description = description?.Trim();
        Decimals = decimals;
    }

    public static AssetMetadata Create(
        string name,
        string symbol,
        string? logoUrl = null,
        string? description = null,
        int? decimals = null)
    {
        return new AssetMetadata(name, symbol, logoUrl, description, decimals);
    }

    public bool Equals(AssetMetadata? other)
    {
        if (other is null) return false;
        return Name == other.Name &&
               Symbol == other.Symbol &&
               LogoUrl == other.LogoUrl &&
               Description == other.Description &&
               Decimals == other.Decimals;
    }

    public override bool Equals(object? obj) => obj is AssetMetadata other && Equals(other);

    public override int GetHashCode() => HashCode.Combine(Name, Symbol, LogoUrl, Description, Decimals);

    public static bool operator ==(AssetMetadata? left, AssetMetadata? right)
    {
        if (left is null) return right is null;
        return left.Equals(right);
    }

    public static bool operator !=(AssetMetadata? left, AssetMetadata? right) => !(left == right);

    public override string ToString() => $"{Name} ({Symbol})";
}
