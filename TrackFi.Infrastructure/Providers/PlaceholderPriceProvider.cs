using Microsoft.Extensions.Logging;
using TrackFi.Domain.Entities;
using TrackFi.Domain.Enums;
using TrackFi.Domain.Interfaces;
using TrackFi.Domain.ValueObjects;

namespace TrackFi.Infrastructure.Providers;

/// <summary>
/// Placeholder implementation of IPriceProvider.
/// V1: Returns null prices - will be replaced with real price API integration.
/// TODO V2: Implement CoinGecko or CoinMarketCap integration.
/// </summary>
public class PlaceholderPriceProvider : IPriceProvider
{
    private readonly ILogger<PlaceholderPriceProvider> _logger;

    public PlaceholderPriceProvider(ILogger<PlaceholderPriceProvider> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public Task<PriceInfo?> GetPriceAsync(
        Asset asset,
        Currency currency,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Placeholder: GetPriceAsync called for asset {AssetId}. Returning null.",
            asset.Id);

        // V1: Return null
        // TODO V2: Call CoinGecko API for real prices
        return Task.FromResult<PriceInfo?>(null);
    }

    public Task<Dictionary<Guid, PriceInfo>> GetPricesAsync(
        List<Asset> assets,
        Currency currency,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Placeholder: GetPricesAsync called for {AssetCount} assets. Returning empty dictionary.",
            assets.Count);

        // V1: Return empty dictionary
        // TODO V2: Batch fetch prices from CoinGecko API
        return Task.FromResult(new Dictionary<Guid, PriceInfo>());
    }

    public Task<PriceInfo?> GetTokenPriceAsync(
        ContractAddress contractAddress,
        Currency currency,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Placeholder: GetTokenPriceAsync called for {ContractAddress}. Returning null.",
            contractAddress);

        // V1: Return null
        // TODO V2: Call CoinGecko API for token price by contract address
        return Task.FromResult<PriceInfo?>(null);
    }
}
