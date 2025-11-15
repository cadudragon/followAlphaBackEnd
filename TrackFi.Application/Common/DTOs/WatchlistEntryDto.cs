namespace TrackFi.Application.Common.DTOs;

/// <summary>
/// Data Transfer Object for WatchlistEntry entity.
/// </summary>
public class WatchlistEntryDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string WalletAddress { get; set; } = string.Empty;
    public string Network { get; set; } = string.Empty;
    public string? Label { get; set; }
    public string? Notes { get; set; }
    public DateTime AddedAt { get; set; }
}
