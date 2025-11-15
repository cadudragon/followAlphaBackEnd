namespace TrackFi.Domain.ValueObjects;

/// <summary>
/// Uniquely identifies an NFT by contract address and token ID.
/// </summary>
public sealed class NftIdentifier : IEquatable<NftIdentifier>
{
    public ContractAddress ContractAddress { get; }
    public string TokenId { get; }

    private NftIdentifier(ContractAddress contractAddress, string tokenId)
    {
        ContractAddress = contractAddress ?? throw new ArgumentNullException(nameof(contractAddress));

        if (string.IsNullOrWhiteSpace(tokenId))
            throw new ArgumentException("Token ID cannot be empty", nameof(tokenId));

        TokenId = tokenId.Trim();
    }

    public static NftIdentifier Create(ContractAddress contractAddress, string tokenId)
    {
        return new NftIdentifier(contractAddress, tokenId);
    }

    public bool Equals(NftIdentifier? other)
    {
        if (other is null) return false;
        return ContractAddress.Equals(other.ContractAddress) &&
               TokenId.Equals(other.TokenId, StringComparison.OrdinalIgnoreCase);
    }

    public override bool Equals(object? obj) => obj is NftIdentifier other && Equals(other);

    public override int GetHashCode() => HashCode.Combine(ContractAddress, TokenId.ToLowerInvariant());

    public static bool operator ==(NftIdentifier? left, NftIdentifier? right)
    {
        if (left is null) return right is null;
        return left.Equals(right);
    }

    public static bool operator !=(NftIdentifier? left, NftIdentifier? right) => !(left == right);

    public override string ToString() => $"{ContractAddress}/{TokenId}";
}
