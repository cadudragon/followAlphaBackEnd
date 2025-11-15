namespace NetZerion.Exceptions;

/// <summary>
/// Exception thrown when the Zerion API returns an error response (4xx or 5xx status codes).
/// </summary>
public class ApiException : NetZerionException
{
    /// <summary>
    /// HTTP status code from the API response.
    /// </summary>
    public int StatusCode { get; set; }

    /// <summary>
    /// Raw response body from the API, if available.
    /// </summary>
    public string? Response { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ApiException"/> class.
    /// </summary>
    /// <param name="statusCode">HTTP status code.</param>
    /// <param name="message">Error message.</param>
    /// <param name="response">Optional raw response body.</param>
    public ApiException(int statusCode, string message, string? response = null)
        : base(message)
    {
        StatusCode = statusCode;
        Response = response;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ApiException"/> class with an inner exception.
    /// </summary>
    /// <param name="statusCode">HTTP status code.</param>
    /// <param name="message">Error message.</param>
    /// <param name="innerException">Inner exception.</param>
    public ApiException(int statusCode, string message, Exception innerException)
        : base(message, innerException)
    {
        StatusCode = statusCode;
    }
}
