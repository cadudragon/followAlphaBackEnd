using TrackFi.Domain.Enums;
using TrackFi.Domain.ValueObjects;

namespace TrackFi.Domain.Entities;

/// <summary>
/// Represents a user profile in the TrackFi system.
/// Users connect via wallet signature (Web3 authentication).
/// </summary>
public class User
{
    public Guid Id { get; private set; }
    public string PrimaryWalletAddress { get; private set; }
    public BlockchainNetwork PrimaryWalletNetwork { get; private set; }
    public string? CoverPictureUrl { get; private set; }
    public string? CoverNftContract { get; private set; }
    public string? CoverNftTokenId { get; private set; }
    public BlockchainNetwork? CoverNftNetwork { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime LastActiveAt { get; private set; }

    // Navigation properties
    private readonly List<UserWallet> _wallets = new();
    private readonly List<WatchlistEntry> _watchlist = new();

    public IReadOnlyCollection<UserWallet> Wallets => _wallets.AsReadOnly();
    public IReadOnlyCollection<WatchlistEntry> Watchlist => _watchlist.AsReadOnly();

    // EF Core constructor
    private User()
    {
        PrimaryWalletAddress = string.Empty;
    }

    public User(string primaryWalletAddress, BlockchainNetwork network)
    {
        if (string.IsNullOrWhiteSpace(primaryWalletAddress))
            throw new ArgumentException("Primary wallet address cannot be empty", nameof(primaryWalletAddress));

        Id = Guid.NewGuid();
        PrimaryWalletAddress = primaryWalletAddress.Trim();
        PrimaryWalletNetwork = network;
        CreatedAt = DateTime.UtcNow;
        LastActiveAt = DateTime.UtcNow;
    }

    public void UpdateCoverPicture(string? url, string? nftContract, string? nftTokenId, BlockchainNetwork? nftNetwork)
    {
        CoverPictureUrl = url?.Trim();
        CoverNftContract = nftContract?.Trim();
        CoverNftTokenId = nftTokenId?.Trim();
        CoverNftNetwork = nftNetwork;
        LastActiveAt = DateTime.UtcNow;
    }

    public void UpdateLastActive()
    {
        LastActiveAt = DateTime.UtcNow;
    }

    public void AddWallet(UserWallet wallet)
    {
        if (wallet == null)
            throw new ArgumentNullException(nameof(wallet));

        if (wallet.UserId != Id)
            throw new InvalidOperationException("Wallet does not belong to this user");

        _wallets.Add(wallet);
        LastActiveAt = DateTime.UtcNow;
    }

    public void AddToWatchlist(WatchlistEntry entry)
    {
        if (entry == null)
            throw new ArgumentNullException(nameof(entry));

        if (entry.UserId != Id)
            throw new InvalidOperationException("Watchlist entry does not belong to this user");

        _watchlist.Add(entry);
        LastActiveAt = DateTime.UtcNow;
    }

    public void RemoveFromWatchlist(WatchlistEntry entry)
    {
        if (entry == null)
            throw new ArgumentNullException(nameof(entry));

        _watchlist.Remove(entry);
        LastActiveAt = DateTime.UtcNow;
    }

    public override string ToString() => $"User: {PrimaryWalletAddress} ({PrimaryWalletNetwork})";
}
