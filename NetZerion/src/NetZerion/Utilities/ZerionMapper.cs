using NetZerion.Models.Entities;
using NetZerion.Models.Enums;
using NetZerion.Models.JsonApi;
using NetZerion.Models.Responses;

namespace NetZerion.Utilities;

/// <summary>
/// Maps Zerion JSON:API responses to domain models.
/// </summary>
public static class ZerionMapper
{
    /// <summary>
    /// Maps a JSON:API position response to PositionsResponse.
    /// </summary>
    public static PositionsResponse MapPositionsResponse(JsonApiResponse<ZerionPositionAttributes> apiResponse)
    {
        // Map positions directly - protocol data is in application_metadata
        var positions = apiResponse.Data.Select(MapPosition).ToList();

        return new PositionsResponse
        {
            Data = positions,
            Links = MapPaginationLinks(apiResponse.Links),
            TotalCount = positions.Count
        };
    }

    /// <summary>
    /// Maps a JSON:API fungible response to PortfolioResponse.
    /// </summary>
    public static PortfolioResponse MapPortfolioResponse(JsonApiResponse<ZerionFungibleAttributes> apiResponse)
    {
        var fungibles = apiResponse.Data.Select(MapFungible).ToList();
        var totalValue = fungibles.Sum(f => f.ValueUsd ?? 0);

        return new PortfolioResponse
        {
            Data = fungibles,
            Links = MapPaginationLinks(apiResponse.Links),
            TotalValueUsd = totalValue,
            TotalCount = fungibles.Count
        };
    }

    /// <summary>
    /// Maps a single JSON:API position resource to Position entity.
    /// </summary>
    private static Position MapPosition(JsonApiResource<ZerionPositionAttributes> resource)
    {
        var attrs = resource.Attributes;
        if (attrs == null)
            return new Position { Id = resource.Id };

        // Extract chain ID from relationships
        var chainId = resource.Relationships?.TryGetValue("chain", out var chainRelationship) == true
            ? chainRelationship.Data?.Id
            : null;

        // Extract protocol ID from relationships (Zerion calls it "dapp")
        var protocolId = resource.Relationships?.TryGetValue("dapp", out var dappRelationship) == true
            ? dappRelationship.Data?.Id
            : null;

        // Use application_metadata for protocol info (icon, url, name)
        var appMetadata = attrs.ApplicationMetadata;

        return new Position
        {
            Id = resource.Id,
            Protocol = new Protocol
            {
                Name = appMetadata?.Name ?? attrs.Protocol ?? "Unknown",
                Id = protocolId ?? attrs.Protocol?.ToLowerInvariant().Replace(" ", "-") ?? "unknown",
                IconUrl = appMetadata?.Icon?.Url,
                WebsiteUrl = appMetadata?.Url
            },
            Name = attrs.Name ?? string.Empty,
            Type = ParsePositionType(attrs.PositionType),
            ProtocolModule = attrs.ProtocolModule,
            PoolAddress = attrs.PoolAddress,
            GroupId = attrs.GroupId,
            Quantity = attrs.Quantity?.Float ?? 0,
            ValueUsd = attrs.Value ?? 0,
            ValueChange24h = attrs.Changes?.Absolute1d,
            PercentChange24h = attrs.Changes?.Percent1d,
            Assets = attrs.FungibleInfo != null
                ? new List<Fungible> { MapFungibleInfoWithValues(attrs.FungibleInfo, attrs.Quantity, attrs.Value, attrs.Price, chainId) }
                : new List<Fungible>(),
            Chain = chainId != null ? new Chain { Id = chainId } : null,
            RawData = attrs
        };
    }

    /// <summary>
    /// Maps a single JSON:API fungible resource to Fungible entity.
    /// </summary>
    private static Fungible MapFungible(JsonApiResource<ZerionFungibleAttributes> resource)
    {
        var attrs = resource.Attributes;
        if (attrs == null)
            return new Fungible();

        // Extract chain ID from relationships
        var chainId = resource.Relationships?.TryGetValue("chain", out var chainRelationship) == true
            ? chainRelationship.Data?.Id
            : null;

        // Find implementation matching the chain, fallback to first
        var implementation = attrs.Implementations?.FirstOrDefault(i => i.ChainId == chainId)
                            ?? attrs.Implementations?.FirstOrDefault();

        return new Fungible
        {
            Address = implementation?.Address ?? "native",
            Symbol = attrs.Symbol ?? string.Empty,
            Name = attrs.Name ?? string.Empty,
            Decimals = implementation?.Decimals ?? 18,
            Balance = attrs.Quantity?.Float ?? 0,
            BalanceRaw = attrs.Quantity?.Int ?? "0",
            PriceUsd = attrs.MarketData?.Price ?? attrs.Value / (attrs.Quantity?.Float ?? 1),
            ValueUsd = attrs.Value,
            IconUrl = attrs.Icon?.Url,
            IsVerified = attrs.Flags?.Verified ?? false,
            IsDisplayable = attrs.Flags?.Displayable ?? true,
            PriceChange24h = attrs.MarketData?.Changes?.Percent1d,
            Metadata = new Dictionary<string, object>
            {
                { "market_cap", attrs.MarketData?.MarketCap ?? 0 },
                { "description", attrs.Description ?? string.Empty }
            }
        };
    }

    /// <summary>
    /// Maps fungible info to a Fungible entity (simplified).
    /// </summary>
    private static Fungible MapFungibleInfo(ZerionFungibleInfo info, string? chainId = null)
    {
        // Find implementation matching the chain, fallback to first
        var implementation = info.Implementations?.FirstOrDefault(i => i.ChainId == chainId)
                            ?? info.Implementations?.FirstOrDefault();

        return new Fungible
        {
            Symbol = info.Symbol ?? string.Empty,
            Name = info.Name ?? string.Empty,
            Address = implementation?.Address ?? "unknown",
            Decimals = implementation?.Decimals ?? 18,
            IconUrl = info.Icon?.Url,
            IsVerified = info.Flags?.Verified ?? false
        };
    }

    /// <summary>
    /// Maps fungible info with position-level quantity, value, and price.
    /// </summary>
    private static Fungible MapFungibleInfoWithValues(
        ZerionFungibleInfo info,
        ZerionQuantity? quantity,
        decimal? value,
        decimal? price,
        string? chainId = null)
    {
        // Find implementation matching the chain, fallback to first
        var implementation = info.Implementations?.FirstOrDefault(i => i.ChainId == chainId)
                            ?? info.Implementations?.FirstOrDefault();

        return new Fungible
        {
            Symbol = info.Symbol ?? string.Empty,
            Name = info.Name ?? string.Empty,
            Address = implementation?.Address ?? "unknown",
            Decimals = implementation?.Decimals ?? 18,
            Balance = quantity?.Float ?? 0,
            BalanceRaw = quantity?.Int ?? "0",
            PriceUsd = price,
            ValueUsd = value,
            IconUrl = info.Icon?.Url,
            IsVerified = info.Flags?.Verified ?? false
        };
    }

    /// <summary>
    /// Maps JSON:API links to PaginationLinks.
    /// </summary>
    private static PaginationLinks MapPaginationLinks(JsonApiLinks? links)
    {
        if (links == null)
            return new PaginationLinks();

        return new PaginationLinks
        {
            Self = links.Self,
            Next = links.Next,
            Previous = links.Prev,
            First = links.First,
            Last = links.Last
        };
    }

    /// <summary>
    /// Parses position type string to PositionType enum.
    /// </summary>
    private static PositionType ParsePositionType(string? typeString)
    {
        return typeString?.ToLowerInvariant() switch
        {
            "deposit" => PositionType.Deposit,
            "loan" => PositionType.Borrowing,  // Loan = borrowed position
            "borrow" => PositionType.Borrowing,
            "staked" => PositionType.Staked,  // Farming staked LP tokens
            "staking" => PositionType.Staking,  // Regular staking
            "reward" => PositionType.Reward,  // Farming/staking rewards
            "locked" => PositionType.Locked,
            "claimable" => PositionType.Claimable,
            "liquidity" => PositionType.LiquidityPool,
            "farming" => PositionType.Farming,
            _ => PositionType.Other
        };
    }

}
