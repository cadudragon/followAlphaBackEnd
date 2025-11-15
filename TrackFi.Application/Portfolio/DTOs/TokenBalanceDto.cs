namespace TrackFi.Application.Portfolio.DTOs;

/// <summary>
/// DTO representing a token balance with pricing information.
/// </summary>
public class TokenBalanceDto
{
    public string? ContractAddress { get; set; } // Null for native tokens (ETH, MATIC, etc.)
    public string Network { get; set; } = string.Empty;
    public string Symbol { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Balance { get; set; } = "0"; // Raw balance as string to preserve precision
    public int Decimals { get; set; }
    public decimal BalanceFormatted { get; set; } // Balance divided by 10^decimals
    public PriceInfoDto? Price { get; set; }
    public decimal? ValueUsd { get; set; } // Balance * Price in USD
    public decimal? Change24h { get; set; } // 24h price change percentage
    public string? LogoUrl { get; set; }
}

public class PriceInfoDto
{
    public decimal Usd { get; set; }
    public decimal? Eur { get; set; }
    public decimal? Btc { get; set; }
    public decimal? Eth { get; set; }
    public DateTime LastUpdated { get; set; }
}
