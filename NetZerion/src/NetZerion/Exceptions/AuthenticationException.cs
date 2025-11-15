namespace NetZerion.Exceptions;

/// <summary>
/// Exception thrown when authentication with the Zerion API fails (401 status code).
/// This typically indicates an invalid or missing API key.
/// </summary>
public class AuthenticationException : ApiException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AuthenticationException"/> class.
    /// </summary>
    /// <param name="message">Error message describing the authentication failure.</param>
    public AuthenticationException(string message)
        : base(401, message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AuthenticationException"/> class with default message.
    /// </summary>
    public AuthenticationException()
        : base(401, "Authentication failed. Please check your API key.")
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AuthenticationException"/> class with an inner exception.
    /// </summary>
    /// <param name="message">Error message.</param>
    /// <param name="innerException">Inner exception.</param>
    public AuthenticationException(string message, Exception innerException)
        : base(401, message, innerException)
    {
    }
}
