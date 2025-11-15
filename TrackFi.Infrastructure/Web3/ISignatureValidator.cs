namespace TrackFi.Infrastructure.Web3;

/// <summary>
/// Interface for Web3 wallet signature validation.
/// Implementations validate cryptographic signatures for different blockchain networks.
/// </summary>
public interface ISignatureValidator
{
    /// <summary>
    /// Validates that a signature was created by the claimed wallet address.
    /// </summary>
    /// <param name="walletAddress">The wallet address claiming to have signed the message</param>
    /// <param name="message">The original message that was signed</param>
    /// <param name="signature">The cryptographic signature</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if signature is valid, false otherwise</returns>
    Task<bool> ValidateAsync(
        string walletAddress,
        string message,
        string signature,
        CancellationToken cancellationToken = default);
}
