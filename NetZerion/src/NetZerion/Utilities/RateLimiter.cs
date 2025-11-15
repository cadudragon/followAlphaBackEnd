using NetZerion.Configuration;
using NetZerion.Exceptions;

namespace NetZerion.Utilities;

/// <summary>
/// Tracks and enforces rate limits for API requests.
/// </summary>
public class RateLimiter
{
    private readonly RateLimitOptions _options;
    private readonly object _lock = new();
    private readonly Queue<DateTimeOffset> _requestTimestamps = new();
    private int _dailyRequestCount;
    private DateTimeOffset _dailyResetTime;

    /// <summary>
    /// Initializes a new instance of the <see cref="RateLimiter"/> class.
    /// </summary>
    /// <param name="options">Rate limit configuration options.</param>
    public RateLimiter(RateLimitOptions options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _dailyResetTime = DateTimeOffset.UtcNow.Date.AddDays(1);
    }

    /// <summary>
    /// Checks if a request can be made and throws if rate limit would be exceeded.
    /// </summary>
    /// <exception cref="RateLimitException">Thrown when rate limit is exceeded.</exception>
    public Task CheckRateLimitAsync(CancellationToken cancellationToken = default)
    {
        if (!_options.EnableRateLimiting)
            return Task.CompletedTask;

        lock (_lock)
        {
            var now = DateTimeOffset.UtcNow;

            // Reset daily counter if needed
            if (now >= _dailyResetTime)
            {
                _dailyRequestCount = 0;
                _dailyResetTime = now.Date.AddDays(1);
            }

            // Clean up old per-minute timestamps
            while (_requestTimestamps.Count > 0 && now - _requestTimestamps.Peek() > TimeSpan.FromMinutes(1))
            {
                _requestTimestamps.Dequeue();
            }

            // Check daily limit
            if (_dailyRequestCount >= _options.RequestsPerDay)
            {
                var resetSeconds = (int)(_dailyResetTime - now).TotalSeconds;
                if (_options.ThrowOnRateLimit)
                {
                    throw new RateLimitException(resetSeconds, 0);
                }
            }

            // Check per-minute limit
            if (_requestTimestamps.Count >= _options.RequestsPerMinute)
            {
                var oldestRequest = _requestTimestamps.Peek();
                var waitTime = TimeSpan.FromMinutes(1) - (now - oldestRequest);

                if (waitTime > TimeSpan.Zero)
                {
                    if (_options.ThrowOnRateLimit)
                    {
                        throw new RateLimitException((int)waitTime.TotalSeconds, _options.RequestsPerMinute - _requestTimestamps.Count);
                    }
                }
            }
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Records that a request was made.
    /// </summary>
    public void RecordRequest()
    {
        if (!_options.EnableRateLimiting)
            return;

        lock (_lock)
        {
            _dailyRequestCount++;
            _requestTimestamps.Enqueue(DateTimeOffset.UtcNow);
        }
    }

    /// <summary>
    /// Gets the number of requests remaining for today.
    /// </summary>
    public int GetDailyRequestsRemaining()
    {
        lock (_lock)
        {
            return Math.Max(0, _options.RequestsPerDay - _dailyRequestCount);
        }
    }

    /// <summary>
    /// Gets the number of requests made today.
    /// </summary>
    public int GetDailyRequestCount()
    {
        lock (_lock)
        {
            return _dailyRequestCount;
        }
    }
}
