using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NetZerion.Clients;
using NetZerion.Configuration;
using NetZerion.Http;

namespace NetZerion.Extensions;

/// <summary>
/// Extension methods for configuring NetZerion services in dependency injection.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds NetZerion services to the service collection with custom configuration.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configureOptions">Action to configure NetZerion options.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddNetZerion(
        this IServiceCollection services,
        Action<NetZerionOptions> configureOptions)
    {
        if (services == null)
            throw new ArgumentNullException(nameof(services));

        if (configureOptions == null)
            throw new ArgumentNullException(nameof(configureOptions));

        // Register options
        services.Configure(configureOptions);

        // Register services
        RegisterNetZerionServices(services);

        return services;
    }

    /// <summary>
    /// Adds NetZerion services to the service collection using configuration section.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration instance.</param>
    /// <param name="sectionName">Configuration section name (default: "NetZerion").</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddNetZerion(
        this IServiceCollection services,
        IConfiguration configuration,
        string sectionName = "NetZerion")
    {
        if (services == null)
            throw new ArgumentNullException(nameof(services));

        if (configuration == null)
            throw new ArgumentNullException(nameof(configuration));

        // Bind configuration section
        services.Configure<NetZerionOptions>(configuration.GetSection(sectionName));

        // Register services
        RegisterNetZerionServices(services);

        return services;
    }

    /// <summary>
    /// Registers all NetZerion services.
    /// </summary>
    private static void RegisterNetZerionServices(IServiceCollection services)
    {
        // Register HttpClient with configuration
        services.AddHttpClient<NetZerionClient>((serviceProvider, client) =>
        {
            var options = serviceProvider.GetRequiredService<IOptions<NetZerionOptions>>().Value;
            options.Validate();

            client.BaseAddress = new Uri(options.BaseUrl);
            client.Timeout = options.Timeout;
            client.DefaultRequestHeaders.Add("User-Agent", options.UserAgent);
        })
        .ConfigurePrimaryHttpMessageHandler((serviceProvider) =>
        {
            var options = serviceProvider.GetRequiredService<IOptions<NetZerionOptions>>().Value;

            // Create handler chain: HttpClientHandler -> RetryHandler -> AuthenticationHandler
            var authHandler = new AuthenticationHandler(options.ApiKey!)
            {
                InnerHandler = new RetryHandler(options.MaxRetries, options.RetryStrategy)
                {
                    InnerHandler = new HttpClientHandler()
                }
            };

            return authHandler;
        });

        // Register ZerionHttpClient
        services.AddScoped<ZerionHttpClient>(serviceProvider =>
        {
            var httpClientFactory = serviceProvider.GetRequiredService<IHttpClientFactory>();
            var httpClient = httpClientFactory.CreateClient(nameof(NetZerionClient));
            var options = serviceProvider.GetRequiredService<IOptions<NetZerionOptions>>().Value;

            return new ZerionHttpClient(httpClient, options);
        });

        // Register client interfaces
        services.AddScoped<IWalletClient, WalletClient>();
        services.AddScoped<ITransactionClient, TransactionClient>();

        // Register main client
        services.AddScoped<NetZerionClient>(serviceProvider =>
        {
            var httpClientFactory = serviceProvider.GetRequiredService<IHttpClientFactory>();
            var httpClient = httpClientFactory.CreateClient(nameof(NetZerionClient));
            var options = serviceProvider.GetRequiredService<IOptions<NetZerionOptions>>().Value;

            return new NetZerionClient(httpClient, options);
        });
    }
}
