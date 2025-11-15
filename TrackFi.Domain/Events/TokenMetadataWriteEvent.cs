using TrackFi.Domain.Enums;

namespace TrackFi.Domain.Events;

/// <summary>
/// Domain event for token metadata that needs to be persisted to database.
/// Processed asynchronously in background to avoid blocking user requests.
/// </summary>
public sealed record TokenMetadataWriteEvent(
    string ContractAddress,
    BlockchainNetwork Network,
    string Symbol,
    string Name,
    int Decimals,
    string? LogoUrl,
    string Source)
{
    /// <summary>
    /// Timestamp when this event was created.
    /// Used for monitoring queue latency.
    /// </summary>
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Number of retry attempts for this event.
    /// </summary>
    public int RetryCount { get; init; } = 0;
}
