using TrackFi.Domain.Enums;

namespace TrackFi.Domain.ValueObjects;

/// <summary>
/// Represents an amount of money in a specific currency.
/// Immutable value object.
/// </summary>
public sealed class Money : IEquatable<Money>
{
    public decimal Amount { get; }
    public Currency Currency { get; }

    private Money(decimal amount, Currency currency)
    {
        if (amount < 0)
            throw new ArgumentException("Amount cannot be negative", nameof(amount));

        Amount = amount;
        Currency = currency;
    }

    public static Money Create(decimal amount, Currency currency)
    {
        return new Money(amount, currency);
    }

    public static Money Zero(Currency currency) => new Money(0, currency);

    public Money Add(Money other)
    {
        if (Currency != other.Currency)
            throw new InvalidOperationException($"Cannot add {other.Currency} to {Currency}");

        return new Money(Amount + other.Amount, Currency);
    }

    public Money Subtract(Money other)
    {
        if (Currency != other.Currency)
            throw new InvalidOperationException($"Cannot subtract {other.Currency} from {Currency}");

        if (Amount < other.Amount)
            throw new InvalidOperationException("Result would be negative");

        return new Money(Amount - other.Amount, Currency);
    }

    public Money Multiply(decimal factor)
    {
        if (factor < 0)
            throw new ArgumentException("Factor cannot be negative", nameof(factor));

        return new Money(Amount * factor, Currency);
    }

    public bool Equals(Money? other)
    {
        if (other is null) return false;
        return Amount == other.Amount && Currency == other.Currency;
    }

    public override bool Equals(object? obj) => obj is Money other && Equals(other);

    public override int GetHashCode() => HashCode.Combine(Amount, Currency);

    public static bool operator ==(Money? left, Money? right)
    {
        if (left is null) return right is null;
        return left.Equals(right);
    }

    public static bool operator !=(Money? left, Money? right) => !(left == right);

    public override string ToString() => $"{Amount:N2} {Currency}";
}
