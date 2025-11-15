namespace NetZerion.Exceptions;

/// <summary>
/// Exception thrown when the Zerion API returns a validation error (400 status code).
/// This typically indicates invalid request parameters.
/// </summary>
public class ValidationException : ApiException
{
    /// <summary>
    /// Dictionary of validation errors, where keys are field names and values are error messages.
    /// </summary>
    public Dictionary<string, string[]> Errors { get; set; } = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="ValidationException"/> class.
    /// </summary>
    /// <param name="message">Error message.</param>
    public ValidationException(string message)
        : base(400, message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ValidationException"/> class with field-specific errors.
    /// </summary>
    /// <param name="message">General error message.</param>
    /// <param name="errors">Dictionary of field-specific validation errors.</param>
    public ValidationException(string message, Dictionary<string, string[]> errors)
        : base(400, message)
    {
        Errors = errors ?? new Dictionary<string, string[]>();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ValidationException"/> class for a single field error.
    /// </summary>
    /// <param name="fieldName">Name of the invalid field.</param>
    /// <param name="errorMessage">Error message for the field.</param>
    public ValidationException(string fieldName, string errorMessage)
        : base(400, $"Validation failed for field '{fieldName}': {errorMessage}")
    {
        Errors[fieldName] = new[] { errorMessage };
    }
}
