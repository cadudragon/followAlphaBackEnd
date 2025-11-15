using System.Diagnostics;
using System.Numerics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TrackFi.Application.Portfolio.DTOs;
using TrackFi.Domain.Entities;
using TrackFi.Domain.Enums;
using TrackFi.Domain.Interfaces;
using TrackFi.Infrastructure.Blockchain;
using TrackFi.Infrastructure.Caching;
using TrackFi.Infrastructure.Persistence.Repositories;

namespace TrackFi.Infrastructure.Portfolio;

/// <summary>
/// Service for fetching and aggregating portfolio data for anonymous (unauthenticated) wallets.
/// Data is cached in Redis with TTL and NOT persisted to database.
/// Only verified/whitelisted tokens are returned to users.
/// Automatically verifies tokens via CoinMarketCap if not in cache.
/// Implements Bulkhead Isolation Pattern to prevent resource exhaustion under high load.
/// </summary>
public class AnonymousPortfolioService(
    AlchemyService alchemyService,
    DistributedCacheService cacheService,
    IVerifiedTokenRepository verifiedTokenRepository,
    UnlistedTokenRepository unlistedTokenRepository,
    TokenVerificationService tokenVerificationService,
    INetworkMetadataRepository networkMetadataRepository,
    IOptions<CacheOptions> cacheOptions,
    IOptions<AlchemyOptions> alchemyOptions,
    ILogger<AnonymousPortfolioService> logger)
{
    /// <summary>
    /// Bulkhead semaphore to limit concurrent metadata fetch operations.
    /// Prevents thundering herd problem that can exhaust HTTP and database connection pools.
    /// Limit: 10 concurrent operations (tested to balance throughput vs resource usage).
    /// Pattern: Netflix Hystrix Bulkhead Isolation
    /// </summary>
    private static readonly SemaphoreSlim _metadataFetchSemaphore = new(10, 10);

    /// <summary>
    /// Maximum timeout for metadata fetch operations.
    /// Prevents orphaned tasks from continuing after client disconnects.
    /// </summary>
    private static readonly TimeSpan MetadataFetchTimeout = TimeSpan.FromSeconds(30);

    private readonly AlchemyService _alchemyService = alchemyService ?? throw new ArgumentNullException(nameof(alchemyService));
    private readonly DistributedCacheService _cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));
    private readonly IVerifiedTokenRepository _verifiedTokenRepository = verifiedTokenRepository ?? throw new ArgumentNullException(nameof(verifiedTokenRepository));
    private readonly UnlistedTokenRepository _unlistedTokenRepository = unlistedTokenRepository ?? throw new ArgumentNullException(nameof(unlistedTokenRepository));
    private readonly TokenVerificationService _tokenVerificationService = tokenVerificationService ?? throw new ArgumentNullException(nameof(tokenVerificationService));
    private readonly INetworkMetadataRepository _networkMetadataRepository = networkMetadataRepository ?? throw new ArgumentNullException(nameof(networkMetadataRepository));
    private readonly CacheOptions _cacheOptions = cacheOptions?.Value ?? throw new ArgumentNullException(nameof(cacheOptions));
    private readonly AlchemyOptions _alchemyOptions = alchemyOptions?.Value ?? throw new ArgumentNullException(nameof(alchemyOptions));
    private readonly ILogger<AnonymousPortfolioService> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    /// <summary>
    /// Get wallet balances (tokens only) aggregated across priority networks from configuration.
    /// Uses two-phase architecture:
    /// Phase 1: Fetch all balances in parallel (cached, 3-min TTL)
    /// Phase 2: Fetch all prices in a SINGLE batched API call (cached, 1-min TTL)
    /// This approach reduces API calls by 75% compared to per-network pricing.
    /// </summary>
    public async Task<MultiNetworkWalletDto> GetMultiNetworkWalletAsync(
        string walletAddress,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Fetching multi-network wallet for {Wallet} using priority networks from configuration",
            walletAddress);

        var priorityNetworks = _alchemyOptions.GetPriorityNetworks();
        var networkMetadata = await _networkMetadataRepository.GetAllAsync(cancellationToken);

        // ===== PHASE 1: Fetch all balances in parallel (WITHOUT prices) =====
        var balanceTasks = priorityNetworks.Select(network =>
            GetNetworkBalancesWithoutPricesAsync(walletAddress, network, cancellationToken)
        ).ToList();

        var balanceResults = await Task.WhenAll(balanceTasks);

        // Build network -> balances dictionary (filter out empty networks)
        var networkBalances = priorityNetworks
            .Zip(balanceResults, (network, balances) => new { network, balances })
            .Where(x => x.balances.Count > 0)
            .ToDictionary(x => x.network, x => x.balances);

        if (networkBalances.Count == 0)
        {
            _logger.LogInformation("No balances found for {Wallet} across any network", walletAddress);
            return new MultiNetworkWalletDto
            {
                WalletAddress = walletAddress,
                IsAnonymous = true,
                Summary = new WalletSummaryDto
                {
                    TotalValueUsd = 0,
                    TotalTokens = 0,
                    LastUpdated = DateTime.UtcNow
                },
                Networks = new List<NetworkWalletDto>(),
                CacheExpiresAt = DateTime.UtcNow.AddMinutes(3)
            };
        }

        // ===== PHASE 2: Single batched price fetch for ALL networks =====
        var pricesByNetworkAndAddress = await FetchPricesForMultiNetworkTokensAsync(
            networkBalances,
            cancellationToken);

        // ===== PHASE 3: Build network DTOs with prices =====
        var networkData = networkBalances.Select(kvp =>
        {
            var network = kvp.Key;
            var balances = kvp.Value;

            // Enrich with prices from batched lookup
            var tokens = balances.Select(b =>
            {
                decimal? priceUsd = null;

                if (b.IsNative)
                {
                    // Use wrapped token price for native token
                    var wrappedAddress = GetNativeTokenPriceAddress(network)?.ToLowerInvariant();
                    if (wrappedAddress != null &&
                        pricesByNetworkAndAddress.TryGetValue((network, wrappedAddress), out var price))
                    {
                        priceUsd = price;
                    }
                }
                else if (!string.IsNullOrEmpty(b.ContractAddress))
                {
                    // Use token's own price
                    var key = b.ContractAddress.ToLowerInvariant();
                    if (pricesByNetworkAndAddress.TryGetValue((network, key), out var price))
                    {
                        priceUsd = price;
                    }
                }

                return new TokenBalanceDto
                {
                    ContractAddress = b.ContractAddress,
                    Network = b.Network,
                    Symbol = b.Symbol,
                    Name = b.Name,
                    Balance = b.Balance,
                    Decimals = b.Decimals,
                    BalanceFormatted = b.BalanceFormatted,
                    Price = priceUsd.HasValue ? new PriceInfoDto
                    {
                        Usd = priceUsd.Value,
                        LastUpdated = DateTime.UtcNow
                    } : null,
                    ValueUsd = priceUsd.HasValue ? b.BalanceFormatted * priceUsd.Value : null,
                    LogoUrl = b.LogoUrl
                };
            })
            .OrderByDescending(t => t.ValueUsd ?? 0)
            .ToList();

            var networkLogoUrl = networkMetadata.TryGetValue(network, out var metadata)
                ? metadata.LogoUrl
                : null;

            return new NetworkWalletDto
            {
                Network = network.ToString(),
                NetworkLogoUrl = networkLogoUrl,
                TotalValueUsd = tokens.Sum(t => t.ValueUsd ?? 0),
                Tokens = tokens,
                TokenCount = tokens.Count
            };
        }).ToList();

        var allTokens = networkData.SelectMany(n => n.Tokens).ToList();
        var totalValueUsd = networkData.Sum(n => n.TotalValueUsd);

        _logger.LogInformation(
            "Multi-network wallet loaded: {TotalTokens} tokens across {NetworkCount} networks with total value ${Value:N2}",
            allTokens.Count,
            networkData.Count,
            totalValueUsd);

        return new MultiNetworkWalletDto
        {
            WalletAddress = walletAddress,
            IsAnonymous = true,
            Summary = new WalletSummaryDto
            {
                TotalValueUsd = totalValueUsd,
                TotalTokens = allTokens.Count,
                LastUpdated = DateTime.UtcNow
            },
            Networks = networkData,
            CacheExpiresAt = DateTime.UtcNow.AddMinutes(3)
        };
    }

    /// <summary>
    /// Get NFTs aggregated across major networks.
    /// Only queries networks where NFTs are commonly held to avoid excessive API calls.
    /// </summary>
    public async Task<MultiNetworkNftDto> GetMultiNetworkNftsAsync(
        string walletAddress,
        CancellationToken cancellationToken = default)
    {
        // Only query major networks for NFTs (most NFT activity happens on these chains)
        // Querying all 50 networks would be too slow and wasteful
        var networks = new[]
        {
            BlockchainNetwork.Ethereum,      // Primary NFT network
            BlockchainNetwork.Polygon,       // Popular for gaming NFTs
            BlockchainNetwork.Arbitrum,      // Growing L2 NFT ecosystem
            BlockchainNetwork.Optimism,      // OP NFTs
            BlockchainNetwork.Base,          // Base NFTs
            BlockchainNetwork.ZkSync,        // zkSync NFTs
            BlockchainNetwork.BNBChain,      // BSC NFTs
            BlockchainNetwork.Avalanche      // AVAX NFTs
        };

        _logger.LogInformation(
            "Fetching multi-network NFTs for {Wallet} across {NetworkCount} major networks",
            walletAddress,
            networks.Length);

        // Fetch NFTs from major networks in parallel
        var nftTasks = networks.Select(network =>
            GetNftsAsync(walletAddress, network, cancellationToken)
        ).ToArray();

        var networkNfts = await Task.WhenAll(nftTasks);

        // Aggregate results
        var allNfts = networkNfts.SelectMany(n => n).ToList();
        var uniqueCollections = allNfts
            .Select(n => n.CollectionName?.ToLowerInvariant() ?? n.ContractAddress.ToLowerInvariant())
            .Distinct()
            .Count();

        // Load network metadata for logos
        var networkMetadata = await _networkMetadataRepository.GetAllAsync(cancellationToken);

        // Build network-specific data (only NFTs)
        var networkData = networks.Zip(networkNfts, (network, nfts) =>
        {
            // Get network logo URL
            var networkLogoUrl = networkMetadata.TryGetValue(network, out var metadata)
                ? metadata.LogoUrl
                : null;

            return new NetworkNftDto
            {
                Network = network.ToString(),
                NetworkLogoUrl = networkLogoUrl,
                Nfts = nfts,
                NftCount = nfts.Count
            };
        })
        .Where(n => n.NftCount > 0) // Filter out networks with no NFTs
        .ToList();

        _logger.LogInformation(
            "Multi-network NFTs loaded: {TotalNfts} NFTs across {NetworkCount} networks",
            allNfts.Count,
            networkData.Count);

        return new MultiNetworkNftDto
        {
            WalletAddress = walletAddress,
            IsAnonymous = true,
            Summary = new NftSummaryDto
            {
                TotalNfts = allNfts.Count,
                TotalCollections = uniqueCollections,
                LastUpdated = DateTime.UtcNow
            },
            Networks = networkData,
            CacheExpiresAt = DateTime.UtcNow.AddMinutes(3) // Cache TTL
        };
    }

    /// <summary>
    /// Get token balances with prices for a wallet.
    /// Balance data is cached for 3 minutes, prices are fetched fresh (1-min cache) on each request.
    /// </summary>
    public async Task<List<TokenBalanceDto>> GetTokenBalancesAsync(
        string walletAddress,
        BlockchainNetwork network,
        CancellationToken cancellationToken = default)
    {
        return await GetNetworkTokenBalancesInternalAsync(
            walletAddress,
            network,
            cancellationToken);
    }

    /// <summary>
    /// Get token balances for a network WITHOUT prices.
    /// Used by multi-network endpoint to fetch balances before batched pricing.
    /// Returns cached balances with 3-minute TTL.
    /// </summary>
    private async Task<List<CachedTokenBalance>> GetNetworkBalancesWithoutPricesAsync(
        string walletAddress,
        BlockchainNetwork network,
        CancellationToken cancellationToken)
    {
        var cacheKey = DistributedCacheService.GenerateKey("tokens_balances", network.ToString(), walletAddress);
        var cachedBalances = await _cacheService.GetAsync<List<CachedTokenBalance>>(cacheKey, cancellationToken);

        if (cachedBalances != null)
        {
            _logger.LogInformation("Token balances cache HIT for {Wallet} on {Network} ({Count} tokens)",
                walletAddress, network, cachedBalances.Count);
            return cachedBalances;
        }

        _logger.LogInformation("Token balances cache MISS for {Wallet} on {Network}, fetching from blockchain",
            walletAddress, network);

        // Fetch token balances from blockchain
        var balances = await FetchTokenBalancesFromBlockchainAsync(walletAddress, network, cancellationToken);

        // Cache balance data (without prices) for 3 minutes
        await _cacheService.SetAsync(cacheKey, balances, _cacheOptions.TokenBalanceTtl, cancellationToken);

        return balances;
    }

    /// <summary>
    /// Internal method to get token balances for a single network with caching.
    /// Used by single-network endpoint. Fetches balances then enriches with prices.
    /// </summary>
    private async Task<List<TokenBalanceDto>> GetNetworkTokenBalancesInternalAsync(
        string walletAddress,
        BlockchainNetwork network,
        CancellationToken cancellationToken)
    {
        // Step 1: Get balances (uses 3-min cache)
        var balances = await GetNetworkBalancesWithoutPricesAsync(walletAddress, network, cancellationToken);

        // Step 2: Enrich ALL tokens (ERC20 + native) with fresh prices (uses 1-min cache internally)
        var tokens = await EnrichBalancesWithPricesAsync(balances, network, cancellationToken);

        // Step 3: Sort by value (highest first)
        return tokens.OrderByDescending(t => t.ValueUsd ?? 0).ToList();
    }

    /// <summary>
    /// Fetch token balances from blockchain (without prices).
    /// Uses batched JSON-RPC request to fetch both native and ERC20 balances in a single HTTP call.
    /// Automatically verifies tokens via CoinMarketCap if not already verified.
    /// Infrastructure failures will propagate to the exception middleware for proper error handling.
    /// </summary>
    private async Task<List<CachedTokenBalance>> FetchTokenBalancesFromBlockchainAsync(
        string walletAddress,
        BlockchainNetwork network,
        CancellationToken cancellationToken)
    {
        var balances = new List<CachedTokenBalance>();

        // Fetch both native and ERC20 balances in a single batched JSON-RPC request (50% fewer HTTP calls)
        var (nativeBalance, tokenBalances) = await _alchemyService.GetAllBalancesAsync(walletAddress, network, cancellationToken);

        // Add native token balance
        var nativeTokenBalance = BuildCachedNativeTokenBalance(nativeBalance, network);
        if (nativeTokenBalance != null)
        {
            balances.Add(nativeTokenBalance);
        }

        // Process ERC20 tokens if any exist
        if (tokenBalances.Count == 0)
        {
            return balances;
        }

        _logger.LogInformation("Processing {Count} token balances for {Network}", tokenBalances.Count, network);

        var verifiedBalances = await ProcessAndVerifyTokenBalancesAsync(tokenBalances, network, cancellationToken);
        balances.AddRange(verifiedBalances);

        return balances;
    }

    /// <summary>
    /// Processes ERC20 token balances: classifies, verifies unknown tokens, and builds cached balance objects.
    /// </summary>
    private async Task<List<CachedTokenBalance>> ProcessAndVerifyTokenBalancesAsync(
        List<AlchemyTokenBalance> tokenBalances,
        BlockchainNetwork network,
        CancellationToken cancellationToken)
    {
        var verifiedTokens = await _verifiedTokenRepository.GetVerifiedTokensAsync(network, cancellationToken);
        var unlistedTokens = await _unlistedTokenRepository.GetUnlistedTokensAsync(network, cancellationToken);

        var (verifiedBalances, unknownBalances, unlistedCount) = ClassifyTokenBalancesByVerification(
            tokenBalances,
            verifiedTokens,
            unlistedTokens);

        _logger.LogInformation(
            "Token classification for {Network}: {VerifiedCount} verified, {UnlistedCount} unlisted (skipped), {UnknownCount} unknown",
            network,
            verifiedBalances.Count,
            unlistedCount,
            unknownBalances.Count);

        var metadataDict = await FetchMetadataForUnknownTokensAsync(unknownBalances, network, cancellationToken);
        var newlyVerifiedBalances = await VerifyUnknownTokensAsync(unknownBalances, metadataDict, network, cancellationToken);

        verifiedBalances.AddRange(newlyVerifiedBalances);
        verifiedTokens = await _verifiedTokenRepository.GetVerifiedTokensAsync(network, cancellationToken);

        _logger.LogInformation(
            "Final workflow summary for {Network}: {VerifiedCount} verified (will return), {UnlistedCount} scam (filtered out), {TotalCount} total processed",
            network,
            verifiedBalances.Count,
            unlistedCount,
            tokenBalances.Count);

        return BuildCachedBalanceObjects(verifiedBalances, verifiedTokens, metadataDict, network);
    }

    /// <summary>
    /// Classifies token balances into verified, unknown, and unlisted categories.
    /// </summary>
    private static (List<AlchemyTokenBalance> verified, List<AlchemyTokenBalance> unknown, int unlistedCount)
        ClassifyTokenBalancesByVerification(
            List<AlchemyTokenBalance> tokenBalances,
            Dictionary<string, VerifiedTokenCacheEntry> verifiedTokens,
            Dictionary<string, UnlistedToken> unlistedTokens)
    {
        var verified = new List<AlchemyTokenBalance>();
        var unknown = new List<AlchemyTokenBalance>();
        int unlistedCount = 0;

        foreach (var tb in tokenBalances)
        {
            var lowerAddress = tb.ContractAddress.ToLowerInvariant();

            if (verifiedTokens.ContainsKey(lowerAddress))
            {
                verified.Add(tb);
            }
            else if (unlistedTokens.ContainsKey(lowerAddress))
            {
                unlistedCount++;
            }
            else
            {
                unknown.Add(tb);
            }
        }

        return (verified, unknown, unlistedCount);
    }

    /// <summary>
    /// Fetches metadata for unknown tokens with controlled concurrency.
    /// Implements Bulkhead Isolation Pattern (Netflix Hystrix) to prevent resource exhaustion.
    /// Uses cooperative cancellation to stop orphaned tasks immediately on timeout or client disconnect.
    /// </summary>
    /// <remarks>
    /// Performance characteristics:
    /// - Before: 100+ concurrent HTTP + DB operations → pool exhaustion
    /// - After: Max 10 concurrent operations → controlled resource usage
    /// - Timeout: 30 seconds max (prevents orphaned tasks)
    /// - Cancellation: Propagates to all child tasks (no orphaned work)
    /// </remarks>
    private async Task<Dictionary<string, (string Symbol, AlchemyTokenMetadata? Metadata)>>
        FetchMetadataForUnknownTokensAsync(
            List<AlchemyTokenBalance> unknownBalances,
            BlockchainNetwork network,
            CancellationToken cancellationToken)
    {
        var metadataDict = new Dictionary<string, (string Symbol, AlchemyTokenMetadata? Metadata)>(
            StringComparer.OrdinalIgnoreCase);

        if (unknownBalances.Count == 0)
        {
            return metadataDict;
        }

        _logger.LogInformation(
            "Fetching metadata for {Count} unknown tokens on {Network} (max {MaxConcurrent} concurrent)",
            unknownBalances.Count,
            network,
            _metadataFetchSemaphore.CurrentCount);

        // Create linked cancellation token with timeout
        // This ensures all tasks stop immediately on timeout or client disconnect
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(MetadataFetchTimeout);

        var startTime = Stopwatch.StartNew();
        var metadataTasks = unknownBalances.Select(async tb =>
        {
            // Bulkhead isolation: wait for semaphore slot
            // This limits concurrent HTTP + DB operations
            await _metadataFetchSemaphore.WaitAsync(timeoutCts.Token);
            try
            {
                var metadata = await _alchemyService.GetTokenMetadataAsync(
                    tb.ContractAddress,
                    network,
                    timeoutCts.Token); // Use timeout token (not original)

                return (tb.ContractAddress, metadata?.Symbol ?? "UNKNOWN", metadata);
            }
            catch (OperationCanceledException)
            {
                // Expected: timeout or client disconnect
                _logger.LogWarning(
                    "Metadata fetch cancelled for {Contract} on {Network} (timeout or client disconnect)",
                    tb.ContractAddress,
                    network);
                return (tb.ContractAddress, "UNKNOWN", (AlchemyTokenMetadata?)null);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error fetching metadata for token {Contract} on {Network}",
                    tb.ContractAddress,
                    network);
                return (tb.ContractAddress, "UNKNOWN", (AlchemyTokenMetadata?)null);
            }
            finally
            {
                // Always release semaphore (even on exception/cancellation)
                _metadataFetchSemaphore.Release();
            }
        });

        try
        {
            var metadataResults = await Task.WhenAll(metadataTasks);
            foreach (var (contractAddress, symbol, metadata) in metadataResults)
            {
                metadataDict[contractAddress.ToLowerInvariant()] = (symbol, metadata);
            }

            startTime.Stop();
            _logger.LogInformation(
                "Fetched metadata for {SuccessCount}/{TotalCount} tokens on {Network} in {Duration}ms",
                metadataDict.Count(x => x.Value.Metadata != null),
                unknownBalances.Count,
                network,
                startTime.ElapsedMilliseconds);
        }
        catch (OperationCanceledException)
        {
            startTime.Stop();
            _logger.LogWarning(
                "Metadata fetch operation cancelled for {Network} after {Duration}ms (timeout or client disconnect). Returning partial results.",
                network,
                startTime.ElapsedMilliseconds);
            // Partial results still returned (best effort)
        }

        return metadataDict;
    }

    /// <summary>
    /// Verifies unknown tokens via CoinMarketCap and returns newly verified balances.
    /// </summary>
    private async Task<List<AlchemyTokenBalance>> VerifyUnknownTokensAsync(
        List<AlchemyTokenBalance> unknownBalances,
        Dictionary<string, (string Symbol, AlchemyTokenMetadata? Metadata)> metadataDict,
        BlockchainNetwork network,
        CancellationToken cancellationToken)
    {
        if (unknownBalances.Count == 0)
        {
            return new List<AlchemyTokenBalance>();
        }

        var tokensToVerify = unknownBalances
            .Select(tb =>
            {
                var lowerAddress = tb.ContractAddress.ToLowerInvariant();
                var symbol = metadataDict.TryGetValue(lowerAddress, out var meta) ? meta.Symbol : "UNKNOWN";
                return (tb.ContractAddress, symbol, network);
            })
            .ToList();

        var verificationResults = await _tokenVerificationService.VerifyTokensAsync(tokensToVerify, cancellationToken);

        var newlyVerified = unknownBalances
            .Where(tb => verificationResults.TryGetValue(tb.ContractAddress.ToLowerInvariant(), out var isVerified) && isVerified)
            .ToList();

        _logger.LogInformation("{NewlyVerifiedCount} unknown tokens verified via CMC on {Network}", newlyVerified.Count, network);

        return newlyVerified;
    }

    /// <summary>
    /// Builds cached balance objects from verified token balances.
    /// </summary>
    private List<CachedTokenBalance> BuildCachedBalanceObjects(
        List<AlchemyTokenBalance> verifiedBalances,
        Dictionary<string, VerifiedTokenCacheEntry> verifiedTokens,
        Dictionary<string, (string Symbol, AlchemyTokenMetadata? Metadata)> metadataDict,
        BlockchainNetwork network)
    {
        var balances = new List<CachedTokenBalance>();

        foreach (var tb in verifiedBalances)
        {
            try
            {
                var cachedBalance = BuildSingleCachedBalance(tb, verifiedTokens, metadataDict, network);
                if (cachedBalance != null)
                {
                    balances.Add(cachedBalance);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing token {Contract}", tb.ContractAddress);
                throw;
            }
        }

        return balances;
    }

    /// <summary>
    /// Builds a single cached balance object from token balance and metadata.
    /// </summary>
    private CachedTokenBalance? BuildSingleCachedBalance(
        AlchemyTokenBalance tb,
        Dictionary<string, VerifiedTokenCacheEntry> verifiedTokens,
        Dictionary<string, (string Symbol, AlchemyTokenMetadata? Metadata)> metadataDict,
        BlockchainNetwork network)
    {
        var lowerAddress = tb.ContractAddress.ToLowerInvariant();

        if (!verifiedTokens.TryGetValue(lowerAddress, out var verifiedToken))
        {
            _logger.LogWarning("Verified token metadata missing for {Contract} on {Network}", tb.ContractAddress, network);
            return null;
        }

        AlchemyTokenMetadata? metadata = null;
        if (metadataDict.TryGetValue(lowerAddress, out var metadataInfo))
        {
            metadata = metadataInfo.Metadata;
        }

        var decimals = DetermineTokenDecimals(metadata, verifiedToken, tb.ContractAddress, network);
        if (decimals == null)
        {
            return null;
        }

        var tokenSymbol = metadata?.Symbol ?? verifiedToken?.Symbol ?? "UNKNOWN";
        var name = metadata?.Name ?? verifiedToken?.Name ?? "Unknown Token";
        var logoUrl = metadata?.Logo ?? verifiedToken?.LogoUrl;
        var balance = HexToBigInteger(tb.TokenBalance);
        var balanceFormatted = ConvertToDecimal(balance, decimals.Value);

        if (balanceFormatted <= 0)
            return null;

        return new CachedTokenBalance
        {
            ContractAddress = tb.ContractAddress,
            Network = network.ToString(),
            Symbol = tokenSymbol,
            Name = name,
            Balance = tb.TokenBalance,
            Decimals = decimals.Value,
            BalanceFormatted = balanceFormatted,
            LogoUrl = logoUrl,
            IsNative = false
        };
    }

    /// <summary>
    /// Determines token decimals with fallback logic.
    /// </summary>
    private int? DetermineTokenDecimals(
        AlchemyTokenMetadata? metadata,
        VerifiedTokenCacheEntry verifiedToken,
        string contractAddress,
        BlockchainNetwork network)
    {
        var metadataDecimals = metadata?.Decimals;
        if (metadataDecimals.HasValue && metadataDecimals.Value > 0)
        {
            return metadataDecimals.Value;
        }

        if (verifiedToken.Decimals > 0)
        {
            return verifiedToken.Decimals;
        }

        _logger.LogWarning("Unable to determine decimals for token {Contract} on {Network}", contractAddress, network);
        return null;
    }

    /// <summary>
    /// Enrich cached balance data with fresh prices.
    /// </summary>
    private async Task<List<TokenBalanceDto>> EnrichBalancesWithPricesAsync(
        List<CachedTokenBalance> balances,
        BlockchainNetwork network,
        CancellationToken cancellationToken)
    {
        if (balances.Count == 0)
            return [];

        // Build list of token addresses to fetch prices for
        var tokenAddresses = new List<(string address, string symbol, BlockchainNetwork network)>();

        // Add wrapped token for native token pricing (WETH for ETH chains, WMATIC for Polygon)
        var nativeToken = balances.FirstOrDefault(b => b.IsNative);
        if (nativeToken != null)
        {
            var nativeTokenPriceAddress = GetNativeTokenPriceAddress(network);
            if (nativeTokenPriceAddress != null)
            {
                tokenAddresses.Add((nativeTokenPriceAddress, nativeToken.Symbol, network));
            }
        }

        // Add ERC20 token addresses
        var erc20Tokens = balances.Where(b => !b.IsNative && !string.IsNullOrEmpty(b.ContractAddress)).ToList();
        tokenAddresses.AddRange(erc20Tokens.Select(t => (t.ContractAddress!, t.Symbol, network)));

        // Fetch all prices in a single batch (uses 1-min cache)
        Dictionary<string, decimal> prices;
        List<TokenPriceError> failures;

        if (tokenAddresses.Count != 0)
        {
            (prices, failures) = await _alchemyService.GetTokenPricesByAddressesAsync(tokenAddresses, cancellationToken);

            if (failures.Count != 0)
            {
                _logger.LogError(
                    "PRICE FETCH FAILED for {FailedCount}/{TotalCount} tokens on {Network}:\n{FailedTokens}",
                    failures.Count,
                    tokenAddresses.Count,
                    network,
                    string.Join("\n", failures.Select(f => $"  - {f.Symbol} ({f.Address}): {f.Error}")));
            }
        }
        else
        {
            prices = [];
            failures = [];
        }

        _logger.LogInformation("Fetched {Count} fresh prices for enrichment ({FailedCount} failed)",
            prices.Count, failures.Count);

        // Build final DTOs with prices
        var tokens = balances.Select(b =>
        {
            decimal? priceUsd = null;

            if (b.IsNative)
            {
                // Use wrapped token price for native token (WETH for ETH, WMATIC for MATIC)
                var nativeTokenPriceAddress = GetNativeTokenPriceAddress(network);
                if (nativeTokenPriceAddress != null)
                {
                    priceUsd = prices.TryGetValue(nativeTokenPriceAddress.ToLowerInvariant(), out var price)
                        ? (decimal?)price
                        : null;
                }
            }
            else if (!string.IsNullOrEmpty(b.ContractAddress))
            {
                // Use token's own price
                priceUsd = prices.TryGetValue(b.ContractAddress.ToLowerInvariant(), out var price)
                    ? (decimal?)price
                    : null;
            }

            return new TokenBalanceDto
            {
                ContractAddress = b.ContractAddress,
                Network = b.Network,
                Symbol = b.Symbol,
                Name = b.Name,
                Balance = b.Balance,
                Decimals = b.Decimals,
                BalanceFormatted = b.BalanceFormatted,
                Price = priceUsd.HasValue ? new PriceInfoDto
                {
                    Usd = priceUsd.Value,
                    LastUpdated = DateTime.UtcNow
                } : null,
                ValueUsd = priceUsd.HasValue ? b.BalanceFormatted * priceUsd.Value : null,
                LogoUrl = b.LogoUrl
            };
        }).ToList();

        return tokens;
    }

    /// <summary>
    /// Fetches prices for tokens from multiple networks in batched API calls.
    /// Alchemy Prices API limits requests to a maximum number of distinct networks per call (configured via AlchemyOptions).
    /// Returns a dictionary with (network, address) composite keys for fast lookups.
    /// </summary>
    private async Task<Dictionary<(BlockchainNetwork network, string address), decimal>>
        FetchPricesForMultiNetworkTokensAsync(
            Dictionary<BlockchainNetwork, List<CachedTokenBalance>> networkBalances,
            CancellationToken cancellationToken)
    {
        // Step 1: Collect ALL tokens from ALL networks
        var allTokenAddresses = new List<(string address, string symbol, BlockchainNetwork network)>();

        foreach (var (network, balances) in networkBalances)
        {
            // Add native token wrapped address for pricing
            var nativeToken = balances.FirstOrDefault(b => b.IsNative);
            if (nativeToken != null)
            {
                var wrappedAddress = GetNativeTokenPriceAddress(network);
                if (wrappedAddress != null)
                {
                    allTokenAddresses.Add((wrappedAddress, nativeToken.Symbol, network));
                }
            }

            // Add ERC20 tokens
            var erc20Tokens = balances.Where(b => !b.IsNative && !string.IsNullOrEmpty(b.ContractAddress));
            allTokenAddresses.AddRange(erc20Tokens.Select(t => (t.ContractAddress!, t.Symbol, network)));
        }

        if (allTokenAddresses.Count == 0)
            return new Dictionary<(BlockchainNetwork, string), decimal>();

        // Step 2: Batch networks into groups based on Alchemy's Prices API limit
        var maxNetworksPerBatch = _alchemyOptions.BatchLimits.PricesApiMaxNetworks;
        var uniqueNetworks = allTokenAddresses.Select(t => t.network).Distinct().ToList();
        var networkBatches = new List<List<BlockchainNetwork>>();

        for (int i = 0; i < uniqueNetworks.Count; i += maxNetworksPerBatch)
        {
            networkBatches.Add(uniqueNetworks.Skip(i).Take(maxNetworksPerBatch).ToList());
        }

        _logger.LogInformation(
            "Fetching prices for {Count} tokens across {NetworkCount} networks in {BatchCount} batched request(s) (max {MaxNetworks} networks per batch)",
            allTokenAddresses.Count,
            uniqueNetworks.Count,
            networkBatches.Count,
            maxNetworksPerBatch);

        // Step 3: Fetch prices for each network batch in parallel
        var batchTasks = networkBatches.Select(async networksInBatch =>
        {
            var tokensInBatch = allTokenAddresses
                .Where(t => networksInBatch.Contains(t.network))
                .ToList();

            return await _alchemyService.GetTokenPricesByAddressesAsync(tokensInBatch, cancellationToken);
        }).ToList();

        var batchResults = await Task.WhenAll(batchTasks);

        // Step 4: Merge all batch results
        var prices = new Dictionary<string, decimal>();
        var failures = new List<TokenPriceError>();

        foreach (var (batchPrices, batchFailures) in batchResults)
        {
            foreach (var (address, price) in batchPrices)
            {
                prices[address] = price;
            }
            failures.AddRange(batchFailures);
        }

        if (failures.Count > 0)
        {
            _logger.LogWarning(
                "Price fetch failed for {FailedCount}/{TotalCount} tokens across all networks:\n{FailedTokens}",
                failures.Count,
                allTokenAddresses.Count,
                string.Join("\n", failures.Take(5).Select(f => $"  - {f.Symbol} ({f.Address}): {f.Error}")) +
                (failures.Count > 5 ? $"\n  ... and {failures.Count - 5} more" : ""));
        }

        // Step 5: Build lookup dictionary with (network, address) composite key
        var pricesByNetworkAndAddress = new Dictionary<(BlockchainNetwork, string), decimal>();

        foreach (var token in allTokenAddresses)
        {
            var key = token.address.ToLowerInvariant();
            if (prices.TryGetValue(key, out var price))
            {
                pricesByNetworkAndAddress[(token.network, key)] = price;
            }
        }

        _logger.LogInformation(
            "Successfully fetched {SuccessCount}/{TotalCount} prices across {NetworkCount} networks",
            pricesByNetworkAndAddress.Count,
            allTokenAddresses.Count,
            networkBalances.Count);

        return pricesByNetworkAndAddress;
    }

    private CachedTokenBalance? BuildCachedNativeTokenBalance(
        string hexBalance,
        BlockchainNetwork network)
    {
        try
        {
            var balance = HexToBigInteger(hexBalance);
            var balanceFormatted = ConvertToDecimal(balance, 18); // Native tokens have 18 decimals

            if (balanceFormatted <= 0)
                return null;

            var (symbol, name) = GetNativeTokenInfo(network);

            return new CachedTokenBalance
            {
                ContractAddress = null, // Native token
                Network = network.ToString(),
                Symbol = symbol,
                Name = name,
                Balance = hexBalance,
                Decimals = 18,
                BalanceFormatted = balanceFormatted,
                IsNative = true
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error building cached native token balance for {Network}", network);
            return null;
        }
    }

    /// <summary>
    /// Get NFTs for a wallet on a specific network.
    /// Infrastructure failures will propagate to the exception middleware for proper error handling.
    /// </summary>
    public async Task<List<NftDto>> GetNftsAsync(
        string walletAddress,
        BlockchainNetwork network,
        CancellationToken cancellationToken = default)
    {
        var cacheKey = DistributedCacheService.GenerateKey("nfts", network.ToString(), walletAddress);
        var cached = await _cacheService.GetAsync<List<NftDto>>(cacheKey, cancellationToken);

        if (cached != null)
        {
            _logger.LogInformation("NFTs cache hit for {Wallet}", walletAddress);
            return cached;
        }

        _logger.LogInformation("NFTs cache miss for {Wallet}, fetching from Alchemy", walletAddress);

        var alchemyNfts = await _alchemyService.GetNftsAsync(walletAddress, network, cancellationToken);
        var nfts = alchemyNfts.Select(nft => BuildNftDto(nft, network)).ToList();

        await _cacheService.SetAsync(cacheKey, nfts, _cacheOptions.NftTtl, cancellationToken);

        _logger.LogInformation("Found {Count} NFTs for {Wallet}", nfts.Count, walletAddress);

        return nfts;
    }

    /// <summary>
    /// Builds an NFT DTO from Alchemy NFT data.
    /// </summary>
    private static NftDto BuildNftDto(AlchemyNft nft, BlockchainNetwork network)
    {
        var metadata = ExtractNftMetadata(nft);

        return new NftDto
        {
            ContractAddress = nft.Contract.Address,
            TokenId = nft.Id.TokenId,
            Network = network.ToString(),
            Name = nft.Title ?? metadata.Name,
            Description = nft.Description ?? metadata.Description,
            CollectionName = nft.ContractMetadata?.Name ?? nft.Contract.Name,
            ImageUrl = metadata.Image,
            ExternalUrl = metadata.ExternalUrl,
            TokenStandard = DetermineTokenStandard(nft),
            Balance = DetermineTokenStandard(nft) == "ERC1155" ? 1 : null
        };
    }

    /// <summary>
    /// Extracts metadata properties from NFT metadata JSON.
    /// </summary>
    private static (string? Name, string? Description, string? Image, string? ExternalUrl) ExtractNftMetadata(AlchemyNft nft)
    {
        if (!nft.Metadata.HasValue || nft.Metadata.Value.ValueKind != System.Text.Json.JsonValueKind.Object)
        {
            return (null, null, null, null);
        }

        string? name = TryGetJsonProperty(nft.Metadata.Value, "name");
        string? description = TryGetJsonProperty(nft.Metadata.Value, "description");
        string? image = TryGetJsonProperty(nft.Metadata.Value, "image");
        string? externalUrl = TryGetJsonProperty(nft.Metadata.Value, "external_url");

        return (name, description, image, externalUrl);
    }

    /// <summary>
    /// Safely extracts a string property from a JSON element.
    /// </summary>
    private static string? TryGetJsonProperty(System.Text.Json.JsonElement element, string propertyName)
    {
        return element.TryGetProperty(propertyName, out var prop) ? prop.GetString() : null;
    }

    /// <summary>
    /// Determines the token standard (ERC721 or ERC1155) for an NFT.
    /// </summary>
    private static string DetermineTokenStandard(AlchemyNft nft)
    {
        return nft.ContractMetadata?.TokenType ?? nft.Id.TokenMetadata?.TokenType ?? "ERC721";
    }

    private static (string symbol, string name) GetNativeTokenInfo(BlockchainNetwork network)
    {
        return network switch
        {
            BlockchainNetwork.Ethereum => ("ETH", "Ethereum"),
            BlockchainNetwork.Polygon => ("MATIC", "Polygon"),
            BlockchainNetwork.Arbitrum => ("ETH", "Ethereum"),
            BlockchainNetwork.Base => ("ETH", "Ethereum"),
            BlockchainNetwork.Unichain => ("ETH", "Ethereum"),
            BlockchainNetwork.Optimism => ("ETH", "Ethereum"),
            BlockchainNetwork.BNBChain => ("BNB", "BNB"),
            BlockchainNetwork.Avalanche => ("AVAX", "Avalanche"),
            BlockchainNetwork.Fantom => ("FTM", "Fantom"),
            BlockchainNetwork.Gnosis => ("xDAI", "xDAI"),
            _ => ("ETH", "Ethereum") // Default to ETH for most EVM chains
        };
    }

    private static string? GetNativeTokenPriceAddress(BlockchainNetwork network)
    {
        return network switch
        {
            // L1 Networks
            BlockchainNetwork.Ethereum => "0xC02aaA39b223FE8D0A0e5C4F27eAD9083C756Cc2", // WETH
            BlockchainNetwork.Polygon => "0x0d500B1d8E8eF31E21C99d1Db9A6444d3ADf1270", // WMATIC (Wrapped MATIC)
            BlockchainNetwork.BNBChain => "0xbb4CdB9CBd36B01bD1cBaEBF2De08d9173bc095c", // WBNB (Wrapped BNB)
            BlockchainNetwork.Avalanche => "0xB31f66AA3C1e785363F0875A1B74E27b85FD66c7", // WAVAX (Wrapped AVAX)
            BlockchainNetwork.Gnosis => "0xe91D153E0b41518A2Ce8Dd3D7944Fa863463a97d", // WXDAI (Wrapped xDAI)

            // L2 Networks
            BlockchainNetwork.Optimism => "0x4200000000000000000000000000000000000006", // WETH on Optimism
            BlockchainNetwork.Arbitrum => "0x82aF49447D8a07e3bd95BD0d56f35241523fBab1", // WETH on Arbitrum
            BlockchainNetwork.Base => "0x4200000000000000000000000000000000000006", // WETH on Base
            BlockchainNetwork.Unichain => "0x4200000000000000000000000000000000000006", // WETH on Unichain

            _ => null
        };
    }

    private static BigInteger HexToBigInteger(string hex)
    {
        if (string.IsNullOrWhiteSpace(hex) || hex == "0x0")
            return BigInteger.Zero;

        hex = hex.StartsWith("0x") ? hex[2..] : hex;

        // Prepend '0' to ensure the value is parsed as positive (high bit = 0)
        // Without this, hex values starting with 8-f are parsed as negative
        return BigInteger.Parse("0" + hex, System.Globalization.NumberStyles.HexNumber);
    }

    private static decimal ConvertToDecimal(BigInteger value, int decimals)
    {
        if (value.IsZero)
            return 0;

        var divisor = BigInteger.Pow(10, decimals);
        var wholePart = BigInteger.Divide(value, divisor);
        var remainder = BigInteger.Remainder(value, divisor);

        var decimalPart = (decimal)remainder / (decimal)divisor;
        return (decimal)wholePart + decimalPart;
    }
}

/// <summary>
/// Internal model for caching token balance data WITHOUT prices.
/// Cached for 3 minutes, prices are fetched fresh on each request.
/// </summary>
internal class CachedTokenBalance
{
    public string? ContractAddress { get; set; }
    public string Network { get; set; } = string.Empty;
    public string Symbol { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Balance { get; set; } = "0";
    public int Decimals { get; set; }
    public decimal BalanceFormatted { get; set; }
    public string? LogoUrl { get; set; }
    public bool IsNative { get; set; }
}
