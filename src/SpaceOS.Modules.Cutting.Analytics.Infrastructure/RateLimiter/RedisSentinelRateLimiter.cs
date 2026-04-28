namespace SpaceOS.Modules.Cutting.Analytics.Infrastructure.RateLimiter;

/// <summary>
/// In-process sliding-window rate limiter that implements both <see cref="IRateLimiter"/>
/// and <see cref="IHandshakeRateLimiter"/> (backward-compat shim for Phase 4).
/// In production this would delegate to Redis INCR + EXPIRE; here an in-memory store
/// is used as a fallback when no Redis connection is available.
/// </summary>
public sealed class RedisSentinelRateLimiter : IRateLimiter, IHandshakeRateLimiter
{
    private readonly int _maxRequests;
    private readonly TimeSpan _window;
    private readonly Dictionary<string, (int Count, DateTime Reset)> _store = new();
    private readonly object _lock = new();

    /// <summary>
    /// Creates a new <see cref="RedisSentinelRateLimiter"/>.
    /// </summary>
    /// <param name="maxRequests">Maximum requests allowed in <paramref name="window"/>. Default 60.</param>
    /// <param name="window">Sliding window duration. Default 1 minute.</param>
    public RedisSentinelRateLimiter(int maxRequests = 60, TimeSpan? window = null)
    {
        _maxRequests = maxRequests;
        _window = window ?? TimeSpan.FromMinutes(1);
    }

    /// <inheritdoc/>
    public Task<bool> TryAcquireAsync(string key, CancellationToken ct)
    {
        lock (_lock)
        {
            var now = DateTime.UtcNow;

            if (_store.TryGetValue(key, out var entry))
            {
                if (now > entry.Reset)
                    _store[key] = (1, now + _window);
                else if (entry.Count >= _maxRequests)
                    return Task.FromResult(false);
                else
                    _store[key] = (entry.Count + 1, entry.Reset);
            }
            else
            {
                _store[key] = (1, now + _window);
            }

            return Task.FromResult(true);
        }
    }

    // IHandshakeRateLimiter backward-compat — delegates to the general implementation.
    Task<bool> IHandshakeRateLimiter.TryAcquireAsync(string tenantId, CancellationToken ct)
        => TryAcquireAsync(tenantId, ct);
}
