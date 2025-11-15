using TrackFi.Domain.Events;

namespace TrackFi.Infrastructure.BackgroundServices;

/// <summary>
/// Background queue for processing token metadata writes asynchronously.
/// Decouples user requests from database writes for better performance and scalability.
/// </summary>
public interface ITokenMetadataBackgroundQueue
{
    /// <summary>
    /// Enqueues a token metadata write event for background processing.
    /// Returns immediately without blocking the caller.
    /// </summary>
    /// <param name="event">The metadata write event to process</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if enqueued successfully, false if queue is full</returns>
    ValueTask<bool> EnqueueAsync(TokenMetadataWriteEvent @event, CancellationToken cancellationToken = default);

    /// <summary>
    /// Dequeues a token metadata write event for processing.
    /// Blocks until an item is available or cancellation is requested.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The next event to process, or null if cancelled</returns>
    ValueTask<TokenMetadataWriteEvent?> DequeueAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current number of items in the queue.
    /// Used for monitoring and telemetry.
    /// </summary>
    int Count { get; }
}
