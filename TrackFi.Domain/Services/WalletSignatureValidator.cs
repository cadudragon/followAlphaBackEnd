using TrackFi.Domain.Enums;

namespace TrackFi.Domain.Services;

/// <summary>
/// Domain service for validating wallet signature ownership.
/// Note: Actual cryptographic validation is implemented in Infrastructure layer.
/// This provides the domain contract and basic validation logic.
/// </summary>
public class WalletSignatureValidator
{
    /// <summary>
    /// Validates that a signature message meets basic requirements.
    /// </summary>
    public bool IsValidMessageFormat(string message, string nonce)
    {
        if (string.IsNullOrWhiteSpace(message))
            return false;

        if (string.IsNullOrWhiteSpace(nonce))
            return false;

        // Message should contain the nonce
        if (!message.Contains(nonce))
            return false;

        // Message should contain "TrackFi" or "TrackFI"
        if (!message.Contains("TrackFi", StringComparison.OrdinalIgnoreCase))
            return false;

        return true;
    }

    /// <summary>
    /// Validates that a nonce hasn't expired.
    /// </summary>
    public bool IsNonceValid(DateTime nonceTimestamp, TimeSpan maxAge)
    {
        var age = DateTime.UtcNow - nonceTimestamp;
        return age <= maxAge;
    }

    /// <summary>
    /// Creates a standard message for wallet signature.
    /// </summary>
    public string CreateSignatureMessage(string walletAddress, BlockchainNetwork network, string nonce)
    {
        if (string.IsNullOrWhiteSpace(walletAddress))
            throw new ArgumentException("Wallet address cannot be empty", nameof(walletAddress));

        if (string.IsNullOrWhiteSpace(nonce))
            throw new ArgumentException("Nonce cannot be empty", nameof(nonce));

        var timestamp = DateTime.UtcNow.ToString("o");

        return $"""
            Sign in to TrackFi

            Wallet: {walletAddress}
            Network: {network}
            Nonce: {nonce}
            Timestamp: {timestamp}

            This signature proves you own this wallet.
            """;
    }

    /// <summary>
    /// Generates a secure random nonce.
    /// </summary>
    public string GenerateNonce()
    {
        return Guid.NewGuid().ToString("N");
    }
}
