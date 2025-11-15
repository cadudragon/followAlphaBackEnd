using System.Net;
using System.Text.Json;
using FluentValidation;
using NetZerion.Exceptions;

namespace TrackFi.Api.Middleware;

/// <summary>
/// Exception handling middleware that implements RFC 7807 Problem Details standard.
/// </summary>
public class ExceptionHandlingMiddleware(
    RequestDelegate next,
    ILogger<ExceptionHandlingMiddleware> logger,
    IHostEnvironment environment)
{
    private readonly RequestDelegate _next = next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger = logger;
    private readonly IHostEnvironment _environment = environment;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        // Generate unique error tracking ID
        var errorId = Guid.NewGuid().ToString();
        var traceId = context.TraceIdentifier;

        // Log the error with tracking information
        _logger.LogError(
            exception,
            "Unhandled exception occurred. ErrorId: {ErrorId}, TraceId: {TraceId}, Path: {Path}",
            errorId,
            traceId,
            context.Request.Path);

        context.Response.ContentType = "application/problem+json";

        var problemDetails = CreateProblemDetails(context, exception, errorId, traceId);

        context.Response.StatusCode = problemDetails.Status ?? (int)HttpStatusCode.InternalServerError;

        await context.Response.WriteAsync(JsonSerializer.Serialize(problemDetails, JsonOptions));
    }

    private ProblemDetailsResponse CreateProblemDetails(
        HttpContext context,
        Exception exception,
        string errorId,
        string traceId)
    {
        var isDevelopment = _environment.IsDevelopment();

        return exception switch
        {
            FluentValidation.ValidationException validationEx => CreateValidationProblem(context, validationEx, errorId, traceId),
            InvalidOperationException invalidOpEx => CreateBadRequestProblem(context, invalidOpEx, errorId, traceId, isDevelopment, "The request could not be processed."),
            ArgumentException argEx => CreateBadRequestProblem(context, argEx, errorId, traceId, isDevelopment, "Invalid request parameters."),
            KeyNotFoundException => CreateNotFoundProblem(context, exception, errorId, traceId, isDevelopment),
            UnauthorizedAccessException => CreateForbiddenProblem(context, errorId, traceId),
            AuthenticationException authEx => CreateAuthenticationProblem(context, authEx, errorId, traceId, isDevelopment),
            RateLimitException rateLimitEx => CreateRateLimitProblem(context, rateLimitEx, errorId, traceId, isDevelopment),
            ApiException apiEx => CreateApiExceptionProblem(context, apiEx, errorId, traceId, isDevelopment),
            NetZerionException netZerionEx => CreateNetZerionProblem(context, netZerionEx, errorId, traceId, isDevelopment),
            _ => CreateGenericProblem(context, exception, errorId, traceId, isDevelopment)
        };
    }

    private static ProblemDetailsResponse CreateValidationProblem(
        HttpContext context,
        FluentValidation.ValidationException validationException,
        string errorId,
        string traceId)
    {
        return new ProblemDetailsResponse
        {
            Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
            Title = "One or more validation errors occurred.",
            Status = (int)HttpStatusCode.BadRequest,
            Detail = "Please check the errors property for details.",
            Instance = context.Request.Path,
            TraceId = traceId,
            ErrorId = errorId,
            Errors = validationException.Errors.GroupBy(e => e.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray())
        };
    }

    private static ProblemDetailsResponse CreateBadRequestProblem(
        HttpContext context,
        Exception exception,
        string errorId,
        string traceId,
        bool isDevelopment,
        string fallbackMessage)
    {
        return new ProblemDetailsResponse
        {
            Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
            Title = "Bad Request",
            Status = (int)HttpStatusCode.BadRequest,
            Detail = isDevelopment ? exception.ToString() : fallbackMessage,
            Instance = context.Request.Path,
            TraceId = traceId,
            ErrorId = errorId
        };
    }

    private static ProblemDetailsResponse CreateNotFoundProblem(
        HttpContext context,
        Exception exception,
        string errorId,
        string traceId,
        bool isDevelopment)
    {
        return new ProblemDetailsResponse
        {
            Type = "https://tools.ietf.org/html/rfc7231#section-6.5.4",
            Title = "Not Found",
            Status = (int)HttpStatusCode.NotFound,
            Detail = isDevelopment ? exception.ToString() : "The requested resource was not found.",
            Instance = context.Request.Path,
            TraceId = traceId,
            ErrorId = errorId
        };
    }

    private static ProblemDetailsResponse CreateForbiddenProblem(
        HttpContext context,
        string errorId,
        string traceId)
    {
        return new ProblemDetailsResponse
        {
            Type = "https://tools.ietf.org/html/rfc7231#section-6.5.3",
            Title = "Forbidden",
            Status = (int)HttpStatusCode.Forbidden,
            Detail = "You do not have permission to access this resource.",
            Instance = context.Request.Path,
            TraceId = traceId,
            ErrorId = errorId
        };
    }

    private static ProblemDetailsResponse CreateAuthenticationProblem(
        HttpContext context,
        AuthenticationException authEx,
        string errorId,
        string traceId,
        bool isDevelopment)
    {
        return new ProblemDetailsResponse
        {
            Type = "https://tools.ietf.org/html/rfc7235#section-3.1",
            Title = "Unauthorized",
            Status = (int)HttpStatusCode.Unauthorized,
            Detail = isDevelopment
                ? $"Zerion API authentication failed: {authEx.Message}\n\nPlease check your API key in appsettings or user secrets."
                : "External API authentication failed.",
            Instance = context.Request.Path,
            TraceId = traceId,
            ErrorId = errorId
        };
    }

    private static ProblemDetailsResponse CreateRateLimitProblem(
        HttpContext context,
        RateLimitException rateLimitEx,
        string errorId,
        string traceId,
        bool isDevelopment)
    {
        return new ProblemDetailsResponse
        {
            Type = "https://tools.ietf.org/html/rfc6585#section-4",
            Title = "Too Many Requests",
            Status = (int)HttpStatusCode.TooManyRequests,
            Detail = isDevelopment
                ? $"Zerion API rate limit exceeded. Retry after {rateLimitEx.RetryAfterSeconds} seconds. Remaining requests: {rateLimitEx.RequestsRemaining}"
                : "Rate limit exceeded. Please try again later.",
            Instance = context.Request.Path,
            TraceId = traceId,
            ErrorId = errorId
        };
    }

    private static ProblemDetailsResponse CreateApiExceptionProblem(
        HttpContext context,
        ApiException apiEx,
        string errorId,
        string traceId,
        bool isDevelopment)
    {
        var (type, title) = MapApiExceptionStatus(apiEx.StatusCode);

        return new ProblemDetailsResponse
        {
            Type = type,
            Title = title,
            Status = apiEx.StatusCode,
            Detail = isDevelopment
                ? $"Zerion API error ({apiEx.StatusCode}): {apiEx.Message}\n\nResponse: {apiEx.Response}"
                : "An error occurred while communicating with external services.",
            Instance = context.Request.Path,
            TraceId = traceId,
            ErrorId = errorId
        };
    }

    private static (string Type, string Title) MapApiExceptionStatus(int statusCode)
    {
        return statusCode switch
        {
            400 => ("https://tools.ietf.org/html/rfc7231#section-6.5.1", "Bad Request"),
            404 => ("https://tools.ietf.org/html/rfc7231#section-6.5.4", "Not Found"),
            500 => ("https://tools.ietf.org/html/rfc7231#section-6.6.1", "External API Error"),
            503 => ("https://tools.ietf.org/html/rfc7231#section-6.6.3", "Service Unavailable"),
            _ => ("https://tools.ietf.org/html/rfc7231#section-6.6.1", "API Error")
        };
    }

    private static ProblemDetailsResponse CreateNetZerionProblem(
        HttpContext context,
        NetZerionException netZerionEx,
        string errorId,
        string traceId,
        bool isDevelopment)
    {
        return new ProblemDetailsResponse
        {
            Type = "https://tools.ietf.org/html/rfc7231#section-6.6.1",
            Title = "External API Error",
            Status = (int)HttpStatusCode.InternalServerError,
            Detail = isDevelopment
                ? $"NetZerion error: {netZerionEx.Message}\n\nStack Trace:\n{netZerionEx.StackTrace}"
                : "An error occurred while communicating with external services.",
            Instance = context.Request.Path,
            TraceId = traceId,
            ErrorId = errorId
        };
    }

    private static ProblemDetailsResponse CreateGenericProblem(
        HttpContext context,
        Exception exception,
        string errorId,
        string traceId,
        bool isDevelopment)
    {
        return new ProblemDetailsResponse
        {
            Type = "https://tools.ietf.org/html/rfc7231#section-6.6.1",
            Title = "Internal Server Error",
            Status = (int)HttpStatusCode.InternalServerError,
            Detail = isDevelopment
                ? $"{exception.Message}\n\nStack Trace:\n{exception.StackTrace}"
                : "An unexpected error occurred. Please contact support with the error ID.",
            Instance = context.Request.Path,
            TraceId = traceId,
            ErrorId = errorId
        };
    }
}

/// <summary>
/// RFC 7807 Problem Details response model.
/// </summary>
public class ProblemDetailsResponse
{
    /// <summary>
    /// A URI reference that identifies the problem type.
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// A short, human-readable summary of the problem type.
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// The HTTP status code.
    /// </summary>
    public int? Status { get; set; }

    /// <summary>
    /// A human-readable explanation specific to this occurrence of the problem.
    /// </summary>
    public string? Detail { get; set; }

    /// <summary>
    /// A URI reference that identifies the specific occurrence of the problem.
    /// </summary>
    public string? Instance { get; set; }

    /// <summary>
    /// The trace identifier for correlation with logs.
    /// </summary>
    public string? TraceId { get; set; }

    /// <summary>
    /// Unique error ID for support and tracking purposes.
    /// </summary>
    public string? ErrorId { get; set; }

    /// <summary>
    /// Validation errors (for validation failures).
    /// </summary>
    public Dictionary<string, string[]>? Errors { get; set; }
}
