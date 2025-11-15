namespace NetZerion.Exceptions;

/// <summary>
/// Exception thrown when the Zerion API rate limit has been exceeded (429 status code).
/// </summary>
public class RateLimitException : ApiException
{
    /// <summary>
    /// Number of seconds to wait before retrying, if provided by the API.
    /// </summary>
    public int? RetryAfterSeconds { get; set; }

    /// <summary>
    /// Number of requests remaining in the current rate limit window.
    /// </summary>
    public int RequestsRemaining { get; set; }

    /// <summary>
    /// Unix timestamp when the rate limit resets.
    /// </summary>
    public long? ResetTimestamp { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="RateLimitException"/> class.
    /// </summary>
    /// <param name="retryAfter">Seconds to wait before retrying.</param>
    /// <param name="remaining">Requests remaining.</param>
    public RateLimitException(int retryAfter, int remaining)
        : base(429, "Rate limit exceeded. Please wait before making more requests.")
    {
        RetryAfterSeconds = retryAfter;
        RequestsRemaining = remaining;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="RateLimitException"/> class with custom message.
    /// </summary>
    /// <param name="message">Custom error message.</param>
    /// <param name="retryAfter">Seconds to wait before retrying.</param>
    /// <param name="remaining">Requests remaining.</param>
    public RateLimitException(string message, int retryAfter, int remaining)
        : base(429, message)
    {
        RetryAfterSeconds = retryAfter;
        RequestsRemaining = remaining;
    }
}
