using TrackFi.Domain.Enums;
using TrackFi.Domain.ValueObjects;

namespace TrackFi.Domain.Entities;

/// <summary>
/// Represents a cryptocurrency wallet account.
/// </summary>
public class Wallet : Account
{
    public WalletAddress Address { get; private set; }
    public BlockchainNetwork Network { get; private set; }

    private Wallet(WalletAddress address, string? name = null)
        : base(name ?? $"{address.Network} Wallet")
    {
        Address = address ?? throw new ArgumentNullException(nameof(address));
        Network = address.Network;
    }

    public static Wallet Create(WalletAddress address, string? name = null)
    {
        return new Wallet(address, name);
    }

    public List<Token> GetTokens()
    {
        return Holdings.OfType<Token>().ToList();
    }

    public List<Nft> GetNfts()
    {
        return Holdings.OfType<Nft>().ToList();
    }

    public List<DeFiPosition> GetDeFiPositions()
    {
        return Holdings.OfType<DeFiPosition>().ToList();
    }

    public override string ToString() => $"Wallet: {Address}";
}
