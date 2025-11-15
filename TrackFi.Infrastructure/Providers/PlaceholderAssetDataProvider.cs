using Microsoft.Extensions.Logging;
using TrackFi.Domain.Entities;
using TrackFi.Domain.Enums;
using TrackFi.Domain.Interfaces;
using TrackFi.Domain.ValueObjects;

namespace TrackFi.Infrastructure.Providers;

/// <summary>
/// Placeholder implementation of IAssetDataProvider.
/// V1: Returns empty lists - will be replaced with real blockchain API integration.
/// TODO V2: Implement Alchemy for EVM and Helius for Solana.
/// </summary>
public class PlaceholderAssetDataProvider : IAssetDataProvider
{
    private readonly ILogger<PlaceholderAssetDataProvider> _logger;

    public PlaceholderAssetDataProvider(ILogger<PlaceholderAssetDataProvider> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public Task<List<Asset>> GetHoldingsAsync(
        WalletAddress walletAddress,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Placeholder: GetHoldingsAsync called for {WalletAddress}. Returning empty list.",
            walletAddress);

        // V1: Return empty list
        // TODO V2: Call Alchemy API for EVM or Helius API for Solana
        return Task.FromResult(new List<Asset>());
    }

    public Task<List<Token>> GetTokensAsync(
        WalletAddress walletAddress,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Placeholder: GetTokensAsync called for {WalletAddress}. Returning empty list.",
            walletAddress);

        // V1: Return empty list
        // TODO V2: Fetch tokens from blockchain APIs
        return Task.FromResult(new List<Token>());
    }

    public Task<List<Nft>> GetNftsAsync(
        WalletAddress walletAddress,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Placeholder: GetNftsAsync called for {WalletAddress}. Returning empty list.",
            walletAddress);

        // V1: Return empty list
        // TODO V2: Fetch NFTs from blockchain APIs
        return Task.FromResult(new List<Nft>());
    }

    public Task<List<DeFiPosition>> GetDeFiPositionsAsync(
        WalletAddress walletAddress,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Placeholder: GetDeFiPositionsAsync called for {WalletAddress}. Returning empty list.",
            walletAddress);

        // V1: Return empty list
        // TODO V2: Fetch DeFi positions from blockchain APIs
        return Task.FromResult(new List<DeFiPosition>());
    }
}
