using System.Diagnostics;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using TrackFi.Infrastructure.Common.Models;

namespace TrackFi.Infrastructure.Common.Handlers;

/// <summary>
/// HTTP message handler that logs all HTTP requests and responses.
/// Provides detailed error information when API calls fail, including response bodies.
/// Automatically attached to all HttpClient instances for comprehensive observability.
/// </summary>
public class HttpLoggingHandler : DelegatingHandler
{
    private readonly ILogger<HttpLoggingHandler> _logger;
    private const int MaxBodyLogLength = 2000; // Prevent logging huge responses

    public HttpLoggingHandler(ILogger<HttpLoggingHandler> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var requestId = Guid.NewGuid().ToString("N")[..8]; // Short ID for correlation
        var stopwatch = Stopwatch.StartNew();

        // Log request at Debug level (can be disabled in production)
        LogRequest(request, requestId);

        HttpResponseMessage? response = null;
        try
        {
            response = await base.SendAsync(request, cancellationToken);
            stopwatch.Stop();

            // Log response - detailed logging only for errors
            if (!response.IsSuccessStatusCode)
            {
                await LogErrorResponseAsync(request, response, stopwatch.Elapsed, requestId, cancellationToken);
            }
            else
            {
                LogSuccessResponse(request, response, stopwatch.Elapsed, requestId);
            }

            return response;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            LogRequestException(request, ex, stopwatch.Elapsed, requestId);
            throw;
        }
    }

    /// <summary>
    /// Logs HTTP request details at Debug level.
    /// </summary>
    private void LogRequest(HttpRequestMessage request, string requestId)
    {
        if (!_logger.IsEnabled(LogLevel.Debug))
            return;

        _logger.LogDebug(
            "[{RequestId}] HTTP Request: {Method} {Url}",
            requestId,
            request.Method,
            request.RequestUri?.ToString() ?? "unknown");
    }

    /// <summary>
    /// Logs successful HTTP responses at Debug level.
    /// </summary>
    private void LogSuccessResponse(
        HttpRequestMessage request,
        HttpResponseMessage response,
        TimeSpan duration,
        string requestId)
    {
        if (!_logger.IsEnabled(LogLevel.Debug))
            return;

        _logger.LogDebug(
            "[{RequestId}] HTTP {Method} {Url} succeeded with {StatusCode} in {Duration}ms",
            requestId,
            request.Method,
            request.RequestUri?.ToString() ?? "unknown",
            (int)response.StatusCode,
            duration.TotalMilliseconds);
    }

    /// <summary>
    /// Logs detailed error information when HTTP requests fail.
    /// Includes status code, error message from response body, and request details.
    /// </summary>
    private async Task LogErrorResponseAsync(
        HttpRequestMessage request,
        HttpResponseMessage response,
        TimeSpan duration,
        string requestId,
        CancellationToken cancellationToken)
    {
        var url = request.RequestUri?.ToString() ?? "unknown";
        var method = request.Method.ToString();
        var statusCode = (int)response.StatusCode;
        var statusDescription = response.ReasonPhrase ?? response.StatusCode.ToString();

        // Read and parse error response body
        string? responseBody = null;
        string? errorMessage = null;

        try
        {
            responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

            // Try to parse as JSON and extract error message
            errorMessage = ExtractErrorMessage(responseBody);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[{RequestId}] Failed to read error response body", requestId);
        }

        // Read request body if available (for POST/PUT requests)
        string? requestBody = null;
        if (request.Content != null && _logger.IsEnabled(LogLevel.Debug))
        {
            try
            {
                // Note: This reads the stream, so only works if content is buffered
                requestBody = await request.Content.ReadAsStringAsync(cancellationToken);
                if (requestBody.Length > MaxBodyLogLength)
                {
                    requestBody = requestBody[..MaxBodyLogLength] + "... (truncated)";
                }
            }
            catch
            {
                requestBody = "(unable to read request body)";
            }
        }

        // Log comprehensive error information
        var logMessage = new StringBuilder();
        logMessage.AppendLine($"[{requestId}] HTTP Request Failed");
        logMessage.AppendLine($"  Method: {method}");
        logMessage.AppendLine($"  URL: {url}");
        logMessage.AppendLine($"  Status: {statusCode} {statusDescription}");
        logMessage.AppendLine($"  Duration: {duration.TotalMilliseconds:F1}ms");

        if (!string.IsNullOrEmpty(errorMessage))
        {
            logMessage.AppendLine($"  Error Message: {errorMessage}");
        }

        if (!string.IsNullOrEmpty(responseBody))
        {
            var truncatedBody = responseBody.Length > MaxBodyLogLength
                ? responseBody[..MaxBodyLogLength] + "... (truncated)"
                : responseBody;
            logMessage.AppendLine($"  Response Body: {truncatedBody}");
        }

        if (!string.IsNullOrEmpty(requestBody))
        {
            logMessage.AppendLine($"  Request Body: {requestBody}");
        }

        _logger.LogError(logMessage.ToString());
    }

    /// <summary>
    /// Logs exceptions that occur during HTTP requests (network failures, timeouts, etc.).
    /// </summary>
    private void LogRequestException(
        HttpRequestMessage request,
        Exception exception,
        TimeSpan duration,
        string requestId)
    {
        _logger.LogError(
            exception,
            "[{RequestId}] HTTP {Method} {Url} failed with exception after {Duration}ms",
            requestId,
            request.Method,
            request.RequestUri?.ToString() ?? "unknown",
            duration.TotalMilliseconds);
    }

    /// <summary>
    /// Extracts error message from JSON response body.
    /// Supports multiple common error formats from different API providers.
    /// </summary>
    private static string? ExtractErrorMessage(string responseBody)
    {
        if (string.IsNullOrWhiteSpace(responseBody))
            return null;

        try
        {
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            var errorResponse = JsonSerializer.Deserialize<ApiErrorResponse>(responseBody, options);

            // Try different error message locations (different APIs use different formats)
            if (errorResponse != null)
            {
                // Nested error object (Alchemy format: { "error": { "message": "..." } })
                if (!string.IsNullOrEmpty(errorResponse.ErrorObject?.Message))
                    return errorResponse.ErrorObject.Message;

                // Direct message field
                if (!string.IsNullOrEmpty(errorResponse.Message))
                    return errorResponse.Message;

                // Direct error field
                if (!string.IsNullOrEmpty(errorResponse.Error))
                    return errorResponse.Error;

                // Detail field
                if (!string.IsNullOrEmpty(errorResponse.Detail))
                    return errorResponse.Detail;

                // Validation errors array
                if (errorResponse.Errors?.Count > 0)
                {
                    var firstError = errorResponse.Errors[0];
                    return $"{firstError.Field}: {firstError.Message}";
                }
            }
        }
        catch
        {
            // If JSON parsing fails, return null and log the raw body instead
        }

        return null;
    }
}
