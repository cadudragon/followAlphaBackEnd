using NetZerion.Configuration;
using NetZerion.Exceptions;
using NetZerion.Utilities;
using System.Net;
using System.Text.Json;

namespace NetZerion.Http;

/// <summary>
/// HTTP client wrapper for making requests to the Zerion API.
/// </summary>
public class ZerionHttpClient : IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly NetZerionOptions _options;
    private readonly RateLimiter _rateLimiter;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="ZerionHttpClient"/> class.
    /// </summary>
    /// <param name="httpClient">Configured HTTP client.</param>
    /// <param name="options">Configuration options.</param>
    public ZerionHttpClient(HttpClient httpClient, NetZerionOptions options)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _rateLimiter = new RateLimiter(_options.RateLimits);
    }

    /// <summary>
    /// Sends a GET request to the Zerion API.
    /// </summary>
    /// <typeparam name="T">Expected response type.</typeparam>
    /// <param name="endpoint">API endpoint (e.g., "/wallets/{address}/positions").</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Deserialized response.</returns>
    public async Task<T> GetAsync<T>(string endpoint, CancellationToken cancellationToken = default)
    {
        await _rateLimiter.CheckRateLimitAsync(cancellationToken);

        HttpResponseMessage response;
        try
        {
            response = await _httpClient.GetAsync(endpoint, cancellationToken);
            _rateLimiter.RecordRequest();
        }
        catch (HttpRequestException ex)
        {
            throw new NetZerionException("HTTP request failed. Check your network connection.", ex);
        }
        catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            throw new NetZerionException($"Request timed out after {_options.Timeout.TotalSeconds} seconds.", ex);
        }

        return await HandleResponseAsync<T>(response, cancellationToken);
    }

    /// <summary>
    /// Handles the HTTP response and throws appropriate exceptions.
    /// </summary>
    private async Task<T> HandleResponseAsync<T>(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        var content = await response.Content.ReadAsStringAsync(cancellationToken);

        if (response.IsSuccessStatusCode)
        {
            try
            {
                var result = JsonSerializer.Deserialize<T>(content, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                return result ?? throw new NetZerionException("Received empty response from API");
            }
            catch (JsonException ex)
            {
                throw new NetZerionException("Failed to deserialize API response", ex);
            }
        }

        // Handle error responses
        throw response.StatusCode switch
        {
            HttpStatusCode.Unauthorized => new AuthenticationException("Invalid API key or unauthorized access"),
            HttpStatusCode.TooManyRequests => ParseRateLimitException(response, content),
            HttpStatusCode.BadRequest => new ValidationException("Bad request - check your parameters"),
            HttpStatusCode.NotFound => new ApiException(404, "Resource not found", content),
            HttpStatusCode.InternalServerError => new ApiException(500, "Zerion API internal error", content),
            HttpStatusCode.ServiceUnavailable => new ApiException(503, "Zerion API is temporarily unavailable", content),
            _ => new ApiException((int)response.StatusCode, $"API request failed with status {(int)response.StatusCode}", content)
        };
    }

    private RateLimitException ParseRateLimitException(HttpResponseMessage response, string content)
    {
        // Try to get Retry-After header
        int retryAfter = 60; // Default to 60 seconds
        if (response.Headers.TryGetValues("Retry-After", out var retryValues))
        {
            var retryValue = retryValues.FirstOrDefault();
            if (int.TryParse(retryValue, out var seconds))
            {
                retryAfter = seconds;
            }
        }

        // Try to get remaining requests
        int remaining = 0;
        if (response.Headers.TryGetValues("X-RateLimit-Remaining", out var remainingValues))
        {
            var remainingValue = remainingValues.FirstOrDefault();
            if (int.TryParse(remainingValue, out var count))
            {
                remaining = count;
            }
        }

        return new RateLimitException(retryAfter, remaining);
    }

    /// <summary>
    /// Gets the number of requests remaining for today.
    /// </summary>
    public int GetDailyRequestsRemaining() => _rateLimiter.GetDailyRequestsRemaining();

    /// <summary>
    /// Gets the number of requests made today.
    /// </summary>
    public int GetDailyRequestCount() => _rateLimiter.GetDailyRequestCount();

    /// <summary>
    /// Disposes the HTTP client.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
            return;

        _httpClient?.Dispose();
        _disposed = true;
        GC.SuppressFinalize(this);
    }
}
