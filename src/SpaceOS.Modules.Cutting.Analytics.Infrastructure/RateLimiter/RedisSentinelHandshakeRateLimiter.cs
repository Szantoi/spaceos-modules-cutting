namespace SpaceOS.Modules.Cutting.Analytics.Infrastructure.RateLimiter;

/// <summary>
/// Backward-compatible adapter that wraps <see cref="RedisSentinelRateLimiter"/>.
/// </summary>
[Obsolete("Use RedisSentinelRateLimiter directly. This adapter will be removed in the next release.")]
public sealed class RedisSentinelHandshakeRateLimiter(RedisSentinelRateLimiter inner) : IHandshakeRateLimiter
{
    private readonly RedisSentinelRateLimiter _inner = inner ?? throw new ArgumentNullException(nameof(inner));

    /// <inheritdoc/>
    public Task<bool> TryAcquireAsync(string tenantId, CancellationToken ct)
        => _inner.TryAcquireAsync(tenantId, ct);
}
