using System.Text.Json.Serialization;

namespace NetZerion.Models.JsonApi;

/// <summary>
/// Attributes for a Zerion position resource.
/// </summary>
public class ZerionPositionAttributes
{
    [JsonPropertyName("protocol")]
    public string? Protocol { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("position_type")]
    public string? PositionType { get; set; }

    [JsonPropertyName("protocol_module")]
    public string? ProtocolModule { get; set; }

    [JsonPropertyName("pool_address")]
    public string? PoolAddress { get; set; }

    [JsonPropertyName("group_id")]
    public string? GroupId { get; set; }

    [JsonPropertyName("quantity")]
    public ZerionQuantity? Quantity { get; set; }

    [JsonPropertyName("value")]
    public decimal? Value { get; set; }

    [JsonPropertyName("price")]
    public decimal? Price { get; set; }

    [JsonPropertyName("changes")]
    public ZerionChanges? Changes { get; set; }

    [JsonPropertyName("fungible_info")]
    public ZerionFungibleInfo? FungibleInfo { get; set; }

    [JsonPropertyName("application_metadata")]
    public ZerionApplicationMetadata? ApplicationMetadata { get; set; }
}

/// <summary>
/// Attributes for a Zerion fungible (token) resource.
/// </summary>
public class ZerionFungibleAttributes
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("symbol")]
    public string? Symbol { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("icon")]
    public ZerionIcon? Icon { get; set; }

    [JsonPropertyName("flags")]
    public ZerionFlags? Flags { get; set; }

    [JsonPropertyName("market_data")]
    public ZerionMarketData? MarketData { get; set; }

    [JsonPropertyName("implementations")]
    public List<ZerionImplementation>? Implementations { get; set; }

    [JsonPropertyName("quantity")]
    public ZerionQuantity? Quantity { get; set; }

    [JsonPropertyName("value")]
    public decimal? Value { get; set; }
}

/// <summary>
/// Quantity information.
/// </summary>
public class ZerionQuantity
{
    [JsonPropertyName("int")]
    public string? Int { get; set; }

    [JsonPropertyName("decimals")]
    public int Decimals { get; set; }

    [JsonPropertyName("float")]
    public decimal Float { get; set; }

    [JsonPropertyName("numeric")]
    public string? Numeric { get; set; }
}

/// <summary>
/// Value changes over time.
/// </summary>
public class ZerionChanges
{
    [JsonPropertyName("absolute_1d")]
    public decimal? Absolute1d { get; set; }

    [JsonPropertyName("percent_1d")]
    public decimal? Percent1d { get; set; }

    [JsonPropertyName("percent_7d")]
    public decimal? Percent7d { get; set; }

    [JsonPropertyName("percent_30d")]
    public decimal? Percent30d { get; set; }
}

/// <summary>
/// Fungible token information.
/// </summary>
public class ZerionFungibleInfo
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("symbol")]
    public string? Symbol { get; set; }

    [JsonPropertyName("icon")]
    public ZerionIcon? Icon { get; set; }

    [JsonPropertyName("flags")]
    public ZerionFlags? Flags { get; set; }

    [JsonPropertyName("implementations")]
    public List<ZerionImplementation>? Implementations { get; set; }
}

/// <summary>
/// Icon/image information.
/// </summary>
public class ZerionIcon
{
    [JsonPropertyName("url")]
    public string? Url { get; set; }
}

/// <summary>
/// Resource flags.
/// </summary>
public class ZerionFlags
{
    [JsonPropertyName("verified")]
    public bool Verified { get; set; }

    [JsonPropertyName("displayable")]
    public bool Displayable { get; set; }
}

/// <summary>
/// Market data for tokens.
/// </summary>
public class ZerionMarketData
{
    [JsonPropertyName("price")]
    public decimal? Price { get; set; }

    [JsonPropertyName("market_cap")]
    public decimal? MarketCap { get; set; }

    [JsonPropertyName("total_supply")]
    public decimal? TotalSupply { get; set; }

    [JsonPropertyName("circulating_supply")]
    public decimal? CirculatingSupply { get; set; }

    [JsonPropertyName("changes")]
    public ZerionChanges? Changes { get; set; }
}

/// <summary>
/// Token implementation on a specific chain.
/// </summary>
public class ZerionImplementation
{
    [JsonPropertyName("chain_id")]
    public string? ChainId { get; set; }

    [JsonPropertyName("address")]
    public string? Address { get; set; }

    [JsonPropertyName("decimals")]
    public int Decimals { get; set; }
}

/// <summary>
/// Chain attributes.
/// </summary>
public class ZerionChainAttributes
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("icon")]
    public ZerionIcon? Icon { get; set; }
}

/// <summary>
/// Application (protocol) metadata embedded in position attributes.
/// </summary>
public class ZerionApplicationMetadata
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("icon")]
    public ZerionIcon? Icon { get; set; }

    [JsonPropertyName("url")]
    public string? Url { get; set; }
}
