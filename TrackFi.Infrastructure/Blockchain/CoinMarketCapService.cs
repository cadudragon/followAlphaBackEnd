using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Json;
using TrackFi.Domain.Enums;

namespace TrackFi.Infrastructure.Blockchain;

/// <summary>
/// Service for interacting with CoinMarketCap API to verify token legitimacy.
/// </summary>
public class CoinMarketCapService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<CoinMarketCapService> _logger;
    private readonly string _apiKey;
    private const string BaseUrl = "https://pro-api.coinmarketcap.com";

    public CoinMarketCapService(
        HttpClient httpClient,
        IOptions<CoinMarketCapOptions> options,
        ILogger<CoinMarketCapService> logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _apiKey = options.Value.ApiKey ?? throw new ArgumentNullException(nameof(options.Value.ApiKey));

        _httpClient.BaseAddress = new Uri(BaseUrl);
        _httpClient.DefaultRequestHeaders.Add("X-CMC_PRO_API_KEY", _apiKey);
        _httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
    }

    /// <summary>
    /// Checks if tokens exist in CoinMarketCap by their symbols.
    /// Returns a dictionary mapping symbol to cryptocurrency data.
    /// </summary>
    /// <param name="symbols">List of token symbols to check (e.g., ["BTC", "ETH", "USDC"])</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Dictionary mapping symbol to CMC cryptocurrency data</returns>
    public async Task<Dictionary<string, CmcCryptocurrency>> GetCryptocurrenciesBySymbolsAsync(
        List<string> symbols,
        CancellationToken cancellationToken = default)
    {
        if (symbols == null || symbols.Count == 0)
        {
            return [];
        }

        try
        {
            var symbolsParam = string.Join(",", symbols.Select(s => s.ToUpperInvariant()));
            var endpoint = $"/v1/cryptocurrency/map?symbol={symbolsParam}";

            _logger.LogInformation(
                "Querying CoinMarketCap for {Count} symbols: {Symbols}",
                symbols.Count,
                symbolsParam);

            var response = await _httpClient.GetAsync(endpoint, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError(
                    "CoinMarketCap API error: {StatusCode} - {Error}",
                    response.StatusCode,
                    errorContent);
                return [];
            }

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            var cmcResponse = JsonSerializer.Deserialize<CoinMarketCapMapResponse>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (cmcResponse == null || cmcResponse.Status.ErrorCode != 0)
            {
                _logger.LogError(
                    "CoinMarketCap API returned error: {ErrorCode} - {ErrorMessage}",
                    cmcResponse?.Status.ErrorCode,
                    cmcResponse?.Status.ErrorMessage);
                return [];
            }

            _logger.LogInformation(
                "Found {Count} cryptocurrencies in CoinMarketCap",
                cmcResponse.Data.Count);

            // Map by symbol (case-insensitive)
            return cmcResponse.Data
                .GroupBy(c => c.Symbol.ToUpperInvariant())
                .ToDictionary(
                    g => g.Key,
                    g => g.First(), // Take first if multiple matches
                    StringComparer.OrdinalIgnoreCase);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error querying CoinMarketCap API for symbols: {Symbols}", string.Join(",", symbols));
            return [];
        }
    }

    /// <summary>
    /// Gets detailed information about a specific cryptocurrency by symbol.
    /// </summary>
    public async Task<CmcCryptocurrency?> GetCryptocurrencyBySymbolAsync(
        string symbol,
        CancellationToken cancellationToken = default)
    {
        var result = await GetCryptocurrenciesBySymbolsAsync([symbol], cancellationToken);
        return result.TryGetValue(symbol.ToUpperInvariant(), out var crypto) ? crypto : null;
    }
}

/// <summary>
/// Configuration options for CoinMarketCap service.
/// </summary>
public class CoinMarketCapOptions
{
    public const string SectionName = "CoinMarketCap";

    public string ApiKey { get; set; } = string.Empty;
}
