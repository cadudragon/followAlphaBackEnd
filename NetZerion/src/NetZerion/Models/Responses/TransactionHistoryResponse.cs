using NetZerion.Models.Entities;

namespace NetZerion.Models.Responses;

/// <summary>
/// Response containing transaction history with cursor-based pagination.
/// </summary>
public class TransactionHistoryResponse
{
    /// <summary>
    /// List of transactions
    /// </summary>
    public List<Transaction> Data { get; set; } = new();

    /// <summary>
    /// Pagination links
    /// </summary>
    public PaginationLinks Links { get; set; } = new();

    /// <summary>
    /// Cursor for fetching the next page of results
    /// </summary>
    public string? NextCursor { get; set; }

    /// <summary>
    /// Cursor for fetching the previous page of results
    /// </summary>
    public string? PreviousCursor { get; set; }

    /// <summary>
    /// Total number of transactions (if available)
    /// </summary>
    public int? TotalCount { get; set; }

    /// <summary>
    /// Indicates if there are more transactions to fetch
    /// </summary>
    public bool HasMore => !string.IsNullOrEmpty(NextCursor) || !string.IsNullOrEmpty(Links.Next);
}
