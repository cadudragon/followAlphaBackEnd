namespace TrackFi.Infrastructure.Common.Models;

/// <summary>
/// Represents a standard API error response.
/// Supports multiple common error formats from different providers.
/// </summary>
public class ApiErrorResponse
{
    /// <summary>
    /// Error message (most APIs use this field).
    /// </summary>
    public string? Message { get; set; }

    /// <summary>
    /// Error code or type.
    /// </summary>
    public string? Error { get; set; }

    /// <summary>
    /// Detailed error information.
    /// </summary>
    public string? Detail { get; set; }

    /// <summary>
    /// HTTP status code.
    /// </summary>
    public int? Status { get; set; }

    /// <summary>
    /// Array of validation errors.
    /// </summary>
    public List<ValidationError>? Errors { get; set; }

    /// <summary>
    /// Nested error object (Alchemy format).
    /// </summary>
    public NestedError? ErrorObject { get; set; }
}

/// <summary>
/// Nested error format used by some APIs (e.g., Alchemy).
/// Example: { "error": { "message": "..." } }
/// </summary>
public class NestedError
{
    public string? Message { get; set; }
    public string? Code { get; set; }
    public string? Type { get; set; }
}

/// <summary>
/// Validation error format.
/// </summary>
public class ValidationError
{
    public string? Field { get; set; }
    public string? Message { get; set; }
    public string? Code { get; set; }
}
