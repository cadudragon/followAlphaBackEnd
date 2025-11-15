namespace NetZerion.Models.Responses;

/// <summary>
/// Pagination links for navigating through API responses.
/// </summary>
public class PaginationLinks
{
    /// <summary>
    /// URL for the current page
    /// </summary>
    public string? Self { get; set; }

    /// <summary>
    /// URL for the next page of results
    /// </summary>
    public string? Next { get; set; }

    /// <summary>
    /// URL for the previous page of results
    /// </summary>
    public string? Previous { get; set; }

    /// <summary>
    /// URL for the first page of results
    /// </summary>
    public string? First { get; set; }

    /// <summary>
    /// URL for the last page of results
    /// </summary>
    public string? Last { get; set; }
}
