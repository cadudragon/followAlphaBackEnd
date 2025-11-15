using TrackFi.Domain.Enums;

namespace TrackFi.Domain.Entities;

/// <summary>
/// Represents a wallet in a user's watchlist.
/// Users can track interesting wallets without owning them.
/// </summary>
public class WatchlistEntry
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public string WalletAddress { get; private set; }
    public BlockchainNetwork Network { get; private set; }
    public string? Label { get; private set; }
    public string? Notes { get; private set; }
    public DateTime AddedAt { get; private set; }

    // Navigation
    public User User { get; private set; } = null!;

    // EF Core constructor
    private WatchlistEntry()
    {
        WalletAddress = string.Empty;
    }

    public WatchlistEntry(
        Guid userId,
        string walletAddress,
        BlockchainNetwork network,
        string? label = null,
        string? notes = null)
    {
        if (string.IsNullOrWhiteSpace(walletAddress))
            throw new ArgumentException("Wallet address cannot be empty", nameof(walletAddress));

        Id = Guid.NewGuid();
        UserId = userId;
        WalletAddress = walletAddress.Trim();
        Network = network;
        Label = label?.Trim();
        Notes = notes?.Trim();
        AddedAt = DateTime.UtcNow;
    }

    public void Update(string? label, string? notes)
    {
        Label = label?.Trim();
        Notes = notes?.Trim();
    }

    public override string ToString() => $"{Label ?? WalletAddress} ({Network})";
}
