using System.Text.RegularExpressions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TrackFi.Domain.Entities;
using TrackFi.Domain.Enums;
using TrackFi.Domain.Interfaces;
using TrackFi.Infrastructure.Persistence;
using TrackFi.Infrastructure.Persistence.Repositories;

namespace TrackFi.Infrastructure.Blockchain;

/// <summary>
/// Service that orchestrates token verification using CoinMarketCap.
/// Automatically adds tokens to VerifiedToken or UnlistedToken based on CMC lookup.
/// </summary>
public class TokenVerificationService
{
    private readonly IVerifiedTokenRepository _verifiedTokenRepository;
    private readonly UnlistedTokenRepository _unlistedTokenRepository;
    private readonly CoinMarketCapService _coinMarketCapService;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<TokenVerificationService> _logger;

    public TokenVerificationService(
        IVerifiedTokenRepository verifiedTokenRepository,
        UnlistedTokenRepository unlistedTokenRepository,
        CoinMarketCapService coinMarketCapService,
        IServiceScopeFactory scopeFactory,
        ILogger<TokenVerificationService> logger)
    {
        _verifiedTokenRepository = verifiedTokenRepository ?? throw new ArgumentNullException(nameof(verifiedTokenRepository));
        _unlistedTokenRepository = unlistedTokenRepository ?? throw new ArgumentNullException(nameof(unlistedTokenRepository));
        _coinMarketCapService = coinMarketCapService ?? throw new ArgumentNullException(nameof(coinMarketCapService));
        _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Verifies a batch of tokens using CoinMarketCap.
    /// Returns a dictionary indicating which tokens were verified.
    /// </summary>
    /// <param name="tokens">List of tokens to verify (symbol, address, network)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Dictionary mapping contract address to verification status (true = verified, false = unlisted)</returns>
    public async Task<Dictionary<string, bool>> VerifyTokensAsync(
        List<TokenToVerify> tokens,
        CancellationToken cancellationToken = default)
    {
        if (tokens == null || tokens.Count == 0)
        {
            return [];
        }

        var result = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
        var tokensByNetwork = tokens.GroupBy(t => t.Network).ToList();

        foreach (var networkGroup in tokensByNetwork)
        {
            await ProcessNetworkTokensAsync(networkGroup.Key, networkGroup.ToList(), result, cancellationToken);
        }

        return result;
    }

    /// <summary>
    /// Processes all tokens for a single network.
    /// </summary>
    private async Task ProcessNetworkTokensAsync(
        BlockchainNetwork network,
        List<TokenToVerify> networkTokens,
        Dictionary<string, bool> result,
        CancellationToken cancellationToken)
    {
        var verifiedTokens = await _verifiedTokenRepository.GetVerifiedTokensAsync(network, cancellationToken);
        var unlistedTokens = await _unlistedTokenRepository.GetUnlistedTokensAsync(network, cancellationToken);

        var tokensToCheck = ClassifyTokensFromCache(networkTokens, verifiedTokens, unlistedTokens, result);

        if (tokensToCheck.Count == 0)
        {
            return;
        }

        _logger.LogInformation(
            "Verifying {Count} tokens on {Network} via CoinMarketCap",
            tokensToCheck.Count,
            network);

        var (tokensWithValidSymbols, tokensWithInvalidSymbols) = FilterTokensBySymbolValidity(tokensToCheck);

        await MarkInvalidTokensAsUnlistedAsync(tokensWithInvalidSymbols, network, result, cancellationToken);

        if (tokensWithValidSymbols.Count > 0)
        {
            await VerifyTokensWithCoinMarketCapAsync(tokensWithValidSymbols, result, cancellationToken);
        }
    }

    /// <summary>
    /// Separates tokens into already verified, already unlisted, and tokens that need verification.
    /// </summary>
    private static List<TokenToVerify> ClassifyTokensFromCache(
        List<TokenToVerify> tokens,
        Dictionary<string, VerifiedTokenCacheEntry> verifiedTokens,
        Dictionary<string, UnlistedToken> unlistedTokens,
        Dictionary<string, bool> result)
    {
        var tokensToCheck = new List<TokenToVerify>();

        foreach (var token in tokens)
        {
            var addressKey = token.ContractAddress.ToLowerInvariant();

            if (verifiedTokens.ContainsKey(addressKey))
            {
                result[addressKey] = true;
            }
            else if (unlistedTokens.ContainsKey(addressKey))
            {
                result[addressKey] = false;
            }
            else
            {
                tokensToCheck.Add(token);
            }
        }

        return tokensToCheck;
    }

    /// <summary>
    /// Separates tokens into those with valid and invalid symbols.
    /// </summary>
    private (List<TokenToVerify> valid, List<TokenToVerify> invalid) FilterTokensBySymbolValidity(
        List<TokenToVerify> tokens)
    {
        var valid = new List<TokenToVerify>();
        var invalid = new List<TokenToVerify>();

        foreach (var token in tokens)
        {
            if (IsValidTokenSymbol(token.Symbol))
            {
                valid.Add(token);
            }
            else
            {
                invalid.Add(token);
                _logger.LogWarning(
                    "Token {Symbol} ({Address}) has invalid symbol format - auto-marked as unlisted (no CMC call)",
                    token.Symbol,
                    token.ContractAddress);
            }
        }

        return (valid, invalid);
    }

    /// <summary>
    /// Marks tokens with invalid symbols as unlisted without making CMC API calls.
    /// </summary>
    private async Task MarkInvalidTokensAsUnlistedAsync(
        List<TokenToVerify> tokens,
        BlockchainNetwork network,
        Dictionary<string, bool> result,
        CancellationToken cancellationToken)
    {
        if (tokens.Count == 0)
        {
            return;
        }

        foreach (var token in tokens)
        {
            var addressKey = token.ContractAddress.ToLowerInvariant();
            await AddUnlistedTokenAsync(token, cancellationToken, reason: "Invalid symbol format");
            result[addressKey] = false;
        }

        _logger.LogInformation(
            "Filtered {Count} tokens with invalid symbols on {Network} (saved CMC API calls)",
            tokens.Count,
            network);
    }

    /// <summary>
    /// Verifies tokens with CoinMarketCap API and updates the result dictionary.
    /// </summary>
    private async Task VerifyTokensWithCoinMarketCapAsync(
        List<TokenToVerify> tokens,
        Dictionary<string, bool> result,
        CancellationToken cancellationToken)
    {
        var symbols = tokens.Select(t => t.Symbol).Distinct().ToList();
        var cmcResults = await _coinMarketCapService.GetCryptocurrenciesBySymbolsAsync(symbols, cancellationToken);

        foreach (var token in tokens)
        {
            var symbolKey = token.Symbol.ToUpperInvariant();
            var addressKey = token.ContractAddress.ToLowerInvariant();

            if (cmcResults.TryGetValue(symbolKey, out var cmcData) && cmcData.IsActive == 1)
            {
                await AddVerifiedTokenAsync(token, cmcData, cancellationToken);
                result[addressKey] = true;

                _logger.LogInformation(
                    "Token {Symbol} verified via CoinMarketCap (CMC ID: {CmcId})",
                    token.Symbol,
                    cmcData.Id);
            }
            else
            {
                await AddUnlistedTokenAsync(token, cancellationToken);
                result[addressKey] = false;

                _logger.LogWarning(
                    "Token {Symbol} ({Address}) not found in CoinMarketCap - added to unlisted",
                    token.Symbol,
                    token.ContractAddress);
            }
        }
    }

    /// <summary>
    /// Verifies a batch of tokens using CoinMarketCap.
    /// Convenience method for verifying multiple tokens with basic info.
    /// </summary>
    /// <param name="tokensToVerify">List of (contractAddress, symbol, network) tuples</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Dictionary mapping contract address to verification status</returns>
    public async Task<Dictionary<string, bool>> VerifyTokensAsync(
        List<(string contractAddress, string symbol, BlockchainNetwork network)> tokensToVerify,
        CancellationToken cancellationToken = default)
    {
        var tokens = tokensToVerify
            .Select(t => new TokenToVerify(t.contractAddress, t.symbol, t.network))
            .ToList();

        return await VerifyTokensAsync(tokens, cancellationToken);
    }

    private async Task AddVerifiedTokenAsync(
        TokenToVerify token,
        CmcCryptocurrency cmcData,
        CancellationToken cancellationToken)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<TrackFiDbContext>();

        var verifiedToken = new VerifiedToken(
            contractAddress: token.ContractAddress,
            network: token.Network,
            symbol: cmcData.Symbol,
            name: cmcData.Name,
            decimals: token.Decimals,
            verifiedBy: "coinmarketcap"
        );

        await context.VerifiedTokens.AddAsync(verifiedToken, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);

        // Invalidate cache
        await _verifiedTokenRepository.InvalidateCacheAsync(token.Network);
    }

    private async Task AddUnlistedTokenAsync(
        TokenToVerify token,
        CancellationToken cancellationToken,
        string? reason = null)
    {
        var unlistedToken = new UnlistedToken(
            contractAddress: token.ContractAddress,
            network: token.Network,
            symbol: token.Symbol,
            name: token.Name
        );

        await _unlistedTokenRepository.AddUnlistedTokenAsync(unlistedToken, cancellationToken);
    }

    /// <summary>
    /// Validates if a token symbol contains only legitimate characters.
    /// Legitimate tokens use alphanumeric characters with optional hyphens or periods.
    /// </summary>
    /// <param name="symbol">Token symbol to validate</param>
    /// <returns>True if symbol is valid, false if it contains special characters or invalid patterns</returns>
    private static bool IsValidTokenSymbol(string symbol)
    {
        if (string.IsNullOrWhiteSpace(symbol))
            return false;

        //TODO: https://learn.microsoft.com/en-us/dotnet/fundamentals/syslib-diagnostics/syslib1040-1049
        // Length check: Most legitimate tokens are 2-10 characters
        if (symbol.Length < 2 || symbol.Length > 10)
            return false;

        // Pattern check: Only alphanumeric, hyphen (-), and period (.)
        // Examples: BTC, ETH, USDC, AAVE, USD.e, AAVE-V2
        if (!Regex.IsMatch(symbol, @"^[A-Za-z0-9\-\.]+$"))
            return false;

        // Must contain at least one letter (prevents pure numbers like "123")
        if (!Regex.IsMatch(symbol, @"[A-Za-z]"))
            return false;

        // Reject if contains only special characters after letters (edge case)
        if (Regex.IsMatch(symbol, @"^[^A-Za-z0-9]+$"))
            return false;

        return true;
    }
}

/// <summary>
/// Represents a token that needs verification.
/// </summary>
public class TokenToVerify(
    string contractAddress,
    string symbol,
    BlockchainNetwork network,
    string? name = null,
    int decimals = 18)
{
    public string ContractAddress { get; } = contractAddress;
    public string Symbol { get; } = symbol;
    public string? Name { get; } = name;
    public BlockchainNetwork Network { get; } = network;
    public int Decimals { get; } = decimals;
}
