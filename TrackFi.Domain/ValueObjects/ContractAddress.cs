using TrackFi.Domain.Enums;

namespace TrackFi.Domain.ValueObjects;

/// <summary>
/// Represents a smart contract address on a blockchain.
/// </summary>
public sealed class ContractAddress : IEquatable<ContractAddress>
{
    public string Address { get; }
    public BlockchainNetwork Network { get; }

    private ContractAddress(string address, BlockchainNetwork network)
    {
        if (string.IsNullOrWhiteSpace(address))
            throw new ArgumentException("Contract address cannot be empty", nameof(address));

        Address = address.Trim();
        Network = network;
    }

    public static ContractAddress Create(string address, BlockchainNetwork network)
    {
        return new ContractAddress(address, network);
    }

    public bool Equals(ContractAddress? other)
    {
        if (other is null) return false;
        return Address.Equals(other.Address, StringComparison.OrdinalIgnoreCase) && Network == other.Network;
    }

    public override bool Equals(object? obj) => obj is ContractAddress other && Equals(other);

    public override int GetHashCode() => HashCode.Combine(Address.ToLowerInvariant(), Network);

    public static bool operator ==(ContractAddress? left, ContractAddress? right)
    {
        if (left is null) return right is null;
        return left.Equals(right);
    }

    public static bool operator !=(ContractAddress? left, ContractAddress? right) => !(left == right);

    public override string ToString() => $"{Address} ({Network})";
}
