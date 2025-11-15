using NetZerion.Models.Entities;

namespace NetZerion.Models.Responses;

/// <summary>
/// Response containing DeFi positions with pagination support.
/// </summary>
public class PositionsResponse
{
    /// <summary>
    /// List of DeFi positions
    /// </summary>
    public List<Position> Data { get; set; } = new();

    /// <summary>
    /// Pagination links
    /// </summary>
    public PaginationLinks Links { get; set; } = new();

    /// <summary>
    /// Total number of positions (if available)
    /// </summary>
    public int? TotalCount { get; set; }

    /// <summary>
    /// Indicates if there are more positions to fetch
    /// </summary>
    public bool HasMore => !string.IsNullOrEmpty(Links.Next);
}
