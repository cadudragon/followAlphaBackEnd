using Microsoft.Extensions.Logging;
using TrackFi.Application.Interfaces;
using TrackFi.Application.Portfolio.DTOs;
using TrackFi.Infrastructure.Caching;

namespace TrackFi.Infrastructure.Portfolio;

/// <summary>
/// Unified portfolio service using IPortfolioProvider abstraction.
/// Responsibilities:
/// 1. Check cache
/// 2. Call provider if cache miss
/// 3. Cache result
/// 4. Return to caller
///
/// Provider is responsible for fetching, aggregating, categorizing, and transforming data.
/// This service is just a thin caching + orchestration layer.
/// </summary>
public class PortfolioService
{
    private readonly IPortfolioProvider _portfolioProvider;
    private readonly DistributedCacheService _cache;
    private readonly ILogger<PortfolioService> _logger;

    // Cache TTL: 5 minutes (structure cache)
    // TODO (FASE 7): Separate price cache with 1-minute TTL
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);

    public PortfolioService(
        IPortfolioProvider portfolioProvider,
        DistributedCacheService cache,
        ILogger<PortfolioService> logger)
    {
        _portfolioProvider = portfolioProvider ?? throw new ArgumentNullException(nameof(portfolioProvider));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Get wallet positions (tokens in wallet) with caching.
    /// </summary>
    public async Task<MultiNetworkWalletDto> GetWalletPositionsAsync(
        string walletAddress,
        List<string> networks,
        CancellationToken cancellationToken = default)
    {
        var cacheKey = GenerateCacheKey("wallet", walletAddress, networks);

        // Check cache
        var cached = await _cache.GetAsync<MultiNetworkWalletDto>(cacheKey, cancellationToken);
        if (cached != null)
        {
            _logger.LogInformation(
                "Cache HIT for wallet positions {Wallet} ({Networks})",
                walletAddress,
                string.Join(", ", networks));

            return cached;
        }

        _logger.LogInformation(
            "Cache MISS - fetching wallet positions from provider for {Wallet} ({Networks})",
            walletAddress,
            string.Join(", ", networks));

        // Fetch from provider (provider handles aggregation + transformation + metadata)
        var result = await _portfolioProvider.GetWalletPositionsAsync(
            walletAddress,
            networks,
            cancellationToken);

        // Cache result
        await _cache.SetAsync(cacheKey, result, CacheDuration, cancellationToken);

        _logger.LogInformation(
            "Cached wallet positions for {Wallet}: {TokenCount} tokens, ${Value:N2}",
            walletAddress,
            result.Summary.TotalTokens,
            result.Summary.TotalValueUsd);

        return result;
    }

    /// <summary>
    /// Get DeFi positions (lending, staking, farming, etc.) with caching.
    /// </summary>
    public async Task<MultiNetworkDeFiPortfolioDto> GetDeFiPositionsAsync(
        string walletAddress,
        List<string> networks,
        CancellationToken cancellationToken = default)
    {
        var cacheKey = GenerateCacheKey("defi", walletAddress, networks);

        // Check cache
        var cached = await _cache.GetAsync<MultiNetworkDeFiPortfolioDto>(cacheKey, cancellationToken);
        if (cached != null)
        {
            _logger.LogInformation(
                "Cache HIT for DeFi positions {Wallet} ({Networks})",
                walletAddress,
                string.Join(", ", networks));

            return cached;
        }

        _logger.LogInformation(
            "Cache MISS - fetching DeFi positions from provider for {Wallet} ({Networks})",
            walletAddress,
            string.Join(", ", networks));

        // Fetch from provider (provider handles aggregation + transformation + metadata)
        var result = await _portfolioProvider.GetDeFiPositionsAsync(
            walletAddress,
            networks,
            cancellationToken);

        // Cache result
        await _cache.SetAsync(cacheKey, result, CacheDuration, cancellationToken);

        _logger.LogInformation(
            "Cached DeFi positions for {Wallet}: ${Value:N2} across {NetworkCount} networks",
            walletAddress,
            result.TotalValueUsd,
            result.Networks.Count);

        return result;
    }

    /// <summary>
    /// Get full portfolio (wallet + DeFi) with caching.
    /// </summary>
    public async Task<FullPortfolioDto> GetFullPortfolioAsync(
        string walletAddress,
        List<string> networks,
        CancellationToken cancellationToken = default)
    {
        var cacheKey = GenerateCacheKey("full", walletAddress, networks);

        // Check cache
        var cached = await _cache.GetAsync<FullPortfolioDto>(cacheKey, cancellationToken);
        if (cached != null)
        {
            _logger.LogInformation(
                "Cache HIT for full portfolio {Wallet} ({Networks})",
                walletAddress,
                string.Join(", ", networks));

            return cached;
        }

        _logger.LogInformation(
            "Cache MISS - fetching full portfolio from provider for {Wallet} ({Networks})",
            walletAddress,
            string.Join(", ", networks));

        // Fetch from provider (provider handles aggregation + transformation + metadata)
        var result = await _portfolioProvider.GetFullPortfolioAsync(
            walletAddress,
            networks,
            cancellationToken);

        // Cache result
        await _cache.SetAsync(cacheKey, result, CacheDuration, cancellationToken);

        _logger.LogInformation(
            "Cached full portfolio for {Wallet}: ${Value:N2} across {NetworkCount} networks",
            walletAddress,
            result.TotalValueUsd,
            result.Networks.Count);

        return result;
    }

    /// <summary>
    /// Generates cache key for portfolio data.
    /// Format: portfolio:{type}:{wallet}:{networks}
    /// </summary>
    private static string GenerateCacheKey(string type, string walletAddress, List<string> networks)
    {
        var networksKey = string.Join(",", networks.OrderBy(n => n));
        return $"portfolio:{type}:{walletAddress.ToLowerInvariant()}:{networksKey}";
    }
}
