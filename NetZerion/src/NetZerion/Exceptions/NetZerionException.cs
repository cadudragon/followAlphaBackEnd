namespace NetZerion.Exceptions;

/// <summary>
/// Base exception for all NetZerion errors.
/// </summary>
public class NetZerionException : Exception
{
    /// <summary>
    /// Optional error code for categorizing the error.
    /// </summary>
    public string? ErrorCode { get; set; }

    /// <summary>
    /// Optional trace ID for correlating the error with logs.
    /// </summary>
    public string? TraceId { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="NetZerionException"/> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    public NetZerionException(string message) : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="NetZerionException"/> class with an inner exception.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The inner exception.</param>
    public NetZerionException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
