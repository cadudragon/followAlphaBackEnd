using TrackFi.Domain.Entities;
using TrackFi.Domain.Enums;
using TrackFi.Domain.ValueObjects;

namespace TrackFi.Domain.Interfaces;

/// <summary>
/// Contract for fetching asset prices from external sources.
/// Implemented by Infrastructure layer (e.g., CoinGeckoService).
/// </summary>
public interface IPriceProvider
{
    /// <summary>
    /// Fetches current price for a single asset.
    /// </summary>
    Task<PriceInfo?> GetPriceAsync(Asset asset, Currency currency, CancellationToken cancellationToken = default);

    /// <summary>
    /// Fetches current prices for multiple assets in batch.
    /// </summary>
    Task<Dictionary<Guid, PriceInfo>> GetPricesAsync(List<Asset> assets, Currency currency, CancellationToken cancellationToken = default);

    /// <summary>
    /// Fetches current price for a token by contract address.
    /// </summary>
    Task<PriceInfo?> GetTokenPriceAsync(ContractAddress contractAddress, Currency currency, CancellationToken cancellationToken = default);
}
