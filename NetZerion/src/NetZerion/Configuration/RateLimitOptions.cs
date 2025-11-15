namespace NetZerion.Configuration;

/// <summary>
/// Configuration options for rate limiting.
/// </summary>
public class RateLimitOptions
{
    /// <summary>
    /// Maximum number of requests allowed per day.
    /// Default: 3000 (Zerion free tier limit).
    /// </summary>
    public int RequestsPerDay { get; set; } = 3000;

    /// <summary>
    /// Maximum number of requests allowed per minute (soft limit).
    /// Default: 100.
    /// </summary>
    public int RequestsPerMinute { get; set; } = 100;

    /// <summary>
    /// Enable automatic throttling to stay within rate limits.
    /// When enabled, requests will be delayed if approaching limits.
    /// Default: true.
    /// </summary>
    public bool EnableAutoThrottling { get; set; } = true;

    /// <summary>
    /// Throw <see cref="Exceptions.RateLimitException"/> when rate limit is exceeded.
    /// When false, the client will wait and retry instead of throwing.
    /// Default: false.
    /// </summary>
    public bool ThrowOnRateLimit { get; set; } = false;

    /// <summary>
    /// Enable rate limit tracking and enforcement.
    /// When disabled, no rate limiting will be applied (use with caution).
    /// Default: true.
    /// </summary>
    public bool EnableRateLimiting { get; set; } = true;
}
