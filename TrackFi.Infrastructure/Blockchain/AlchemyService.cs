using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net.Http.Json;
using System.Text.Json;
using TrackFi.Domain.Entities;
using TrackFi.Domain.Enums;
using TrackFi.Infrastructure.Caching;
using TrackFi.Infrastructure.Persistence.Repositories;

namespace TrackFi.Infrastructure.Blockchain;

/// <summary>
/// Service for interacting with Alchemy API to fetch blockchain data.
///
/// KNOWN ISSUES & WORKAROUNDS:
///
/// 1. NATIVE TOKEN PRICING:
///    Problem: Alchemy Pricing API does not accept the placeholder address
///             (0xEeeeeEeeeEeEeeEeEeEeeEEEeeeeEeeeeeeeEEeE) for native tokens (ETH, MATIC, etc).
///             This placeholder works for balance queries but returns "Token not found" for pricing.
///
///    Solution: Use wrapped token addresses (WETH, WMATIC) as pricing proxies since wrapped tokens
///              have 1:1 price parity with their native counterparts (1 ETH = 1 WETH).
///              See GetNativeTokenPriceAddress() in AnonymousPortfolioService for address mapping.
///
///    Example addresses:
///    - Ethereum: 0xC02aaA39b223FE8D0A0e5C4F27eAD9083C756Cc2 (WETH)
///    - Base: 0x4200000000000000000000000000000000000006 (WETH)
///    - Polygon: 0x0d500B1d8E8eF31E21C99d1Db9A6444d3ADf1270 (WMATIC)
///
/// 2. DUPLICATE TOKENS IN MULTI-NETWORK API:
///    Problem: Alchemy's multi-network token balance API (assets/tokens/by-address) returns
///             duplicate entries for each token. Every token appears exactly 2 times in the response
///             with identical data (address, balance, metadata, prices).
///
///    Solution: Deduplicate tokens by contract address before processing. Keep only the first
///              occurrence of each unique address. This deduplication is handled in
///              ProcessMultiNetworkTokensAsync() in AnonymousPortfolioService.
///
///    Impact: Without deduplication, users see duplicate tokens in their portfolio
///            (e.g., 2 USDC entries with same balance). This affects all networks when using
///            the multi-network endpoint.
///
/// 3. PRICING NETWORK SUPPORT:
///    Not all blockchain networks support Alchemy's Pricing API. Use SupportsAlchemyPricing()
///    to check if a network has pricing support before calling GetTokenPricesByAddressesAsync().
///    Networks without pricing support should gracefully handle null prices.
///    See GetAlchemyPriceNetwork() for the complete list of supported networks.
/// </summary>
public class AlchemyService(
    HttpClient httpClient,
    IOptions<AlchemyOptions> alchemyOptions,
    ILogger<AlchemyService> logger,
    DistributedCacheService cacheService,
    IOptions<CacheOptions> cacheOptions)
{
    private readonly HttpClient _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
    private readonly ILogger<AlchemyService> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly DistributedCacheService _cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));
    private readonly CacheOptions _cacheOptions = cacheOptions?.Value ?? throw new ArgumentNullException(nameof(cacheOptions));
    private readonly AlchemyOptions _alchemyOptions = alchemyOptions?.Value ?? throw new ArgumentNullException(nameof(alchemyOptions));
    private readonly string _apiKey = alchemyOptions?.Value.ApiKey ?? throw new ArgumentNullException("Alchemy API key not configured");

    // Limit concurrent cache operations to prevent memory issues in production
    // 50 is generous - allows good parallelism while preventing unbounded concurrency
    private static readonly SemaphoreSlim CacheSemaphore = new(50, 50);

    // REMOVED: GetTokenBalancesAsync() - real-time balance indexing causes N+1 problem, use Zerion offline indexing instead

    // REMOVED: GetAllBalancesAsync() - real-time balance indexing causes N+1 problem, use Zerion offline indexing instead

    // REMOVED: GetMultiNetworkTokenBalancesAsync() - real-time balance indexing causes N+1 problem, use Zerion offline indexing instead
    // REMOVED: FetchTokensForBatchAsync() - helper method for multi-network indexing, no longer needed

    // REMOVED: GetTokenMetadataAsync() - token metadata is now provided by Zerion in the portfolio response

    /// <summary>
    /// Get native token balance (ETH, MATIC, etc.).
    /// </summary>
    public async Task<string> GetNativeBalanceAsync(
        string walletAddress,
        BlockchainNetwork network,
        CancellationToken cancellationToken = default)
    {
        var endpoint = GetAlchemyEndpoint(network);
        var url = $"{endpoint}{_apiKey}";

        var requestBody = new
        {
            jsonrpc = "2.0",
            id = 1,
            method = "eth_getBalance",
            @params = new[] { walletAddress, "latest" }
        };

        try
        {
            var response = await _httpClient.PostAsJsonAsync(url, requestBody, cancellationToken);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<AlchemyBalanceResponse>(cancellationToken);
            return result?.Result ?? "0x0";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching native balance from Alchemy for {Wallet}", walletAddress);
            throw;
            //return "0x0";
        }
    }

    /// <summary>
    /// Get NFTs owned by a wallet.
    /// </summary>
    public async Task<List<AlchemyNft>> GetNftsAsync(
        string walletAddress,
        BlockchainNetwork network,
        CancellationToken cancellationToken = default)
    {
        var endpoint = GetAlchemyEndpoint(network);
        var url = $"{endpoint}{_apiKey}/getNFTs/?owner={walletAddress}";

        try
        {
            var response = await _httpClient.GetAsync(url, cancellationToken);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<AlchemyNftResponse>(cancellationToken);
            return result?.OwnedNfts ?? [];
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching NFTs from Alchemy for {Wallet}", walletAddress);
            throw;
        }
    }

    /// <summary>
    /// Get token prices by symbols from Alchemy Prices API.
    /// </summary>
    public async Task<Dictionary<string, decimal>> GetTokenPricesBySymbolsAsync(
        List<string> symbols,
        CancellationToken cancellationToken = default)
    {
        if (symbols.Count == 0)
            return [];

        try
        {
            var url = $"https://api.g.alchemy.com/prices/v1/{_apiKey}/tokens/by-symbol";

            var requestBody = new
            {
                symbols
            };

            var response = await _httpClient.PostAsJsonAsync(url, requestBody, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Alchemy Prices API returned {StatusCode} for symbols: {Symbols}",
                    response.StatusCode, string.Join(",", symbols));
                return [];
            }

            var result = await response.Content.ReadFromJsonAsync<AlchemyPricesResponse>(cancellationToken);

            if (result?.Data == null)
                return [];

            return result.Data
                .Where(token => !token.Error.HasValue) // Skip tokens with errors
                .Where(token => token.Prices != null && token.Prices.Count != 0)
                .Select(token => new
                {
                    Symbol = token.Symbol?.ToUpperInvariant() ?? string.Empty,
                    UsdPrice = token.Prices!.FirstOrDefault(p =>
                        string.Equals(p.Currency, "USD", StringComparison.OrdinalIgnoreCase))
                })
                .Where(x => x.UsdPrice?.Value != null && decimal.TryParse(x.UsdPrice.Value, out _))
                .ToDictionary(
                    x => x.Symbol,
                    x => decimal.Parse(x.UsdPrice!.Value!)
                );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching prices from Alchemy for symbols: {Symbols}", string.Join(",", symbols));
            throw;
        }
    }

    /// <summary>
    /// Get token prices by addresses from Alchemy Prices API (batch request).
    /// Prices are cached individually per token for 1 minute (improves cache hit rate across different requests).
    /// Returns both successful prices and failed token details for visibility.
    /// </summary>
    public async Task<(Dictionary<string, decimal> prices, List<TokenPriceError> failures)> GetTokenPricesByAddressesAsync(
        List<(string address, string symbol, BlockchainNetwork network)> tokens,
        CancellationToken cancellationToken = default)
    {
        if (tokens.Count == 0)
            return (new Dictionary<string, decimal>(), new List<TokenPriceError>());

        // Step 1: Check cache in parallel
        var (cachedPrices, tokensToFetch) = await CheckCachedPricesAsync(tokens, cancellationToken);

        _logger.LogInformation("Price cache: {CacheHits} hits, {CacheMisses} misses out of {Total} tokens",
            cachedPrices.Count, tokensToFetch.Count, tokens.Count);

        if (tokensToFetch.Count == 0)
        {
            return (cachedPrices, new List<TokenPriceError>());
        }

        // Step 2: Fetch prices from Alchemy API
        var (fetchedPrices, failures) = await FetchPricesFromAlchemyAsync(tokensToFetch, tokens, cancellationToken);

        // Step 3: Merge cached and fetched prices
        foreach (var kvp in fetchedPrices)
        {
            cachedPrices[kvp.Key] = kvp.Value;
        }

        // Step 4: Cache newly fetched prices
        await CacheFetchedPricesAsync(fetchedPrices, tokensToFetch, cancellationToken);

        return (cachedPrices, failures);
    }

    /// <summary>
    /// Checks cache for token prices in parallel, returns cached prices and tokens that need fetching.
    /// </summary>
    private async Task<(Dictionary<string, decimal> cachedPrices, List<(string address, string symbol, BlockchainNetwork network)> tokensToFetch)>
        CheckCachedPricesAsync(
            List<(string address, string symbol, BlockchainNetwork network)> tokens,
            CancellationToken cancellationToken)
    {
        var cachedPrices = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
        var tokensToFetch = new List<(string address, string symbol, BlockchainNetwork network)>();

        var cacheTasks = tokens.Select(async token =>
        {
            await CacheSemaphore.WaitAsync(cancellationToken);
            try
            {
                var symbolUpper = token.symbol.ToUpperInvariant();
                var cacheKey = DistributedCacheService.GenerateKey(
                    "token_price",
                    token.network.ToString(),
                    symbolUpper,
                    token.address.ToLowerInvariant());
                var cachedPrice = await _cacheService.GetAsync<decimal?>(cacheKey, cancellationToken);

                return new { Token = token, CachedPrice = cachedPrice };
            }
            finally
            {
                CacheSemaphore.Release();
            }
        }).ToList();

        var cacheResults = await Task.WhenAll(cacheTasks);

        foreach (var result in cacheResults)
        {
            if (result.CachedPrice.HasValue)
            {
                cachedPrices[result.Token.address.ToLowerInvariant()] = result.CachedPrice.Value;
            }
            else
            {
                tokensToFetch.Add(result.Token);
            }
        }

        return (cachedPrices, tokensToFetch);
    }

    /// <summary>
    /// Fetches token prices from Alchemy API and processes the response.
    /// </summary>
    private async Task<(Dictionary<string, decimal> prices, List<TokenPriceError> failures)>
        FetchPricesFromAlchemyAsync(
            List<(string address, string symbol, BlockchainNetwork network)> tokensToFetch,
            List<(string address, string symbol, BlockchainNetwork network)> allTokens,
            CancellationToken cancellationToken)
    {
        try
        {
            var url = $"https://api.g.alchemy.com/prices/v1/{_apiKey}/tokens/by-address";

            var requestBody = new
            {
                addresses = tokensToFetch.Select(t => new
                {
                    network = GetAlchemyPriceNetwork(t.network),
                    t.address
                }).ToList()
            };

            _logger.LogInformation(
                "Fetching prices from Alchemy API for {Count} tokens: {Symbols}",
                tokensToFetch.Count,
                string.Join(", ", tokensToFetch.Take(10).Select(t => t.symbol)) + (tokensToFetch.Count > 10 ? "..." : ""));

            var response = await _httpClient.PostAsJsonAsync(url, requestBody, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                return HandleApiError(allTokens, response.StatusCode);
            }

            var result = await response.Content.ReadFromJsonAsync<AlchemyPricesResponse>(cancellationToken);

            if (result?.Data == null)
            {
                return HandleEmptyResponse(allTokens);
            }

            return ProcessAlchemyResponse(result, tokensToFetch);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching prices from Alchemy for {Count} addresses", tokensToFetch.Count);
            throw;
        }
    }

    /// <summary>
    /// Handles API error responses by returning all tokens as failures.
    /// </summary>
    private (Dictionary<string, decimal> prices, List<TokenPriceError> failures) HandleApiError(
        List<(string address, string symbol, BlockchainNetwork network)> tokens,
        System.Net.HttpStatusCode statusCode)
    {
        _logger.LogError("Alchemy Prices API returned {StatusCode} for {Count} addresses",
            statusCode, tokens.Count);

        var allFailed = tokens.Select(t => new TokenPriceError
        {
            Address = t.address,
            Symbol = t.symbol,
            Error = $"HTTP {statusCode}"
        }).ToList();

        return (new Dictionary<string, decimal>(), allFailed);
    }

    /// <summary>
    /// Handles empty responses from Alchemy API.
    /// </summary>
    private (Dictionary<string, decimal> prices, List<TokenPriceError> failures) HandleEmptyResponse(
        List<(string address, string symbol, BlockchainNetwork network)> tokens)
    {
        _logger.LogError("Alchemy returned null/empty response for {Count} tokens", tokens.Count);

        var allFailed = tokens.Select(t => new TokenPriceError
        {
            Address = t.address,
            Symbol = t.symbol,
            Error = "Empty response from Alchemy"
        }).ToList();

        return (new Dictionary<string, decimal>(), allFailed);
    }

    /// <summary>
    /// Processes Alchemy API response and extracts prices and failures.
    /// </summary>
    private (Dictionary<string, decimal> prices, List<TokenPriceError> failures) ProcessAlchemyResponse(
        AlchemyPricesResponse result,
        List<(string address, string symbol, BlockchainNetwork network)> tokensToFetch)
    {
        var prices = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
        var failures = new List<TokenPriceError>();

        var tokenMetadata = BuildTokenMetadataLookup(tokensToFetch);

        foreach (var tokenData in result.Data)
        {
            ProcessSingleTokenPrice(tokenData, tokenMetadata, prices, failures);
        }

        return (prices, failures);
    }

    /// <summary>
    /// Builds a lookup dictionary for token metadata.
    /// </summary>
    private static Dictionary<string, (string symbol, BlockchainNetwork network)> BuildTokenMetadataLookup(
        List<(string address, string symbol, BlockchainNetwork network)> tokens)
    {
        return tokens
            .GroupBy(t => t.address.ToLowerInvariant(), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                g => g.Key,
                g => (symbol: g.First().symbol, network: g.First().network),
                StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Processes a single token price from Alchemy response.
    /// </summary>
    private static void ProcessSingleTokenPrice(
        AlchemyTokenPrice tokenData,
        Dictionary<string, (string symbol, BlockchainNetwork network)> tokenMetadata,
        Dictionary<string, decimal> prices,
        List<TokenPriceError> failures)
    {
        if (tokenData.Error.HasValue)
        {
            AddTokenFailure(tokenData, tokenMetadata, failures, ExtractErrorMessage(tokenData.Error.Value));
            return;
        }

        if (string.IsNullOrEmpty(tokenData.Address))
            return;

        if (TryExtractUsdPrice(tokenData, out var price))
        {
            prices[tokenData.Address.ToLowerInvariant()] = price;
        }
        else
        {
            var errorMsg = tokenData.Prices == null || tokenData.Prices.Count == 0
                ? "No price data available"
                : "Invalid or missing USD price";
            AddTokenFailure(tokenData, tokenMetadata, failures, errorMsg);
        }
    }

    /// <summary>
    /// Extracts error message from JSON element.
    /// </summary>
    private static string ExtractErrorMessage(System.Text.Json.JsonElement error)
    {
        return error.ValueKind == System.Text.Json.JsonValueKind.String
            ? error.GetString() ?? "Unknown error"
            : error.ToString();
    }

    /// <summary>
    /// Attempts to extract USD price from token data.
    /// </summary>
    private static bool TryExtractUsdPrice(AlchemyTokenPrice tokenData, out decimal price)
    {
        price = 0;

        if (tokenData.Prices == null || tokenData.Prices.Count == 0)
            return false;

        var usdPrice = tokenData.Prices.FirstOrDefault(p =>
            string.Equals(p.Currency, "USD", StringComparison.OrdinalIgnoreCase));

        return usdPrice?.Value != null && decimal.TryParse(usdPrice.Value, out price);
    }

    /// <summary>
    /// Adds a token failure entry with metadata.
    /// </summary>
    private static void AddTokenFailure(
        AlchemyTokenPrice tokenData,
        Dictionary<string, (string symbol, BlockchainNetwork network)> tokenMetadata,
        List<TokenPriceError> failures,
        string errorMessage)
    {
        var address = tokenData.Address ?? "unknown";
        var symbol = tokenMetadata.TryGetValue(address.ToLowerInvariant(), out var meta)
            ? meta.symbol
            : tokenData.Symbol ?? "UNKNOWN";

        failures.Add(new TokenPriceError
        {
            Address = address,
            Symbol = symbol,
            Error = errorMessage
        });
    }

    /// <summary>
    /// Caches fetched prices in parallel with 1-minute TTL.
    /// </summary>
    private async Task CacheFetchedPricesAsync(
        Dictionary<string, decimal> prices,
        List<(string address, string symbol, BlockchainNetwork network)> tokensToFetch,
        CancellationToken cancellationToken)
    {
        var tokenMetadata = BuildTokenMetadataLookup(tokensToFetch);

        var cacheWriteTasks = prices.Select(async kvp =>
        {
            await CacheSemaphore.WaitAsync(cancellationToken);
            try
            {
                if (tokenMetadata.TryGetValue(kvp.Key, out var meta))
                {
                    var symbolUpper = meta.symbol.ToUpperInvariant();
                    var cacheKey = DistributedCacheService.GenerateKey(
                        "token_price",
                        meta.network.ToString(),
                        symbolUpper,
                        kvp.Key);
                    await _cacheService.SetAsync(cacheKey, (decimal?)kvp.Value, _cacheOptions.TokenPriceTtl, cancellationToken);
                }
            }
            finally
            {
                CacheSemaphore.Release();
            }
        });

        await Task.WhenAll(cacheWriteTasks);
    }

    /// <summary>
    /// Maps BlockchainNetwork enum to Alchemy's pricing API network identifier.
    /// Only networks listed here support Alchemy's pricing API.
    /// Reference: https://docs.alchemy.com/reference/token-api-quickstart
    /// </summary>
    private static string GetAlchemyPriceNetwork(BlockchainNetwork network)
    {
        return network switch
        {
            // L1 Chains with pricing support
            BlockchainNetwork.Ethereum => "eth-mainnet",
            BlockchainNetwork.Polygon => "polygon-mainnet",
            BlockchainNetwork.BNBChain => "bnb-mainnet",
            BlockchainNetwork.Avalanche => "avax-mainnet",
            BlockchainNetwork.Gnosis => "gnosis-mainnet",

            // L2 Chains with pricing support
            BlockchainNetwork.Optimism => "opt-mainnet",
            BlockchainNetwork.Arbitrum => "arb-mainnet",
            BlockchainNetwork.ArbitrumNova => "arbnova-mainnet",
            BlockchainNetwork.Base => "base-mainnet",
            BlockchainNetwork.PolygonZkEVM => "polygonzkevm-mainnet",
            BlockchainNetwork.ZkSync => "zksync-mainnet",
            BlockchainNetwork.Blast => "blast-mainnet",
            BlockchainNetwork.Linea => "linea-mainnet",
            BlockchainNetwork.Scroll => "scroll-mainnet",
            BlockchainNetwork.Metis => "metis-mainnet",
            BlockchainNetwork.Zora => "zora-mainnet",

            // Additional networks with pricing support
            BlockchainNetwork.Unichain => "unichain-mainnet",

            _ => throw new NotSupportedException(
                $"Network '{network}' does not support Alchemy pricing API. " +
                $"Only specific networks have pricing support. See appsettings.json for supported networks.")
        };
    }

    /// <summary>
    /// Checks if a blockchain network supports Alchemy's pricing API.
    /// Networks without pricing support will need alternative price sources.
    /// </summary>
    public static bool SupportsAlchemyPricing(BlockchainNetwork network)
    {
        try
        {
            GetAlchemyPriceNetwork(network);
            return true;
        }
        catch (NotSupportedException)
        {
            return false;
        }
    }

    /// <summary>
    /// Maps BlockchainNetwork enum to Alchemy RPC endpoint.
    /// Only networks supported by Alchemy SDK are included.
    /// Reference: https://docs.alchemy.com/reference/api-overview
    /// </summary>
    private static string GetAlchemyEndpoint(BlockchainNetwork network)
    {
        return network switch
        {
            // ==================================================================================
            // LAYER 1 CHAINS (Alchemy Supported)
            // ==================================================================================
            BlockchainNetwork.Ethereum => "https://eth-mainnet.g.alchemy.com/v2/",
            BlockchainNetwork.Polygon => "https://polygon-mainnet.g.alchemy.com/v2/",
            BlockchainNetwork.BNBChain => "https://bnb-mainnet.g.alchemy.com/v2/",
            BlockchainNetwork.Avalanche => "https://avax-mainnet.g.alchemy.com/v2/",
            BlockchainNetwork.Gnosis => "https://gnosis-mainnet.g.alchemy.com/v2/",
            BlockchainNetwork.Celo => "https://celo-mainnet.g.alchemy.com/v2/",
            BlockchainNetwork.Moonbeam => "https://moonbeam-mainnet.g.alchemy.com/v2/",
            BlockchainNetwork.Astar => "https://astar-mainnet.g.alchemy.com/v2/",

            // ==================================================================================
            // LAYER 2 CHAINS (Alchemy Supported)
            // ==================================================================================
            BlockchainNetwork.Optimism => "https://opt-mainnet.g.alchemy.com/v2/",
            BlockchainNetwork.Arbitrum => "https://arb-mainnet.g.alchemy.com/v2/",
            BlockchainNetwork.ArbitrumNova => "https://arbnova-mainnet.g.alchemy.com/v2/",
            BlockchainNetwork.Base => "https://base-mainnet.g.alchemy.com/v2/",
            BlockchainNetwork.PolygonZkEVM => "https://polygonzkevm-mainnet.g.alchemy.com/v2/",
            BlockchainNetwork.ZkSync => "https://zksync-mainnet.g.alchemy.com/v2/",
            BlockchainNetwork.Blast => "https://blast-mainnet.g.alchemy.com/v2/",
            BlockchainNetwork.Linea => "https://linea-mainnet.g.alchemy.com/v2/",
            BlockchainNetwork.Scroll => "https://scroll-mainnet.g.alchemy.com/v2/",
            BlockchainNetwork.Metis => "https://metis-mainnet.g.alchemy.com/v2/",
            BlockchainNetwork.Zora => "https://zora-mainnet.g.alchemy.com/v2/",
            BlockchainNetwork.Mode => "https://mode-mainnet.g.alchemy.com/v2/",
            BlockchainNetwork.Mantle => "https://mantle-mainnet.g.alchemy.com/v2/",
            BlockchainNetwork.Fraxtal => "https://frax-mainnet.g.alchemy.com/v2/",
            BlockchainNetwork.Unichain => "https://unichain-mainnet.g.alchemy.com/v2/",
            BlockchainNetwork.Boba => "https://boba-mainnet.g.alchemy.com/v2/",
            BlockchainNetwork.Rootstock => "https://rootstock-mainnet.g.alchemy.com/v2/",
            BlockchainNetwork.DegenChain => "https://degen-mainnet.g.alchemy.com/v2/",

            // Networks NOT supported by Alchemy (keep for clarity)
            _ => throw new NotSupportedException(
                $"Network '{network}' is not supported by Alchemy. " +
                $"Supported networks: Ethereum, Polygon, BNB Chain, Avalanche, Gnosis, Celo, Moonbeam, Astar, " +
                $"Optimism, Arbitrum, Arbitrum Nova, Base, Polygon zkEVM, zkSync, Blast, Linea, Scroll, Metis, Zora, " +
                $"Mode, Mantle, Fraxtal, Unichain, Boba, Rootstock, Degen Chain. " +
                $"See https://docs.alchemy.com/reference/api-overview")
        };
    }
}

// Response models
public class AlchemyTokenBalanceResponse
{
    public TokenBalanceResult? Result { get; set; }
}

public class TokenBalanceResult
{
    public string? Address { get; set; }
    public List<AlchemyTokenBalance> TokenBalances { get; set; } = [];
}

public class AlchemyTokenBalance
{
    public string ContractAddress { get; set; } = string.Empty;
    public string TokenBalance { get; set; } = string.Empty;
}

public class AlchemyTokenMetadataResponse
{
    public AlchemyTokenMetadata? Result { get; set; }
}

public class AlchemyTokenMetadata
{
    public int? Decimals { get; set; }
    public string? Logo { get; set; }
    public string? Name { get; set; }
    public string? Symbol { get; set; }
}

public class AlchemyBalanceResponse
{
    public string? Result { get; set; }
}

public class AlchemyNftResponse
{
    public List<AlchemyNft> OwnedNfts { get; set; } = [];
    public int TotalCount { get; set; }
}

public class AlchemyNft
{
    public AlchemyNftContract Contract { get; set; } = new();
    public AlchemyNftId Id { get; set; } = new();
    public string? Title { get; set; }
    public string? Description { get; set; }
    public System.Text.Json.JsonElement? Metadata { get; set; } // Can be object, string, or null
    public AlchemyNftTokenUri? TokenUri { get; set; }
    public AlchemyNftContractMetadata? ContractMetadata { get; set; }
}

public class AlchemyNftContract
{
    public string Address { get; set; } = string.Empty;
    public string? Name { get; set; }
    public string? Symbol { get; set; }
    public int? TotalSupply { get; set; }
    public string? TokenType { get; set; } // ERC721, ERC1155
}

public class AlchemyNftId
{
    public string TokenId { get; set; } = string.Empty;
    public AlchemyNftTokenMetadata? TokenMetadata { get; set; }
}

public class AlchemyNftTokenMetadata
{
    public string? TokenType { get; set; } // ERC721, ERC1155
}

public class AlchemyNftTokenUri
{
    public string? Raw { get; set; }
    public string? Gateway { get; set; }
}

public class AlchemyNftContractMetadata
{
    public string? Name { get; set; }
    public string? Symbol { get; set; }
    public string? TotalSupply { get; set; }
    public string? TokenType { get; set; }
}

public class AlchemyNftMetadata
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public string? Image { get; set; }
    public string? ExternalUrl { get; set; }
}

// Alchemy Prices API response models
public class AlchemyPricesResponse
{
    public List<AlchemyTokenPrice> Data { get; set; } = [];
}

public class AlchemyTokenPrice
{
    public string? Network { get; set; }
    public string? Address { get; set; }
    public string? Symbol { get; set; }  // Keep this for by-symbol endpoint compatibility
    public List<AlchemyPrice>? Prices { get; set; }
    public System.Text.Json.JsonElement? Error { get; set; }  // Can be string or object
}

public class AlchemyPrice
{
    public string? Currency { get; set; }
    public string? Value { get; set; }
    public DateTime? LastUpdatedAt { get; set; }
}

/// <summary>
/// Represents a token that failed to get a price from Alchemy.
/// Used for visibility and debugging - we don't swallow errors silently.
/// </summary>
public record TokenPriceError
{
    public required string Address { get; init; }
    public required string Symbol { get; init; }
    public required string Error { get; init; }
}

// Multi-network assets API response models
public class AlchemyMultiNetworkAssetsResponse
{
    public AlchemyMultiNetworkAssetsData? Data { get; set; }
}

public class AlchemyMultiNetworkAssetsData
{
    public List<AlchemyMultiNetworkToken> Tokens { get; set; } = [];
    public string? PageKey { get; set; }
}

/// <summary>
/// Represents a token balance from the multi-network /assets/tokens/by-address endpoint.
/// Contains balance, metadata, and price data all in one response.
/// </summary>
public class AlchemyMultiNetworkToken
{
    /// <summary>Wallet address that owns this token</summary>
    public string? Address { get; set; }

    /// <summary>Network where this token exists (e.g., "eth-mainnet", "polygon-mainnet")</summary>
    public string? Network { get; set; }

    /// <summary>Token contract address</summary>
    public string? TokenAddress { get; set; }

    /// <summary>Raw token balance as string (needs decimals to format)</summary>
    public string? TokenBalance { get; set; }

    /// <summary>Token metadata (symbol, name, decimals, logo)</summary>
    public AlchemyMultiNetworkTokenMetadata? TokenMetadata { get; set; }

    /// <summary>Token prices in various currencies</summary>
    public List<AlchemyPrice>? TokenPrices { get; set; }

    /// <summary>Error message if token data could not be fetched</summary>
    public string? Error { get; set; }
}

public class AlchemyMultiNetworkTokenMetadata
{
    public int? Decimals { get; set; }  // Nullable because Alchemy returns null for native tokens and some invalid tokens
    public string? Logo { get; set; }
    public string? Name { get; set; }
    public string? Symbol { get; set; }
}
