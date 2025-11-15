using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using TrackFi.Domain.Enums;
using TrackFi.Domain.Interfaces;

namespace TrackFi.Infrastructure.DeFi;

/// <summary>
/// Moralis DeFi data provider implementation.
/// Fetches DeFi positions using Moralis Web3 Data API.
/// </summary>
public class MoralisService : IDeFiDataProvider
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<MoralisService> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public MoralisService(
        HttpClient httpClient,
        ILogger<MoralisService> logger,
        string apiKey,
        string? baseUrl = null)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        if (string.IsNullOrWhiteSpace(apiKey))
            throw new ArgumentException("Moralis API key is required", nameof(apiKey));

        _httpClient.BaseAddress = new Uri(baseUrl ?? "https://deep-index.moralis.io/api/v2.2/");
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        _httpClient.DefaultRequestHeaders.Add("X-API-Key", apiKey);

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
    }

    /// <inheritdoc />
    public async Task<List<DeFiPositionData>> GetPositionsAsync(
        string walletAddress,
        BlockchainNetwork network,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(walletAddress))
            throw new ArgumentException("Wallet address is required", nameof(walletAddress));

        // Check if network is supported
        if (!IsNetworkSupported(network))
        {
            _logger.LogWarning(
                "Network {Network} is not supported by Moralis. Returning empty positions.",
                network);
            return [];
        }

        var chain = MapNetworkToChain(network);
        var endpoint = $"wallets/{walletAddress}/defi/positions?chain={chain}";

        _logger.LogInformation(
            "Fetching DeFi positions from Moralis for {Wallet} on {Network}",
            walletAddress,
            network);

        try
        {
            var response = await _httpClient.GetAsync(endpoint, cancellationToken);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            var moralisPositions = JsonSerializer.Deserialize<MoralisPositionsResponse>(content, _jsonOptions)
                ?? [];

            _logger.LogInformation(
                "Found {Count} positions from Moralis for {Wallet} on {Network}",
                moralisPositions.Count,
                walletAddress,
                network);

            var positions = moralisPositions.Select(MapToPosition).ToList();

            // Aggregate Moralis-specific positions (lending by protocol_id, etc.)
            return AggregateMoralisPositions(positions);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error fetching DeFi positions from Moralis for {Wallet}", walletAddress);
            throw new InvalidOperationException($"Failed to fetch DeFi positions from Moralis: {ex.Message}", ex);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to parse Moralis response for {Wallet}", walletAddress);
            throw new InvalidOperationException($"Failed to parse Moralis response: {ex.Message}", ex);
        }
    }

    /// <inheritdoc />
    public async Task<Dictionary<BlockchainNetwork, List<DeFiPositionData>>> GetMultiNetworkPositionsAsync(
        string walletAddress,
        IEnumerable<BlockchainNetwork> networks,
        CancellationToken cancellationToken = default)
    {
        var networkList = networks?.ToList() ?? throw new ArgumentNullException(nameof(networks));

        _logger.LogInformation(
            "Fetching DeFi positions from Moralis for {Wallet} across {Count} networks",
            walletAddress,
            networkList.Count);

        var tasks = networkList.Select(async network =>
        {
            var positions = await GetPositionsAsync(walletAddress, network, cancellationToken);
            return new KeyValuePair<BlockchainNetwork, List<DeFiPositionData>>(network, positions);
        });

        var results = await Task.WhenAll(tasks);
        return results.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
    }

    /// <summary>
    /// Aggregates Moralis positions by protocol (Moralis-specific).
    /// Unlike Zerion's group_id approach, Moralis positions are aggregated by protocol_id.
    /// </summary>
    private List<DeFiPositionData> AggregateMoralisPositions(List<DeFiPositionData> positions)
    {
        var result = new List<DeFiPositionData>();

        // Group positions by protocol_id and label to identify lending positions
        var lendingPositions = positions
            .Where(p => p.PositionType == DeFiPositionDataType.Supplied ||
                       p.PositionType == DeFiPositionDataType.Borrowed)
            .ToList();

        var otherPositions = positions
            .Where(p => p.PositionType != DeFiPositionDataType.Supplied &&
                       p.PositionType != DeFiPositionDataType.Borrowed)
            .ToList();

        // Aggregate lending positions by protocol_id
        if (lendingPositions.Count != 0)
        {
            var aggregated = AggregateLendingByProtocol(lendingPositions);
            result.AddRange(aggregated);
        }

        // Add other positions as-is (staking, liquidity, etc.)
        result.AddRange(otherPositions);

        return result;
    }

    /// <summary>
    /// Aggregates lending positions by protocol_id (Moralis-specific).
    /// Combines supplied and borrowed positions from the same protocol.
    /// </summary>
    private List<DeFiPositionData> AggregateLendingByProtocol(List<DeFiPositionData> positions)
    {
        return positions
            .GroupBy(p => p.ProtocolId)
            .Select(group =>
            {
                // Get all supplied and borrowed positions for this protocol
                var suppliedPositions = group.Where(p => p.PositionType == DeFiPositionDataType.Supplied).ToList();
                var borrowedPositions = group.Where(p => p.PositionType == DeFiPositionDataType.Borrowed).ToList();

                if (suppliedPositions.Count == 0 && borrowedPositions.Count == 0)
                {
                    _logger.LogWarning("Lending protocol {ProtocolId} has no positions, skipping", group.Key);
                    return null;
                }

                var firstPosition = suppliedPositions.Count != 0 ? suppliedPositions.First() : borrowedPositions.First();

                _logger.LogDebug(
                    "Aggregating lending protocol {ProtocolId}: Protocol={Protocol}, TokenCount={TokenCount}",
                    group.Key,
                    firstPosition.ProtocolName,
                    suppliedPositions.Sum(p => p.Tokens.Count) + borrowedPositions.Sum(p => p.Tokens.Count));

                // Create aggregated lending position
                // Note: USD values will be calculated after price enrichment
                return new DeFiPositionData
                {
                    Id = firstPosition.Id,
                    ProtocolName = firstPosition.ProtocolName,
                    ProtocolId = firstPosition.ProtocolId,
                    ProtocolUrl = firstPosition.ProtocolUrl,
                    ProtocolLogo = firstPosition.ProtocolLogo,
                    PositionType = DeFiPositionDataType.Supplied, // Mark as lending
                    Label = "Lending",
                    TotalValueUsd = 0, // Calculated after price enrichment
                    UnclaimedValueUsd = null, // Calculated after price enrichment
                    Apy = firstPosition.Apy,
                    // Combine all tokens from supplied and borrowed
                    Tokens = suppliedPositions.SelectMany(p => p.Tokens)
                        .Concat(borrowedPositions.SelectMany(p => p.Tokens))
                        .ToList(),
                    Details = new DeFiPositionDetails
                    {
                        SuppliedValueUsd = null, // Calculated after price enrichment
                        BorrowedValueUsd = null, // Calculated after price enrichment
                        NetValueUsd = null, // Calculated after price enrichment
                        Market = firstPosition.Details?.Market,
                        IsDebt = borrowedPositions.Count != 0
                    },
                    AccountData = firstPosition.AccountData,
                    ProjectedEarnings = CombineProjectedEarnings(
                        suppliedPositions.Concat(borrowedPositions).ToList())
                };
            })
            .Where(p => p != null)
            .Cast<DeFiPositionData>()
            .ToList();
    }

    /// <summary>
    /// Combines projected earnings from multiple positions.
    /// </summary>
    private static ProjectedEarnings? CombineProjectedEarnings(List<DeFiPositionData> positions)
    {
        var allEarnings = positions
            .Select(p => p.ProjectedEarnings)
            .Where(e => e != null)
            .ToList();

        if (allEarnings.Count == 0)
            return null;

        return new ProjectedEarnings
        {
            Daily = allEarnings.Sum(e => e!.Daily ?? 0),
            Weekly = allEarnings.Sum(e => e!.Weekly ?? 0),
            Monthly = allEarnings.Sum(e => e!.Monthly ?? 0),
            Yearly = allEarnings.Sum(e => e!.Yearly ?? 0)
        };
    }

    /// <summary>
    /// Maps Moralis position to domain model.
    /// </summary>
    private DeFiPositionData MapToPosition(MoralisPosition moralisPosition)
    {
        var positionType = MapLabelToPositionType(moralisPosition.Position.Label);

        // Generate unique ID from protocol, address, and label
        var id = $"{moralisPosition.ProtocolId}-{moralisPosition.Position.Address ?? "unknown"}-{moralisPosition.Position.Label}";

        return new DeFiPositionData
        {
            Id = id,
            ProtocolName = moralisPosition.ProtocolName,
            ProtocolId = moralisPosition.ProtocolId,
            ProtocolUrl = moralisPosition.ProtocolUrl,
            ProtocolLogo = moralisPosition.ProtocolLogo,
            PositionType = positionType,
            Label = moralisPosition.Position.Label,
            TotalValueUsd = 0, // Calculated after price enrichment
            UnclaimedValueUsd = null, // Calculated after price enrichment
            Apy = moralisPosition.Position.PositionDetails?.Apy,
            Tokens = [.. moralisPosition.Position.Tokens.Select(MapToToken)],
            Details = MapToPositionDetails(moralisPosition.Position.PositionDetails),
            AccountData = MapToAccountData(moralisPosition.AccountData),
            ProjectedEarnings = MapToProjectedEarnings(moralisPosition.TotalProjectedEarnings)
        };
    }

    /// <summary>
    /// Maps Moralis token to domain model.
    /// Note: UsdPrice and UsdValue are set to null - prices will be enriched from Alchemy.
    /// </summary>
    private DeFiToken MapToToken(MoralisToken moralisToken)
    {
        return new DeFiToken
        {
            Name = moralisToken.Name,
            Symbol = moralisToken.Symbol,
            ContractAddress = moralisToken.ContractAddress,
            Decimals = int.TryParse(moralisToken.Decimals, out var decimals) ? decimals : 0,
            TokenType = MapTokenType(moralisToken.TokenType),
            Balance = decimal.TryParse(moralisToken.BalanceFormatted, out var balance) ? balance : 0, // Use formatted balance
            BalanceFormatted = moralisToken.BalanceFormatted,
            UsdPrice = null, // Prices enriched from Alchemy later
            UsdValue = null, // Calculated after price enrichment
            Logo = moralisToken.Logo ?? moralisToken.Thumbnail
        };
    }

    /// <summary>
    /// Maps Moralis position details to domain model.
    /// </summary>
    private DeFiPositionDetails? MapToPositionDetails(MoralisPositionDetails? details)
    {
        if (details == null) return null;

        return new DeFiPositionDetails
        {
            Market = details.Market,
            IsDebt = details.IsDebt,
            IsVariableDebt = details.IsVariableDebt,
            IsStableDebt = details.IsStableDebt,
            Apy = details.Apy,
            IsEnabledAsCollateral = details.IsEnabledAsCollateral,
            ProjectedEarnings = MapToProjectedEarnings(details.ProjectedEarnings)
        };
    }

    /// <summary>
    /// Maps Moralis account data to domain model.
    /// </summary>
    private static DeFiAccountData? MapToAccountData(MoralisAccountData? accountData)
    {
        if (accountData == null) return null;

        return new DeFiAccountData
        {
            NetApy = accountData.NetApy,
            HealthFactor = accountData.HealthFactor
        };
    }

    /// <summary>
    /// Maps Moralis projected earnings to domain model.
    /// </summary>
    private static ProjectedEarnings? MapToProjectedEarnings(MoralisProjectedEarnings? earnings)
    {
        if (earnings == null) return null;

        return new ProjectedEarnings
        {
            Daily = earnings.Daily,
            Weekly = earnings.Weekly,
            Monthly = earnings.Monthly,
            Yearly = earnings.Yearly
        };
    }

    /// <summary>
    /// Maps position label to position type enum.
    /// </summary>
    private static DeFiPositionDataType MapLabelToPositionType(string label)
    {
        return label?.ToLowerInvariant() switch
        {
            "supplied" => DeFiPositionDataType.Supplied,
            "borrowed" => DeFiPositionDataType.Borrowed,
            "liquidity" => DeFiPositionDataType.Liquidity,
            "staked" => DeFiPositionDataType.Staked,
            "farming" => DeFiPositionDataType.Farming,
            "vested" => DeFiPositionDataType.Vested,
            "locked" => DeFiPositionDataType.Locked,
            _ => DeFiPositionDataType.Other
        };
    }

    /// <summary>
    /// Maps Moralis token type to domain enum.
    /// </summary>
    private static DeFiTokenType MapTokenType(string tokenType)
    {
        return tokenType?.ToLowerInvariant() switch
        {
            "supplied" => DeFiTokenType.Supplied,
            "borrowed" => DeFiTokenType.Borrowed,
            "reward" => DeFiTokenType.Reward,
            "defi-token" => DeFiTokenType.DeFiToken,
            _ => DeFiTokenType.Underlying
        };
    }

    /// <summary>
    /// Checks if a network is supported by Moralis.
    /// </summary>
    private static bool IsNetworkSupported(BlockchainNetwork network)
    {
        return network switch
        {
            BlockchainNetwork.Ethereum => true,
            BlockchainNetwork.Polygon => true,
            BlockchainNetwork.Arbitrum => true,
            BlockchainNetwork.Base => true,
            BlockchainNetwork.Unichain => false,
            _ => false
        };
    }

    /// <summary>
    /// Maps TrackFi BlockchainNetwork to Moralis chain identifier.
    /// </summary>
    private static string MapNetworkToChain(BlockchainNetwork network)
    {
        return network switch
        {
            BlockchainNetwork.Ethereum => "eth",
            BlockchainNetwork.Polygon => "polygon",
            BlockchainNetwork.Arbitrum => "arbitrum",
            BlockchainNetwork.Base => "base",
            _ => throw new NotSupportedException($"Network {network} is not supported by Moralis")
        };
    }
}
