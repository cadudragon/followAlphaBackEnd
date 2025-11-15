namespace TrackFi.Application.Common.DTOs;

/// <summary>
/// Data Transfer Object for User entity.
/// </summary>
public class UserDto
{
    public Guid Id { get; set; }
    public string PrimaryWalletAddress { get; set; } = string.Empty;
    public string PrimaryWalletNetwork { get; set; } = string.Empty;
    public string? CoverPictureUrl { get; set; }
    public string? CoverNftContract { get; set; }
    public string? CoverNftTokenId { get; set; }
    public string? CoverNftNetwork { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime LastActiveAt { get; set; }
}
