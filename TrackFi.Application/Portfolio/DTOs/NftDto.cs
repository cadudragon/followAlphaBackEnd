namespace TrackFi.Application.Portfolio.DTOs;

/// <summary>
/// DTO representing an NFT with metadata and floor price.
/// </summary>
public class NftDto
{
    public string ContractAddress { get; set; } = string.Empty;
    public string TokenId { get; set; } = string.Empty;
    public string Network { get; set; } = string.Empty;
    public string? Name { get; set; }
    public string? Description { get; set; }
    public string? CollectionName { get; set; }
    public string? ImageUrl { get; set; }
    public string? ExternalUrl { get; set; }
    public string TokenStandard { get; set; } = string.Empty; // ERC721, ERC1155
    public int? Balance { get; set; } // For ERC1155 (quantity owned)
    public NftFloorPriceDto? FloorPrice { get; set; }
    public decimal? EstimatedValueUsd { get; set; }
}

public class NftFloorPriceDto
{
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "ETH"; // ETH, MATIC, etc.
    public decimal? UsdValue { get; set; }
    public DateTime LastUpdated { get; set; }
}
