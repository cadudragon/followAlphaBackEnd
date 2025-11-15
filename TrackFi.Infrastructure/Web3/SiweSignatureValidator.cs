using Microsoft.Extensions.Logging;
using Nethereum.Signer;

namespace TrackFi.Infrastructure.Web3;

/// <summary>
/// Sign-In with Ethereum (SIWE) signature validator.
/// Validates signatures from EVM-compatible chains (Ethereum, Polygon, Arbitrum).
/// Uses Nethereum library for cryptographic operations.
/// </summary>
public class SiweSignatureValidator : ISignatureValidator
{
    private readonly ILogger<SiweSignatureValidator> _logger;
    private readonly EthereumMessageSigner _signer;

    public SiweSignatureValidator(ILogger<SiweSignatureValidator> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _signer = new EthereumMessageSigner();
    }

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
            // Recover the address that signed the message
            var recoveredAddress = _signer.EncodeUTF8AndEcRecover(message, signature);

            // Compare with claimed wallet address (case-insensitive)
            var isValid = recoveredAddress.Equals(walletAddress, StringComparison.OrdinalIgnoreCase);

            if (!isValid)
            {
                _logger.LogWarning(
                    "Signature validation failed. Expected: {ExpectedAddress}, Recovered: {RecoveredAddress}",
                    walletAddress,
                    recoveredAddress);
            }
            else
            {
                _logger.LogInformation(
                    "Signature validated successfully for wallet: {WalletAddress}",
                    walletAddress);
            }

            return Task.FromResult(isValid);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error validating signature for wallet: {WalletAddress}",
                walletAddress);
            return Task.FromResult(false);
        }
    }
}
