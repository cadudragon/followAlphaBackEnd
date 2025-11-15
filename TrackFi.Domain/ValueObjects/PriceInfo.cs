namespace TrackFi.Domain.ValueObjects;

/// <summary>
/// Represents the price of an asset at a specific point in time.
/// </summary>
public sealed class PriceInfo : IEquatable<PriceInfo>
{
    public Money Price { get; }
    public DateTime Timestamp { get; }
    public string Source { get; }

    private PriceInfo(Money price, DateTime timestamp, string source)
    {
        Price = price ?? throw new ArgumentNullException(nameof(price));
        Timestamp = timestamp;
        Source = source ?? throw new ArgumentNullException(nameof(source));
    }

    public static PriceInfo Create(Money price, DateTime timestamp, string source)
    {
        if (string.IsNullOrWhiteSpace(source))
            throw new ArgumentException("Source cannot be empty", nameof(source));

        return new PriceInfo(price, timestamp, source);
    }

    public bool IsStale(TimeSpan maxAge)
    {
        return DateTime.UtcNow - Timestamp > maxAge;
    }

    public bool Equals(PriceInfo? other)
    {
        if (other is null) return false;
        return Price.Equals(other.Price) && Timestamp == other.Timestamp && Source == other.Source;
    }

    public override bool Equals(object? obj) => obj is PriceInfo other && Equals(other);

    public override int GetHashCode() => HashCode.Combine(Price, Timestamp, Source);

    public static bool operator ==(PriceInfo? left, PriceInfo? right)
    {
        if (left is null) return right is null;
        return left.Equals(right);
    }

    public static bool operator !=(PriceInfo? left, PriceInfo? right) => !(left == right);

    public override string ToString() => $"{Price} @ {Timestamp:u} (Source: {Source})";
}
