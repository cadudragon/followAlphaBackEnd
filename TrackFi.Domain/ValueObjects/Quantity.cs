namespace TrackFi.Domain.ValueObjects;

/// <summary>
/// Represents a generic quantity with decimal precision.
/// Used for token amounts, shares, etc.
/// </summary>
public sealed class Quantity : IEquatable<Quantity>, IComparable<Quantity>
{
    public decimal Value { get; }

    private Quantity(decimal value)
    {
        if (value < 0)
            throw new ArgumentException("Quantity cannot be negative", nameof(value));

        Value = value;
    }

    public static Quantity Create(decimal value) => new Quantity(value);

    public static Quantity Zero => new Quantity(0);

    public Quantity Add(Quantity other) => new Quantity(Value + other.Value);

    public Quantity Subtract(Quantity other)
    {
        if (Value < other.Value)
            throw new InvalidOperationException("Result would be negative");

        return new Quantity(Value - other.Value);
    }

    public Quantity Multiply(decimal factor)
    {
        if (factor < 0)
            throw new ArgumentException("Factor cannot be negative", nameof(factor));

        return new Quantity(Value * factor);
    }

    public bool Equals(Quantity? other)
    {
        if (other is null) return false;
        return Value == other.Value;
    }

    public override bool Equals(object? obj) => obj is Quantity other && Equals(other);

    public override int GetHashCode() => Value.GetHashCode();

    public int CompareTo(Quantity? other)
    {
        if (other is null) return 1;
        return Value.CompareTo(other.Value);
    }

    public static bool operator ==(Quantity? left, Quantity? right)
    {
        if (left is null) return right is null;
        return left.Equals(right);
    }

    public static bool operator !=(Quantity? left, Quantity? right) => !(left == right);

    public static bool operator >(Quantity left, Quantity right) => left.CompareTo(right) > 0;

    public static bool operator <(Quantity left, Quantity right) => left.CompareTo(right) < 0;

    public static bool operator >=(Quantity left, Quantity right) => left.CompareTo(right) >= 0;

    public static bool operator <=(Quantity left, Quantity right) => left.CompareTo(right) <= 0;

    public override string ToString() => Value.ToString("N8");
}
