using Microsoft.Extensions.Logging;
using Solnet.Wallet;
using System.Text;

namespace TrackFi.Infrastructure.Web3;

/// <summary>
/// Solana wallet signature validator.
/// Validates signatures from Solana wallets using Solnet library.
/// </summary>
public class SolanaSignatureValidator : ISignatureValidator
{
    private readonly ILogger<SolanaSignatureValidator> _logger;

    public SolanaSignatureValidator(ILogger<SolanaSignatureValidator> logger) => _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    public Task<bool> ValidateAsync(
        string walletAddress,
        string message,
        string signature,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(walletAddress))
        {
            _logger.LogWarning("Wallet address is empty");
            return Task.FromResult(false);
        }

        if (string.IsNullOrWhiteSpace(message))
        {
            _logger.LogWarning("Message is empty");
            return Task.FromResult(false);
        }

        if (string.IsNullOrWhiteSpace(signature))
        {
            _logger.LogWarning("Signature is empty");
            return Task.FromResult(false);
        }

        try
        {
            // Parse public key from wallet address
            var publicKey = new PublicKey(walletAddress);

            // Convert message to bytes
            var messageBytes = Encoding.UTF8.GetBytes(message);

            // Convert signature from base64/hex to bytes
            byte[] signatureBytes;
            try
            {
                signatureBytes = Convert.FromBase64String(signature);
            }
            catch
            {
                // Try hex if base64 fails
                signatureBytes = Convert.FromHexString(signature);
            }

            // Verify signature
            var isValid = publicKey.Verify(messageBytes, signatureBytes);

            if (!isValid)
            {
                _logger.LogWarning(
                    "Signature validation failed for Solana wallet: {WalletAddress}",
                    walletAddress);
            }
            else
            {
                _logger.LogInformation(
                    "Signature validated successfully for Solana wallet: {WalletAddress}",
                    walletAddress);
            }

            return Task.FromResult(isValid);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error validating Solana signature for wallet: {WalletAddress}",
                walletAddress);
            return Task.FromResult(false);
        }
    }
}
