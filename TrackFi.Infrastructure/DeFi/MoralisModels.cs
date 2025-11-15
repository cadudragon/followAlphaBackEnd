using System.Text.Json.Serialization;

namespace TrackFi.Infrastructure.DeFi;

/// <summary>
/// Root response from Moralis DeFi Positions API.
/// </summary>
public class MoralisPositionsResponse : List<MoralisPosition>
{
}

/// <summary>
/// Single DeFi position from Moralis API.
/// </summary>
public class MoralisPosition
{
    [JsonPropertyName("protocol_name")]
    public string ProtocolName { get; set; } = string.Empty;

    [JsonPropertyName("protocol_id")]
    public string ProtocolId { get; set; } = string.Empty;

    [JsonPropertyName("protocol_url")]
    public string? ProtocolUrl { get; set; }

    [JsonPropertyName("protocol_logo")]
    public string? ProtocolLogo { get; set; }

    [JsonPropertyName("account_data")]
    public MoralisAccountData? AccountData { get; set; }

    [JsonPropertyName("total_projected_earnings_usd")]
    public MoralisProjectedEarnings? TotalProjectedEarnings { get; set; }

    [JsonPropertyName("position")]
    public MoralisPositionData Position { get; set; } = new();
}

/// <summary>
/// Account-level data (health factor, net APY, etc.).
/// </summary>
public class MoralisAccountData
{
    [JsonPropertyName("net_apy")]
    public decimal? NetApy { get; set; }

    [JsonPropertyName("health_factor")]
    public decimal? HealthFactor { get; set; }
}

/// <summary>
/// Projected earnings in USD.
/// </summary>
public class MoralisProjectedEarnings
{
    [JsonPropertyName("daily")]
    public decimal? Daily { get; set; }

    [JsonPropertyName("weekly")]
    public decimal? Weekly { get; set; }

    [JsonPropertyName("monthly")]
    public decimal? Monthly { get; set; }

    [JsonPropertyName("yearly")]
    public decimal? Yearly { get; set; }
}

/// <summary>
/// Position data (tokens, balance, details).
/// </summary>
public class MoralisPositionData
{
    [JsonPropertyName("label")]
    public string Label { get; set; } = string.Empty;

    [JsonPropertyName("balance_usd")]
    public decimal? BalanceUsd { get; set; }

    [JsonPropertyName("total_unclaimed_usd_value")]
    public decimal? TotalUnclaimedUsdValue { get; set; }

    [JsonPropertyName("tokens")]
    public List<MoralisToken> Tokens { get; set; } = [];

    [JsonPropertyName("address")]
    public string? Address { get; set; }

    [JsonPropertyName("position_details")]
    public MoralisPositionDetails? PositionDetails { get; set; }
}

/// <summary>
/// Token within a position.
/// </summary>
public class MoralisToken
{
    [JsonPropertyName("token_type")]
    public string TokenType { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("symbol")]
    public string Symbol { get; set; } = string.Empty;

    [JsonPropertyName("contract_address")]
    public string ContractAddress { get; set; } = string.Empty;

    [JsonPropertyName("decimals")]
    public string Decimals { get; set; } = string.Empty;

    [JsonPropertyName("logo")]
    public string? Logo { get; set; }

    [JsonPropertyName("thumbnail")]
    public string? Thumbnail { get; set; }

    [JsonPropertyName("balance")]
    public string Balance { get; set; } = string.Empty;

    [JsonPropertyName("balance_formatted")]
    public string BalanceFormatted { get; set; } = string.Empty;

    [JsonPropertyName("usd_price")]
    public decimal? UsdPrice { get; set; }

    [JsonPropertyName("usd_value")]
    public decimal? UsdValue { get; set; }
}

/// <summary>
/// Position-specific details (APY, debt status, etc.).
/// </summary>
public class MoralisPositionDetails
{
    [JsonPropertyName("market")]
    public string? Market { get; set; }

    [JsonPropertyName("is_debt")]
    public bool IsDebt { get; set; }

    [JsonPropertyName("is_variable_debt")]
    public bool IsVariableDebt { get; set; }

    [JsonPropertyName("is_stable_debt")]
    public bool IsStableDebt { get; set; }

    [JsonPropertyName("apy")]
    public decimal? Apy { get; set; }

    [JsonPropertyName("projected_earnings_usd")]
    public MoralisProjectedEarnings? ProjectedEarnings { get; set; }

    [JsonPropertyName("is_enabled_as_collateral")]
    public bool? IsEnabledAsCollateral { get; set; }
}
