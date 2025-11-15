using NetZerion.Clients;
using NetZerion.Configuration;
using NetZerion.Http;

namespace NetZerion;

/// <summary>
/// Main entry point for accessing the Zerion API.
/// </summary>
public class NetZerionClient : IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly ZerionHttpClient _zerionHttpClient;
    private readonly bool _ownsHttpClient;
    private bool _disposed;

    /// <summary>
    /// Gets the wallet-related API client.
    /// </summary>
    public IWalletClient Wallet { get; }

    /// <summary>
    /// Gets the transaction-related API client.
    /// </summary>
    public ITransactionClient Transactions { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="NetZerionClient"/> class with an API key.
    /// </summary>
    /// <param name="apiKey">Zerion API key.</param>
    /// <param name="options">Optional configuration options.</param>
    public NetZerionClient(string apiKey, NetZerionOptions? options = null)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
            throw new ArgumentException("API key cannot be null or empty.", nameof(apiKey));

        options ??= new NetZerionOptions();
        options.ApiKey = apiKey;
        options.Validate();

        _httpClient = CreateHttpClient(options);
        _zerionHttpClient = new ZerionHttpClient(_httpClient, options);
        _ownsHttpClient = true;

        // Initialize clients
        Wallet = new WalletClient(_zerionHttpClient);
        Transactions = new TransactionClient(_zerionHttpClient);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="NetZerionClient"/> class with a pre-configured HTTP client.
    /// This constructor is intended for dependency injection scenarios.
    /// </summary>
    /// <param name="httpClient">Pre-configured HTTP client with authentication.</param>
    /// <param name="options">Configuration options.</param>
    public NetZerionClient(HttpClient httpClient, NetZerionOptions options)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        options = options ?? throw new ArgumentNullException(nameof(options));

        _zerionHttpClient = new ZerionHttpClient(_httpClient, options);
        _ownsHttpClient = false;

        // Initialize clients
        Wallet = new WalletClient(_zerionHttpClient);
        Transactions = new TransactionClient(_zerionHttpClient);
    }

    /// <summary>
    /// Creates and configures an HTTP client with authentication and retry policies.
    /// </summary>
    private static HttpClient CreateHttpClient(NetZerionOptions options)
    {
        // Create the delegating handler chain
        var authHandler = new AuthenticationHandler(options.ApiKey!)
        {
            InnerHandler = new RetryHandler(options.MaxRetries, options.RetryStrategy)
            {
                InnerHandler = new HttpClientHandler()
            }
        };

        var client = new HttpClient(authHandler)
        {
            BaseAddress = new Uri(options.BaseUrl),
            Timeout = options.Timeout
        };

        // Add user agent header
        client.DefaultRequestHeaders.Add("User-Agent", options.UserAgent);

        return client;
    }

    /// <summary>
    /// Gets the number of API requests remaining for today.
    /// </summary>
    /// <returns>Number of requests remaining.</returns>
    public int GetDailyRequestsRemaining() => _zerionHttpClient.GetDailyRequestsRemaining();

    /// <summary>
    /// Gets the total number of API requests made today.
    /// </summary>
    /// <returns>Number of requests made.</returns>
    public int GetDailyRequestCount() => _zerionHttpClient.GetDailyRequestCount();

    /// <summary>
    /// Disposes of the resources used by the client.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
            return;

        _zerionHttpClient?.Dispose();

        if (_ownsHttpClient)
        {
            _httpClient?.Dispose();
        }

        _disposed = true;
        GC.SuppressFinalize(this);
    }
}
