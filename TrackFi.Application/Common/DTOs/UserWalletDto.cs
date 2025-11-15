namespace TrackFi.Application.Common.DTOs;

/// <summary>
/// Data Transfer Object for UserWallet entity.
/// </summary>
public class UserWalletDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string WalletAddress { get; set; } = string.Empty;
    public string Network { get; set; } = string.Empty;
    public string? Label { get; set; }
    public bool IsVerified { get; set; }
    public DateTime? VerifiedAt { get; set; }
    public DateTime AddedAt { get; set; }
}
