namespace TrackFi.Infrastructure.DeFi;

/// <summary>
/// Configuration options for DeFi data provider.
/// </summary>
public class DeFiProviderOptions
{
    /// <summary>
    /// The provider to use (Zerion or Moralis).
    /// </summary>
    public DeFiProvider Provider { get; set; } = DeFiProvider.Zerion;

    /// <summary>
    /// Networks to query for DeFi positions.
    /// Configurable list to control which networks are enabled for multi-chain DeFi queries.
    /// In the future, this will be managed in the database per user/application.
    /// </summary>
    public string[] SupportedNetworks { get; set; } = [];

    /// <summary>
    /// Moralis API configuration.
    /// </summary>
    public MoralisOptions Moralis { get; set; } = new();

    /// <summary>
    /// Zerion API configuration.
    /// </summary>
    public ZerionOptions Zerion { get; set; } = new();
}

/// <summary>
/// Available DeFi data providers.
/// </summary>
public enum DeFiProvider
{
    Zerion,
    Moralis
}

/// <summary>
/// Moralis API configuration.
/// Reference: https://docs.moralis.io/web3-data-api
/// </summary>
public class MoralisOptions
{
    public string? ApiKey { get; set; }
    public string? BaseUrl { get; set; }

    /// <summary>
    /// Rate limiting configuration for Moralis API.
    /// </summary>
    public MoralisRateLimits RateLimits { get; set; } = new();
}

/// <summary>
/// Rate limiting configuration for Moralis API.
/// Reference: https://docs.moralis.io/web3-data-api/rate-limits
/// </summary>
public class MoralisRateLimits
{
    /// <summary>
    /// Compute units available per day.
    /// Free tier: 40,000 CU/day
    /// Different endpoints consume different amounts of compute units.
    /// Default: 40000
    /// </summary>
    public int ComputeUnitsPerDay { get; set; } = 40000;

    /// <summary>
    /// Maximum requests per second.
    /// Free tier: 25 requests/second
    /// Default: 25
    /// </summary>
    public int RequestsPerSecond { get; set; } = 25;

    /// <summary>
    /// Validates that rate limits are within acceptable ranges.
    /// </summary>
    public void Validate()
    {
        if (ComputeUnitsPerDay <= 0)
            throw new InvalidOperationException(
                $"{nameof(ComputeUnitsPerDay)} must be greater than 0. Current value: {ComputeUnitsPerDay}");

        if (RequestsPerSecond <= 0)
            throw new InvalidOperationException(
                $"{nameof(RequestsPerSecond)} must be greater than 0. Current value: {RequestsPerSecond}");
    }
}

/// <summary>
/// Zerion API configuration.
/// Reference: https://developers.zerion.io/reference
/// </summary>
public class ZerionOptions
{
    public string? ApiKey { get; set; }

    /// <summary>
    /// Rate limiting configuration for Zerion API.
    /// </summary>
    public ZerionRateLimits RateLimits { get; set; } = new();
}

/// <summary>
/// Rate limiting configuration for Zerion API.
/// Reference: https://developers.zerion.io/reference
/// </summary>
public class ZerionRateLimits
{
    /// <summary>
    /// Maximum concurrent requests to avoid overwhelming the API.
    /// Recommended: 10 concurrent requests
    /// Default: 10
    /// </summary>
    public int MaxConcurrentRequests { get; set; } = 10;

    /// <summary>
    /// Maximum requests per minute.
    /// Zerion documented limit: 100 requests/minute
    /// We use 90 by default to provide a safety margin.
    /// Default: 90
    /// </summary>
    public int RequestsPerMinute { get; set; } = 90;

    /// <summary>
    /// Maximum requests per day.
    /// Zerion documented limit: 3,000 requests/day
    /// We use 2800 by default to provide a safety margin.
    /// Default: 2800
    /// </summary>
    public int RequestsPerDay { get; set; } = 2800;

    /// <summary>
    /// Validates that rate limits are within acceptable ranges.
    /// </summary>
    public void Validate()
    {
        if (MaxConcurrentRequests <= 0)
            throw new InvalidOperationException(
                $"{nameof(MaxConcurrentRequests)} must be greater than 0. Current value: {MaxConcurrentRequests}");

        if (RequestsPerMinute <= 0)
            throw new InvalidOperationException(
                $"{nameof(RequestsPerMinute)} must be greater than 0. Current value: {RequestsPerMinute}");

        if (RequestsPerDay <= 0)
            throw new InvalidOperationException(
                $"{nameof(RequestsPerDay)} must be greater than 0. Current value: {RequestsPerDay}");

        // Warn if exceeding known API limits (but don't fail - plan may have changed)
        if (RequestsPerMinute > 100)
            Console.WriteLine(
                $"WARNING: {nameof(RequestsPerMinute)} is set to {RequestsPerMinute}, " +
                "which exceeds Zerion's documented limit of 100. This may cause API throttling.");

        if (RequestsPerDay > 3000)
            Console.WriteLine(
                $"WARNING: {nameof(RequestsPerDay)} is set to {RequestsPerDay}, " +
                "which exceeds Zerion's documented limit of 3000. This may cause API throttling.");
    }
}
