using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;
using TrackFi.Api;
using TrackFi.Api.Endpoints;
using TrackFi.Api.Middleware;
using TrackFi.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices(builder.Configuration);
builder.Services.AddApiServices(builder.Configuration);

// Add OpenAPI
builder.Services.AddOpenApi();

// Register DbInitializer for seeding (only used in Development)
builder.Services.AddScoped<DbInitializer>();

var app = builder.Build();

// Apply pending migrations on startup
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();

    try
    {
        var context = services.GetRequiredService<TrackFiDbContext>();
        logger.LogInformation("Applying pending database migrations...");
        await context.Database.MigrateAsync();
        logger.LogInformation("Database migrations applied successfully");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "An error occurred while applying database migrations");
        throw; // Fail startup if migrations fail
    }
}

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();

    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/openapi/v1.json", "TrackFi API v1");
    });

    app.UseReDoc(options =>
    {
        options.SpecUrl = "/openapi/v1.json";
        options.DocumentTitle = "TrackFi API Documentation";
    });

    app.MapScalarApiReference(options =>
    {
        options.Title = "TrackFi API";
    });
}

// Add exception handling middleware
app.UseMiddleware<ExceptionHandlingMiddleware>();

app.UseHttpsRedirection();

// Enable static files (for network logos, etc.)
app.UseStaticFiles();

// Enable CORS
app.UseCors();

// Map health checks
app.MapHealthChecks("/health");

// Map API endpoints
app.MapPortfolioPreviewEndpoints(); // Anonymous portfolio (no auth required)
app.MapDeFiEndpoints(); // DeFi positions (Zerion API)
app.MapUserEndpoints();
app.MapUserWalletEndpoints();
app.MapWatchlistEndpoints();

// Seed database in Development environment only
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var services = scope.ServiceProvider;

    try
    {
        var dbInitializer = services.GetRequiredService<DbInitializer>();
        var logger = services.GetRequiredService<ILogger<Program>>();

        logger.LogInformation("Development environment detected. Checking if database seeding is needed...");
        await dbInitializer.SeedAsync();
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while seeding the database");
    }
}

app.Run();
