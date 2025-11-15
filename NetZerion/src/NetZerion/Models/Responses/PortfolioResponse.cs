using NetZerion.Models.Entities;

namespace NetZerion.Models.Responses;

/// <summary>
/// Response containing fungible token portfolio with pagination support.
/// </summary>
public class PortfolioResponse
{
    /// <summary>
    /// List of fungible tokens
    /// </summary>
    public List<Fungible> Data { get; set; } = new();

    /// <summary>
    /// Pagination links
    /// </summary>
    public PaginationLinks Links { get; set; } = new();

    /// <summary>
    /// Total portfolio value in USD
    /// </summary>
    public decimal? TotalValueUsd { get; set; }

    /// <summary>
    /// Total number of tokens (if available)
    /// </summary>
    public int? TotalCount { get; set; }

    /// <summary>
    /// Indicates if there are more tokens to fetch
    /// </summary>
    public bool HasMore => !string.IsNullOrEmpty(Links.Next);
}
