using System.Text.Json.Serialization;

namespace TrackFi.Infrastructure.Blockchain;

/// <summary>
/// CoinMarketCap API response for cryptocurrency map endpoint.
/// </summary>
public class CoinMarketCapMapResponse
{
    [JsonPropertyName("data")]
    public List<CmcCryptocurrency> Data { get; set; } = new();

    [JsonPropertyName("status")]
    public CmcStatus Status { get; set; } = new();
}

/// <summary>
/// Cryptocurrency data from CoinMarketCap.
/// </summary>
public class CmcCryptocurrency
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("rank")]
    public int? Rank { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("symbol")]
    public string Symbol { get; set; } = string.Empty;

    [JsonPropertyName("slug")]
    public string Slug { get; set; } = string.Empty;

    [JsonPropertyName("is_active")]
    public int IsActive { get; set; }

    [JsonPropertyName("status")]
    public string? StatusText { get; set; }

    [JsonPropertyName("first_historical_data")]
    public DateTime? FirstHistoricalData { get; set; }

    [JsonPropertyName("last_historical_data")]
    public DateTime? LastHistoricalData { get; set; }

    [JsonPropertyName("platform")]
    public CmcPlatform? Platform { get; set; }
}

/// <summary>
/// Platform information for tokens (parent blockchain).
/// </summary>
public class CmcPlatform
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("symbol")]
    public string Symbol { get; set; } = string.Empty;

    [JsonPropertyName("slug")]
    public string Slug { get; set; } = string.Empty;

    [JsonPropertyName("token_address")]
    public string TokenAddress { get; set; } = string.Empty;
}

/// <summary>
/// API response status information.
/// </summary>
public class CmcStatus
{
    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; set; }

    [JsonPropertyName("error_code")]
    public int ErrorCode { get; set; }

    [JsonPropertyName("error_message")]
    public string? ErrorMessage { get; set; }

    [JsonPropertyName("elapsed")]
    public int Elapsed { get; set; }

    [JsonPropertyName("credit_count")]
    public int CreditCount { get; set; }

    [JsonPropertyName("notice")]
    public string? Notice { get; set; }
}
