namespace TrackFi.Domain.Enums;

/// <summary>
/// Status of token verification/whitelisting.
/// </summary>
public enum VerificationStatus
{
    /// <summary>
    /// Token is verified and approved for display.
    /// </summary>
    Verified = 1,

    /// <summary>
    /// Token verification is pending review.
    /// </summary>
    Pending = 2,

    /// <summary>
    /// Token verification was rejected.
    /// </summary>
    Rejected = 3,

    /// <summary>
    /// Token was previously verified but is now suspended (e.g., security issue).
    /// </summary>
    Suspended = 4
}
