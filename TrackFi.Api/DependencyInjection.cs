using System.Reflection;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using NetZerion.Extensions;
using TrackFi.Application.Common.Behaviors;
using TrackFi.Domain.Interfaces;
using TrackFi.Infrastructure.Blockchain;
using TrackFi.Infrastructure.Caching;
using TrackFi.Infrastructure.Common.Handlers;
using TrackFi.Infrastructure.DeFi;
using TrackFi.Infrastructure.Portfolio;
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
        services.AddScoped<IVerifiedTokenRepository, VerifiedTokenRepository>();
        services.AddScoped<UnlistedTokenRepository>();
        services.AddScoped<TokenMetadataRepository>();
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

        // Add CoinMarketCap services
        services.Configure<CoinMarketCapOptions>(configuration.GetSection(CoinMarketCapOptions.SectionName));
        services.AddHttpClient<CoinMarketCapService>()
            .AddHttpMessageHandler<HttpLoggingHandler>();
        services.AddScoped<TokenVerificationService>();

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

        // Add DeFi Data Provider Factory (supports Zerion and Moralis)
        services.AddScoped<IDeFiDataProvider>(sp =>
        {
            var providerOptions = configuration.GetSection("DeFi").Get<DeFiProviderOptions>()
                ?? new DeFiProviderOptions();

            var logger = sp.GetRequiredService<ILoggerFactory>();

            return providerOptions.Provider switch
            {
                DeFiProvider.Moralis => CreateMoralisService(sp, providerOptions, logger),
                DeFiProvider.Zerion => CreateZerionService(sp, providerOptions, logger),
                _ => throw new InvalidOperationException($"Unknown DeFi provider: {providerOptions.Provider}")
            };
        });

        // Add DeFi Price Enrichment Service
        services.AddScoped<DeFiPriceEnrichmentService>();

        // Add Portfolio Services
        services.AddScoped<AnonymousPortfolioService>();
        services.AddScoped<DeFiPortfolioService>();

        return services;
    }

    /// <summary>
    /// Creates a Zerion service instance.
    /// </summary>
    private static ZerionService CreateZerionService(
        IServiceProvider sp,
        DeFiProviderOptions options,
        ILoggerFactory loggerFactory)
    {
        var walletClient = sp.GetRequiredService<NetZerion.Clients.IWalletClient>();
        var logger = loggerFactory.CreateLogger<ZerionService>();

        return new ZerionService(walletClient, logger);
    }

    /// <summary>
    /// Creates a Moralis service instance.
    /// </summary>
    private static MoralisService CreateMoralisService(
        IServiceProvider sp,
        DeFiProviderOptions options,
        ILoggerFactory loggerFactory)
    {
        var apiKey = options.Moralis.ApiKey
            ?? throw new InvalidOperationException("Moralis API key is required when using Moralis provider");

        var httpClient = new HttpClient();
        var logger = loggerFactory.CreateLogger<MoralisService>();

        return new MoralisService(
            httpClient,
            logger,
            apiKey,
            options.Moralis.BaseUrl);
    }
}
