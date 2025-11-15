using System.Text.Json;
using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using TrackFi.Api.Middleware;

namespace TrackFi.Tests.Api.Middleware;

public class ExceptionHandlingMiddlewareTests
{
    [Fact]
    public async Task InvokeAsync_WithValidationException_ReturnsBadRequestWithErrors()
    {
        var failures = new[]
        {
            new ValidationFailure("address", "Address is required"),
            new ValidationFailure("network", "Network is invalid")
        };

        var validationException = new ValidationException(failures);

        var (context, problem) = await InvokeMiddlewareAsync(validationException, environment: Environments.Development);

        context.Response.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
        context.Response.ContentType.Should().Be("application/problem+json");
        problem.Title.Should().Be("One or more validation errors occurred.");
        problem.Type.Should().Be("https://tools.ietf.org/html/rfc7231#section-6.5.1");
        problem.TraceId.Should().Be(context.TraceIdentifier);
        problem.ErrorId.Should().NotBeNullOrEmpty();
        problem.Errors.Should().NotBeNull();
        problem.Errors.Should().ContainKey("address");
        problem.Errors.Should().ContainKey("network");
    }

    [Fact]
    public async Task InvokeAsync_WithArgumentException_InProduction_HidesSensitiveDetails()
    {
        var exception = new ArgumentException("Sensitive detail", "address");

        var (_, problem) = await InvokeMiddlewareAsync(exception, environment: Environments.Production);

        problem.Status.Should().Be(StatusCodes.Status400BadRequest);
        problem.Detail.Should().Be("Invalid request parameters.");
    }

    [Fact]
    public async Task InvokeAsync_WithUnhandledException_InDevelopment_ReturnsStackTrace()
    {
        var exception = new Exception("Boom!");

        var (_, problem) = await InvokeMiddlewareAsync(exception, environment: Environments.Development);

        problem.Status.Should().Be(StatusCodes.Status500InternalServerError);
        problem.Detail.Should().Contain("Stack Trace:");
    }

    private static async Task<(HttpContext context, ProblemDetailsResponse problem)> InvokeMiddlewareAsync(
        Exception exception,
        string environment)
    {
        var context = new DefaultHttpContext
        {
            Request =
            {
                Path = "/api/test"
            },
            TraceIdentifier = Guid.NewGuid().ToString()
        };
        context.Response.Body = new MemoryStream();

        var middleware = new ExceptionHandlingMiddleware(
            _ => throw exception,
            NullLogger<ExceptionHandlingMiddleware>.Instance,
            new TestHostEnvironment(environment));

        await middleware.InvokeAsync(context);

        context.Response.Body.Seek(0, SeekOrigin.Begin);
        var json = await new StreamReader(context.Response.Body).ReadToEndAsync();
        var problem = JsonSerializer.Deserialize<ProblemDetailsResponse>(
            json,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
            ?? throw new InvalidOperationException("Failed to deserialize problem details.");

        return (context, problem);
    }

    private sealed class TestHostEnvironment(string environmentName) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = environmentName;
        public string ApplicationName { get; set; } = nameof(ExceptionHandlingMiddlewareTests);
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
