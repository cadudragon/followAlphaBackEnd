using System.Threading.Channels;
using Microsoft.Extensions.Logging;
using TrackFi.Domain.Events;

namespace TrackFi.Infrastructure.BackgroundServices;

/// <summary>
/// High-performance background queue implementation using System.Threading.Channels.
/// Provides lock-free, bounded queue with backpressure support.
/// Thread-safe for multiple producers and consumers.
/// </summary>
public sealed class TokenMetadataBackgroundQueue : ITokenMetadataBackgroundQueue
{
    private readonly Channel<TokenMetadataWriteEvent> _queue;
    private readonly ILogger<TokenMetadataBackgroundQueue> _logger;

    public TokenMetadataBackgroundQueue(
        int capacity,
        ILogger<TokenMetadataBackgroundQueue> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        // Create bounded channel with Drop oldest strategy when full
        // This prevents memory exhaustion under extreme load
        var options = new BoundedChannelOptions(capacity)
        {
            FullMode = BoundedChannelFullMode.DropOldest, // Drop oldest items if queue is full
            SingleReader = false, // Multiple background workers can dequeue
            SingleWriter = false  // Multiple user requests can enqueue
        };

        _queue = Channel.CreateBounded<TokenMetadataWriteEvent>(options);

        _logger.LogInformation(
            "TokenMetadataBackgroundQueue initialized with capacity {Capacity}",
            capacity);
    }

    /// <inheritdoc />
    public async ValueTask<bool> EnqueueAsync(
        TokenMetadataWriteEvent @event,
        CancellationToken cancellationToken = default)
    {
        if (@event == null)
            throw new ArgumentNullException(nameof(@event));

        try
        {
            await _queue.Writer.WriteAsync(@event, cancellationToken);

            _logger.LogDebug(
                "Enqueued token metadata for {Symbol} ({Contract}) on {Network}. Queue depth: {Count}",
                @event.Symbol,
                @event.ContractAddress,
                @event.Network,
                Count);

            return true;
        }
        catch (ChannelClosedException)
        {
            _logger.LogWarning(
                "Failed to enqueue token metadata - queue is closed: {Symbol} ({Contract})",
                @event.Symbol,
                @event.ContractAddress);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Unexpected error enqueueing token metadata: {Symbol} ({Contract})",
                @event.Symbol,
                @event.ContractAddress);
            return false;
        }
    }

    /// <inheritdoc />
    public async ValueTask<TokenMetadataWriteEvent?> DequeueAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            var @event = await _queue.Reader.ReadAsync(cancellationToken);

            // Calculate queue latency for monitoring
            var latency = DateTimeOffset.UtcNow - @event.CreatedAt;
            if (latency.TotalSeconds > 5)
            {
                _logger.LogWarning(
                    "High queue latency detected: {Latency}s for token {Symbol} ({Contract})",
                    latency.TotalSeconds,
                    @event.Symbol,
                    @event.ContractAddress);
            }

            return @event;
        }
        catch (ChannelClosedException)
        {
            _logger.LogInformation("Queue channel closed, stopping dequeue operations");
            return null;
        }
        catch (OperationCanceledException)
        {
            // Normal cancellation during shutdown
            return null;
        }
    }

    /// <inheritdoc />
    public int Count => _queue.Reader.Count;
}
