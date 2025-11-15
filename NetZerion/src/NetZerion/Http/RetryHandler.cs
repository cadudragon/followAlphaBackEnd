using NetZerion.Configuration;
using System.Net;

namespace NetZerion.Http;

/// <summary>
/// HTTP message handler that implements retry logic for transient failures.
/// </summary>
public class RetryHandler : DelegatingHandler
{
    private readonly int _maxRetries;
    private readonly RetryStrategy _strategy;

    /// <summary>
    /// Initializes a new instance of the <see cref="RetryHandler"/> class.
    /// </summary>
    /// <param name="maxRetries">Maximum number of retry attempts.</param>
    /// <param name="strategy">Retry delay strategy.</param>
    public RetryHandler(int maxRetries, RetryStrategy strategy)
    {
        _maxRetries = maxRetries;
        _strategy = strategy;
    }

    /// <summary>
    /// Sends an HTTP request with retry logic.
    /// </summary>
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        HttpResponseMessage? response = null;
        Exception? lastException = null;

        for (int attempt = 0; attempt <= _maxRetries; attempt++)
        {
            try
            {
                response = await base.SendAsync(request, cancellationToken);

                // Don't retry on success or client errors (4xx except 429)
                if (response.IsSuccessStatusCode ||
                    (response.StatusCode >= HttpStatusCode.BadRequest &&
                     response.StatusCode < HttpStatusCode.InternalServerError &&
                     response.StatusCode != HttpStatusCode.TooManyRequests))
                {
                    return response;
                }

                // Retry on server errors (5xx) or 429 (rate limit)
                if (attempt < _maxRetries)
                {
                    var delay = CalculateDelay(attempt);
                    await Task.Delay(delay, cancellationToken);
                }
            }
            catch (HttpRequestException ex)
            {
                lastException = ex;

                if (attempt < _maxRetries)
                {
                    var delay = CalculateDelay(attempt);
                    await Task.Delay(delay, cancellationToken);
                }
            }
            catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
            {
                // Timeout - retry
                lastException = ex;

                if (attempt < _maxRetries)
                {
                    var delay = CalculateDelay(attempt);
                    await Task.Delay(delay, cancellationToken);
                }
            }
        }

        // If we have a response (even a failed one), return it
        if (response != null)
            return response;

        // Otherwise throw the last exception
        throw lastException ?? new HttpRequestException("Request failed after retries");
    }

    private TimeSpan CalculateDelay(int attempt)
    {
        return _strategy switch
        {
            RetryStrategy.Linear => TimeSpan.FromSeconds(2),
            RetryStrategy.ExponentialBackoff => TimeSpan.FromSeconds(Math.Pow(2, attempt)),
            _ => TimeSpan.Zero
        };
    }
}
