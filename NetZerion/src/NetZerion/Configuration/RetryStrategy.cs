namespace NetZerion.Configuration;

/// <summary>
/// Defines the retry strategy for failed HTTP requests.
/// </summary>
public enum RetryStrategy
{
    /// <summary>
    /// No retries - fail immediately on error.
    /// </summary>
    None,

    /// <summary>
    /// Linear backoff - wait the same amount of time between each retry.
    /// </summary>
    Linear,

    /// <summary>
    /// Exponential backoff - double the wait time after each retry (recommended).
    /// </summary>
    ExponentialBackoff
}
