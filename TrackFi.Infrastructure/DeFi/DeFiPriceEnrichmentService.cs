using Microsoft.Extensions.Logging;
using TrackFi.Domain.Enums;
using TrackFi.Domain.Interfaces;
using TrackFi.Infrastructure.Blockchain;
using TrackFi.Infrastructure.Persistence.Repositories;

namespace TrackFi.Infrastructure.DeFi;

/// <summary>
/// Service for enriching DeFi positions with prices from Alchemy.
/// Separates pricing (1-min cache) from position data (3-min cache).
/// Includes token verification layer - verified tokens use Alchemy pricing, unverified use provider pricing.
/// </summary>
public class DeFiPriceEnrichmentService(
    AlchemyService? alchemyService,
    TokenVerificationService tokenVerificationService,
    IVerifiedTokenRepository verifiedTokenRepository,
    UnlistedTokenRepository unlistedTokenRepository,
    ILogger<DeFiPriceEnrichmentService> logger)
{
    private readonly AlchemyService? _alchemyService = alchemyService;
    private readonly TokenVerificationService _tokenVerificationService = tokenVerificationService ?? throw new ArgumentNullException(nameof(tokenVerificationService));
    private readonly IVerifiedTokenRepository _verifiedTokenRepository = verifiedTokenRepository ?? throw new ArgumentNullException(nameof(verifiedTokenRepository));
    private readonly UnlistedTokenRepository _unlistedTokenRepository = unlistedTokenRepository ?? throw new ArgumentNullException(nameof(unlistedTokenRepository));
    private readonly ILogger<DeFiPriceEnrichmentService> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    /// <summary>
    /// Enriches DeFi positions with current prices from Alchemy and verification metadata.
    /// Verified tokens use Alchemy pricing (global layer), unverified tokens use provider's original pricing (fallback).
    /// Updates token prices, USD values, position totals, and verification flags.
    /// </summary>
    public virtual async Task<List<DeFiPositionData>> EnrichWithPricesAsync(
        List<DeFiPositionData> positions,
        BlockchainNetwork network,
        CancellationToken cancellationToken = default)
    {
        if (_alchemyService == null)
            throw new InvalidOperationException("AlchemyService is required for price enrichment");

        if (positions.Count == 0)
            return positions;

        var allTokens = CollectUniqueTokens(positions);

        if (allTokens.Count == 0)
        {
            _logger.LogWarning("No tokens found to enrich for network {Network}", network);
            return positions;
        }

        _logger.LogInformation(
            "Processing {TokenCount} unique tokens for network {Network}",
            allTokens.Count,
            network);

        var verificationResults = await ClassifyTokensAsync(allTokens, network, cancellationToken);
        var pricesByAddress = await FetchPricesForVerifiedTokensAsync(allTokens, verificationResults, network, cancellationToken);
        var enrichedPositions = EnrichPositions(positions, verificationResults, pricesByAddress);

        _logger.LogInformation(
            "Enrichment complete for {Network}: {TotalPositions} positions, {DisconnectedCount} disconnected from global pricing",
            network,
            enrichedPositions.Count,
            enrichedPositions.Count(p => p.IsDisconnectedFromGlobalPricing));

        return enrichedPositions;
    }

    /// <summary>
    /// Collects all unique tokens from positions.
    /// </summary>
    private static List<DeFiToken> CollectUniqueTokens(List<DeFiPositionData> positions)
    {
        return positions
            .SelectMany(p => p.Tokens)
            .Where(t => !string.IsNullOrEmpty(t.ContractAddress))
            .GroupBy(t => t.ContractAddress.ToLowerInvariant())
            .Select(g => g.First())
            .ToList();
    }

    /// <summary>
    /// Fetches Alchemy prices for verified tokens only.
    /// </summary>
    private async Task<Dictionary<string, decimal>> FetchPricesForVerifiedTokensAsync(
        List<DeFiToken> allTokens,
        Dictionary<string, TokenVerificationResult> verificationResults,
        BlockchainNetwork network,
        CancellationToken cancellationToken)
    {
        var verifiedTokens = allTokens
            .Where(t => verificationResults.TryGetValue(t.ContractAddress.ToLowerInvariant(), out var result) && result.IsVerified)
            .ToList();

        var unverifiedCount = allTokens.Count - verifiedTokens.Count;

        _logger.LogInformation(
            "Token classification for {Network}: {VerifiedCount} verified (will use Alchemy price), {UnverifiedCount} unverified (will use provider price)",
            network,
            verifiedTokens.Count,
            unverifiedCount);

        if (verifiedTokens.Count == 0)
        {
            return new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
        }

        var tokenAddresses = verifiedTokens
            .Select(t => (address: t.ContractAddress, symbol: t.Symbol, network))
            .ToList();

        var (prices, failures) = await _alchemyService!.GetTokenPricesByAddressesAsync(tokenAddresses, cancellationToken);

        if (failures.Count > 0)
        {
            _logger.LogError(
                "PRICE FETCH FAILED for {FailedCount}/{TotalCount} verified tokens on {Network}:\n{FailedTokens}",
                failures.Count,
                verifiedTokens.Count,
                network,
                string.Join("\n", failures.Select(f => $"  - {f.Symbol} ({f.Address}): {f.Error}")));
        }

        return prices;
    }

    /// <summary>
    /// Enriches all positions with verification flags and prices.
    /// </summary>
    private List<DeFiPositionData> EnrichPositions(
        List<DeFiPositionData> positions,
        Dictionary<string, TokenVerificationResult> verificationResults,
        Dictionary<string, decimal> pricesByAddress)
    {
        var enrichedPositions = new List<DeFiPositionData>();

        foreach (var position in positions)
        {
            var (enrichedTokens, hasUnverifiedTokens) = EnrichPositionTokens(
                position.Tokens,
                verificationResults,
                pricesByAddress);

            var enrichedPosition = BuildEnrichedPosition(position, enrichedTokens, hasUnverifiedTokens);
            enrichedPositions.Add(enrichedPosition);
        }

        return enrichedPositions;
    }

    /// <summary>
    /// Enriches all tokens within a position.
    /// </summary>
    private static (List<DeFiToken> enrichedTokens, bool hasUnverifiedTokens) EnrichPositionTokens(
        List<DeFiToken> tokens,
        Dictionary<string, TokenVerificationResult> verificationResults,
        Dictionary<string, decimal> pricesByAddress)
    {
        var enrichedTokens = new List<DeFiToken>();
        bool hasUnverifiedTokens = false;

        foreach (var token in tokens)
        {
            var enrichedToken = EnrichToken(token, verificationResults, pricesByAddress, ref hasUnverifiedTokens);
            enrichedTokens.Add(enrichedToken);
        }

        return (enrichedTokens, hasUnverifiedTokens);
    }

    /// <summary>
    /// Enriches a single token with verification metadata and pricing.
    /// </summary>
    private static DeFiToken EnrichToken(
        DeFiToken token,
        Dictionary<string, TokenVerificationResult> verificationResults,
        Dictionary<string, decimal> pricesByAddress,
        ref bool hasUnverifiedTokens)
    {
        var lowerAddress = token.ContractAddress.ToLowerInvariant();
        var verification = verificationResults.GetValueOrDefault(lowerAddress);

        bool isVerified = verification?.IsVerified ?? false;
        bool isUnlisted = verification?.IsUnlisted ?? false;

        if (!isVerified)
        {
            hasUnverifiedTokens = true;
        }

        var (usdPrice, usdValue, priceSource) = CalculateTokenPricing(
            token,
            isVerified,
            lowerAddress,
            pricesByAddress);

        return new DeFiToken
        {
            Name = token.Name,
            Symbol = token.Symbol,
            ContractAddress = token.ContractAddress,
            Decimals = token.Decimals,
            TokenType = token.TokenType,
            Balance = token.Balance,
            BalanceFormatted = token.BalanceFormatted,
            UsdPrice = usdPrice,
            UsdValue = usdValue,
            Logo = token.Logo,
            IsVerified = isVerified,
            IsUnlisted = isUnlisted,
            PriceSource = priceSource
        };
    }

    /// <summary>
    /// Calculates pricing for a token (price, value, source).
    /// </summary>
    private static (decimal? usdPrice, decimal? usdValue, string priceSource) CalculateTokenPricing(
        DeFiToken token,
        bool isVerified,
        string lowerAddress,
        Dictionary<string, decimal> pricesByAddress)
    {
        if (isVerified && pricesByAddress.TryGetValue(lowerAddress, out var alchemyPrice))
        {
            return (alchemyPrice, token.Balance * alchemyPrice, "Alchemy");
        }

        return (token.UsdPrice, token.UsdValue, "Zerion");
    }

    /// <summary>
    /// Builds an enriched position from original data and enriched tokens.
    /// </summary>
    private static DeFiPositionData BuildEnrichedPosition(
        DeFiPositionData position,
        List<DeFiToken> enrichedTokens,
        bool hasUnverifiedTokens)
    {
        var totalValueUsd = CalculateTotalValueUsd(position, enrichedTokens);
        var enrichedDetails = CalculateAggregatedValues(position, enrichedTokens);

        return new DeFiPositionData
        {
            Id = position.Id,
            ProtocolName = position.ProtocolName,
            ProtocolId = position.ProtocolId,
            ProtocolUrl = position.ProtocolUrl,
            ProtocolLogo = position.ProtocolLogo,
            ProtocolModule = position.ProtocolModule,
            PoolAddress = position.PoolAddress,
            GroupId = position.GroupId,
            Name = position.Name,
            PositionType = position.PositionType,
            Label = position.Label,
            TotalValueUsd = totalValueUsd,
            UnclaimedValueUsd = CalculateUnclaimedValue(position, enrichedTokens),
            Apy = position.Apy,
            Tokens = enrichedTokens,
            Details = enrichedDetails,
            AccountData = position.AccountData,
            ProjectedEarnings = position.ProjectedEarnings,
            HasUnverifiedTokens = hasUnverifiedTokens,
            IsDisconnectedFromGlobalPricing = hasUnverifiedTokens
        };
    }

    /// <summary>
    /// Classifies tokens as verified, unverified, or unlisted.
    /// Checks VerifiedTokenRepository, UnlistedTokenRepository, and verifies unknown tokens via CMC.
    /// </summary>
    private async Task<Dictionary<string, TokenVerificationResult>> ClassifyTokensAsync(
        List<DeFiToken> tokens,
        BlockchainNetwork network,
        CancellationToken cancellationToken)
    {
        // Step 1: Load verified tokens cache
        var verifiedTokens = await _verifiedTokenRepository.GetVerifiedTokensAsync(network, cancellationToken);

        // Step 2: Load unlisted (scam) tokens cache
        var unlistedTokens = await _unlistedTokenRepository.GetUnlistedTokensAsync(network, cancellationToken);

        // Step 3: Classify each token
        var results = new Dictionary<string, TokenVerificationResult>(StringComparer.OrdinalIgnoreCase);
        var unknownTokens = new List<(string Address, string Symbol, BlockchainNetwork Network)>();

        foreach (var token in tokens)
        {
            var lowerAddress = token.ContractAddress.ToLowerInvariant();

            if (verifiedTokens.ContainsKey(lowerAddress))
            {
                results[lowerAddress] = new TokenVerificationResult { IsVerified = true, IsUnlisted = false };
            }
            else if (unlistedTokens.ContainsKey(lowerAddress))
            {
                results[lowerAddress] = new TokenVerificationResult { IsVerified = false, IsUnlisted = true };
            }
            else
            {
                // Unknown - needs CMC verification
                unknownTokens.Add((token.ContractAddress, token.Symbol, network));
                results[lowerAddress] = new TokenVerificationResult { IsVerified = false, IsUnlisted = false };
            }
        }

        // Step 4: Verify unknown tokens via CMC
        if (unknownTokens.Count > 0)
        {
            _logger.LogInformation(
                "Verifying {UnknownCount} unknown tokens via CMC for {Network}",
                unknownTokens.Count,
                network);

            var verificationResults = await _tokenVerificationService.VerifyTokensAsync(unknownTokens, cancellationToken);

            foreach (var (address, isVerified) in verificationResults)
            {
                var lowerAddress = address.ToLowerInvariant();
                if (results.ContainsKey(lowerAddress))
                {
                    results[lowerAddress].IsVerified = isVerified;

                    if (isVerified)
                    {
                        _logger.LogInformation(
                            "Token {Address} verified via CMC on {Network}",
                            address,
                            network);
                    }
                }
            }
        }

        return results;
    }

    /// <summary>
    /// Result of token verification classification.
    /// </summary>
    private class TokenVerificationResult
    {
        public bool IsVerified { get; set; }
        public bool IsUnlisted { get; set; }
    }

    /// <summary>
    /// Calculates aggregated USD values for farming/lending positions.
    /// </summary>
    private static DeFiPositionDetails? CalculateAggregatedValues(
        DeFiPositionData position,
        List<DeFiToken> enrichedTokens)
    {
        if (position.Details == null)
            return null;

        // For farming positions: calculate staked and rewards values
        if (position.PositionType == DeFiPositionDataType.Farming &&
            position.Details.StakedCount.HasValue &&
            position.Details.RewardsCount.HasValue)
        {
            var stakedCount = position.Details.StakedCount.Value;
            var rewardsCount = position.Details.RewardsCount.Value;

            var stakedTokens = enrichedTokens.Take(stakedCount).ToList();
            var rewardTokens = enrichedTokens.Skip(stakedCount).Take(rewardsCount).ToList();

            return new DeFiPositionDetails
            {
                StakedCount = stakedCount,
                RewardsCount = rewardsCount,
                StakedValueUsd = stakedTokens.Sum(t => t.UsdValue ?? 0),
                RewardsValueUsd = rewardTokens.Sum(t => t.UsdValue ?? 0)
            };
        }

        // For lending positions: calculate supplied, borrowed, and net values
        if (position.Label == "Lending")
        {
            var suppliedValue = enrichedTokens
                .Where(t => t.TokenType == DeFiTokenType.Supplied)
                .Sum(t => t.UsdValue ?? 0);

            var borrowedValue = enrichedTokens
                .Where(t => t.TokenType == DeFiTokenType.Borrowed)
                .Sum(t => t.UsdValue ?? 0);

            return new DeFiPositionDetails
            {
                Market = position.Details.Market,
                IsDebt = position.Details.IsDebt,
                IsVariableDebt = position.Details.IsVariableDebt,
                IsStableDebt = position.Details.IsStableDebt,
                Apy = position.Details.Apy,
                IsEnabledAsCollateral = position.Details.IsEnabledAsCollateral,
                ProjectedEarnings = position.Details.ProjectedEarnings,
                SuppliedValueUsd = suppliedValue,
                BorrowedValueUsd = borrowedValue,
                NetValueUsd = suppliedValue - borrowedValue
            };
        }

        return position.Details;
    }

    /// <summary>
    /// Calculates total position value in USD.
    /// For lending positions, borrowed tokens are subtracted (debt).
    /// For all other positions, all tokens are summed.
    /// </summary>
    private static decimal CalculateTotalValueUsd(
        DeFiPositionData position,
        List<DeFiToken> enrichedTokens)
    {
        // For lending positions, borrowed tokens are debt and should be subtracted
        if (position.Label == "Lending")
        {
            var suppliedValue = enrichedTokens
                .Where(t => t.TokenType == DeFiTokenType.Supplied || t.TokenType == DeFiTokenType.DeFiToken)
                .Sum(t => t.UsdValue ?? 0);

            var borrowedValue = enrichedTokens
                .Where(t => t.TokenType == DeFiTokenType.Borrowed)
                .Sum(t => t.UsdValue ?? 0);

            return suppliedValue - borrowedValue;
        }

        // For all other position types, sum all tokens
        return enrichedTokens.Sum(t => t.UsdValue ?? 0);
    }

    /// <summary>
    /// Calculates unclaimed rewards value from reward tokens.
    /// </summary>
    private static decimal? CalculateUnclaimedValue(
        DeFiPositionData position,
        List<DeFiToken> enrichedTokens)
    {
        // For farming positions, unclaimed value is rewards
        if (position.PositionType == DeFiPositionDataType.Farming &&
            position.Details?.RewardsCount.HasValue == true &&
            position.Details?.StakedCount.HasValue == true)
        {
            var stakedCount = position.Details.StakedCount.Value;
            var rewardsCount = position.Details.RewardsCount.Value;

            var rewardTokens = enrichedTokens.Skip(stakedCount).Take(rewardsCount).ToList();
            return rewardTokens.Sum(t => t.UsdValue ?? 0);
        }

        return null;
    }
}
