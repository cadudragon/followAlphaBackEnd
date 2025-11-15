namespace NetZerion.Configuration;

/// <summary>
/// Configuration options for the NetZerion client.
/// </summary>
public class NetZerionOptions
{
    /// <summary>
    /// Base URL for the Zerion API.
    /// Default: https://api.zerion.io/v1/
    /// IMPORTANT: Must end with trailing slash for HttpClient to work correctly
    /// </summary>
    public string BaseUrl { get; set; } = "https://api.zerion.io/v1/";

    /// <summary>
    /// Zerion API key for authentication.
    /// Required for all API requests.
    /// </summary>
    public string? ApiKey { get; set; }

    /// <summary>
    /// HTTP request timeout.
    /// Default: 30 seconds.
    /// </summary>
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Maximum number of retry attempts for failed requests.
    /// Default: 3.
    /// </summary>
    public int MaxRetries { get; set; } = 3;

    /// <summary>
    /// Retry delay strategy for failed requests.
    /// Default: ExponentialBackoff.
    /// </summary>
    public RetryStrategy RetryStrategy { get; set; } = RetryStrategy.ExponentialBackoff;

    /// <summary>
    /// Rate limiting configuration.
    /// </summary>
    public RateLimitOptions RateLimits { get; set; } = new();

    /// <summary>
    /// Enable detailed logging of HTTP requests and responses.
    /// Useful for debugging but may impact performance.
    /// Default: false.
    /// </summary>
    public bool EnableLogging { get; set; } = false;

    /// <summary>
    /// Custom HttpClient factory for advanced scenarios.
    /// When provided, this factory will be used instead of the default HttpClient creation.
    /// </summary>
    public Func<HttpClient>? HttpClientFactory { get; set; }

    /// <summary>
    /// User agent string sent with requests.
    /// Default: NetZerion/{version}
    /// </summary>
    public string UserAgent { get; set; } = "NetZerion/1.0.0-preview.1";

    /// <summary>
    /// Validates the configuration options.
    /// </summary>
    /// <exception cref="ArgumentException">Thrown when configuration is invalid.</exception>
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(ApiKey))
        {
            throw new ArgumentException("API key is required. Please provide a valid Zerion API key.", nameof(ApiKey));
        }

        if (string.IsNullOrWhiteSpace(BaseUrl))
        {
            throw new ArgumentException("Base URL cannot be empty.", nameof(BaseUrl));
        }

        if (!Uri.TryCreate(BaseUrl, UriKind.Absolute, out _))
        {
            throw new ArgumentException("Base URL must be a valid absolute URI.", nameof(BaseUrl));
        }

        if (Timeout <= TimeSpan.Zero)
        {
            throw new ArgumentException("Timeout must be greater than zero.", nameof(Timeout));
        }

        if (MaxRetries < 0)
        {
            throw new ArgumentException("MaxRetries cannot be negative.", nameof(MaxRetries));
        }

        if (RateLimits.RequestsPerDay <= 0)
        {
            throw new ArgumentException("RequestsPerDay must be greater than zero.", nameof(RateLimits));
        }

        if (RateLimits.RequestsPerMinute <= 0)
        {
            throw new ArgumentException("RequestsPerMinute must be greater than zero.", nameof(RateLimits));
        }
    }
}
