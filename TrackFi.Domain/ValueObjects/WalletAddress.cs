using TrackFi.Domain.Enums;

namespace TrackFi.Domain.ValueObjects;

/// <summary>
/// Represents a blockchain wallet address.
/// </summary>
public sealed class WalletAddress : IEquatable<WalletAddress>
{
    public string Address { get; }
    public BlockchainNetwork Network { get; }

    private WalletAddress(string address, BlockchainNetwork network)
    {
        if (string.IsNullOrWhiteSpace(address))
            throw new ArgumentException("Address cannot be empty", nameof(address));

        Address = address.Trim();
        Network = network;
    }

    public static WalletAddress Create(string address, BlockchainNetwork network)
    {
        return new WalletAddress(address, network);
    }

    public bool Equals(WalletAddress? other)
    {
        if (other is null) return false;
        return Address.Equals(other.Address, StringComparison.OrdinalIgnoreCase) && Network == other.Network;
    }

    public override bool Equals(object? obj) => obj is WalletAddress other && Equals(other);

    public override int GetHashCode() => HashCode.Combine(Address.ToLowerInvariant(), Network);

    public static bool operator ==(WalletAddress? left, WalletAddress? right)
    {
        if (left is null) return right is null;
        return left.Equals(right);
    }

    public static bool operator !=(WalletAddress? left, WalletAddress? right) => !(left == right);

    public override string ToString() => $"{Address} ({Network})";
}
