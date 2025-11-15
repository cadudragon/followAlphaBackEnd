using TrackFi.Domain.Enums;

namespace TrackFi.Domain.Entities;

/// <summary>
/// Represents a wallet verified and owned by a user.
/// Verified through cryptographic signature.
/// </summary>
public class UserWallet
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public string WalletAddress { get; private set; }
    public BlockchainNetwork Network { get; private set; }
    public string? Label { get; private set; }
    public bool IsVerified { get; private set; }
    public string? SignatureProof { get; private set; }
    public string? SignatureMessage { get; private set; }
    public DateTime? VerifiedAt { get; private set; }
    public DateTime AddedAt { get; private set; }

    // Navigation
    public User User { get; private set; } = null!;

    // EF Core constructor
    private UserWallet()
    {
        WalletAddress = string.Empty;
    }

    public UserWallet(Guid userId, string walletAddress, BlockchainNetwork network, string? label = null)
    {
        if (string.IsNullOrWhiteSpace(walletAddress))
            throw new ArgumentException("Wallet address cannot be empty", nameof(walletAddress));

        Id = Guid.NewGuid();
        UserId = userId;
        WalletAddress = walletAddress.Trim();
        Network = network;
        Label = label?.Trim();
        IsVerified = false;
        AddedAt = DateTime.UtcNow;
    }

    public void Verify(string signature, string message)
    {
        if (string.IsNullOrWhiteSpace(signature))
            throw new ArgumentException("Signature cannot be empty", nameof(signature));

        if (string.IsNullOrWhiteSpace(message))
            throw new ArgumentException("Message cannot be empty", nameof(message));

        IsVerified = true;
        SignatureProof = signature;
        SignatureMessage = message;
        VerifiedAt = DateTime.UtcNow;
    }

    public void UpdateLabel(string label)
    {
        if (string.IsNullOrWhiteSpace(label))
            throw new ArgumentException("Label cannot be empty", nameof(label));

        Label = label.Trim();
    }

    public override string ToString() => $"{Label ?? WalletAddress} ({Network}) - Verified: {IsVerified}";
}
