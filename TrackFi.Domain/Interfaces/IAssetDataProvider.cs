using TrackFi.Domain.Entities;
using TrackFi.Domain.ValueObjects;

namespace TrackFi.Domain.Interfaces;

/// <summary>
/// Contract for fetching asset holdings from external data sources.
/// Implemented by Infrastructure layer (e.g., AlchemyService, HeliusService).
/// </summary>
public interface IAssetDataProvider
{
    /// <summary>
    /// Fetches all holdings for a given wallet address.
    /// </summary>
    Task<List<Asset>> GetHoldingsAsync(WalletAddress walletAddress, CancellationToken cancellationToken = default);

    /// <summary>
    /// Fetches only token holdings.
    /// </summary>
    Task<List<Token>> GetTokensAsync(WalletAddress walletAddress, CancellationToken cancellationToken = default);

    /// <summary>
    /// Fetches only NFT holdings.
    /// </summary>
    Task<List<Nft>> GetNftsAsync(WalletAddress walletAddress, CancellationToken cancellationToken = default);

    /// <summary>
    /// Fetches only DeFi positions.
    /// </summary>
    Task<List<DeFiPosition>> GetDeFiPositionsAsync(WalletAddress walletAddress, CancellationToken cancellationToken = default);
}
