using System.Reflection;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using NetZerion.Extensions;
using TrackFi.Application.Common.Behaviors;
using TrackFi.Application.Interfaces;
using TrackFi.Domain.Interfaces;
using TrackFi.Infrastructure.Blockchain;
using TrackFi.Infrastructure.Caching;
using TrackFi.Infrastructure.Common.Handlers;
using TrackFi.Infrastructure.DeFi;
using TrackFi.Infrastructure.Portfolio;
using TrackFi.Infrastructure.Portfolio.Providers;
using TrackFi.Infrastructure.Providers;
using TrackFi.Infrastructure.Persistence;
using TrackFi.Infrastructure.Persistence.Repositories;
using TrackFi.Infrastructure.Web3;

namespace TrackFi.Api;

public static class DependencyInjection
{
    public static IServiceCollection AddApiServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Add CORS
        var allowedOrigins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
            ?? ["http://localhost:3000"];

        services.AddCors(options =>
        {
            options.AddDefaultPolicy(policy =>
            {
                policy.WithOrigins(allowedOrigins)
                      .AllowAnyMethod()
                      .AllowAnyHeader()
                      .AllowCredentials();
            });
        });

        // Add health checks
        services.AddHealthChecks();

        return services;
    }

    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        // Add MediatR
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(Application.AssemblyReference).Assembly);
            cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
        });

        // Add AutoMapper
        services.AddAutoMapper(cfg =>
        {
            cfg.AddMaps(typeof(Application.AssemblyReference).Assembly);
        });

        // Add FluentValidation
        services.AddValidatorsFromAssembly(typeof(Application.AssemblyReference).Assembly);

        return services;
    }

    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Add DbContext
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? "Host=localhost;Database=trackfi;Username=postgres;Password=postgres";

        services.AddDbContext<TrackFiDbContext>(options =>
            options.UseNpgsql(connectionString));

        // Add Repositories
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IUserWalletRepository, UserWalletRepository>();
        services.AddScoped<IWatchlistRepository, WatchlistRepository>();
        // REMOVED: Token verification repositories (not needed with Zerion)
        // services.AddScoped<IVerifiedTokenRepository, VerifiedTokenRepository>();
        // services.AddScoped<UnlistedTokenRepository>();
        // services.AddScoped<TokenMetadataRepository>();
        services.AddSingleton<INetworkMetadataRepository, NetworkMetadataRepository>(); // Singleton for in-memory cache

        // Add Web3 Services
        services.AddScoped<ISignatureValidator, SiweSignatureValidator>();
        services.AddScoped<SolanaSignatureValidator>();

        // Add External Services
        services.AddScoped<IAssetDataProvider, PlaceholderAssetDataProvider>();
        services.AddScoped<IPriceProvider, PlaceholderPriceProvider>();

        // Add Caching
        services.Configure<CacheOptions>(configuration.GetSection("Cache"));
        var redisConnectionString = configuration.GetConnectionString("Redis") ?? "localhost:6379";
        services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = redisConnectionString;
        });
        services.AddScoped<DistributedCacheService>();

        // Add HTTP Logging Handler (logs all HTTP requests/responses with detailed error information)
        services.AddTransient<HttpLoggingHandler>();

        // Add Blockchain Services (HTTP clients)
        services.Configure<AlchemyOptions>(configuration.GetSection("Alchemy"));

        // Validate Alchemy batch limits on startup
        var alchemyOptions = configuration.GetSection("Alchemy").Get<AlchemyOptions>();
        alchemyOptions?.BatchLimits.Validate();

        services.AddHttpClient<AlchemyService>()
            .AddHttpMessageHandler<HttpLoggingHandler>();

        // Add CoinMarketCap services (kept for future use)
        services.Configure<CoinMarketCapOptions>(configuration.GetSection(CoinMarketCapOptions.SectionName));
        services.AddHttpClient<CoinMarketCapService>()
            .AddHttpMessageHandler<HttpLoggingHandler>();
        // REMOVED: TokenVerificationService (not needed with Zerion only_non_trash filter)
        // services.AddScoped<TokenVerificationService>();

        // Configure DeFi provider options
        services.Configure<DeFiProviderOptions>(configuration.GetSection("DeFi"));

        // Validate DeFi rate limits on startup
        var defiOptions = configuration.GetSection("DeFi").Get<DeFiProviderOptions>();
        defiOptions?.Zerion.RateLimits.Validate();
        defiOptions?.Moralis.RateLimits.Validate();

        // Add NetZerion (Zerion API wrapper)
        services.AddNetZerion(options =>
        {
            options.ApiKey = configuration["Zerion:ApiKey"] ?? configuration["DeFi:Zerion:ApiKey"];
            options.Timeout = TimeSpan.FromSeconds(30);
            options.MaxRetries = 3;
            options.RateLimits = new NetZerion.Configuration.RateLimitOptions
            {
                RequestsPerDay = 3000,
                RequestsPerMinute = 100
            };
        });

        // Add Portfolio Provider (Zerion implementation)
        // ZerionPortfolioProvider handles all aggregation, categorization, and transformation
        services.AddScoped<IPortfolioProvider, ZerionPortfolioProvider>();

        // Add Unified Portfolio Service (thin caching + orchestration layer)
        services.AddScoped<PortfolioService>();

        return services;
    }
}
