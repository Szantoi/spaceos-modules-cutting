namespace SpaceOS.Modules.Cutting.Analytics.Infrastructure.RateLimiter;

/// <summary>
/// Rate limiter scoped to tenant handshake operations.
/// Kept for backward-compatibility with Phase 4 callers.
/// </summary>
public interface IHandshakeRateLimiter
{
    /// <summary>
    /// Attempts to acquire a token for the given <paramref name="tenantId"/>.
    /// Returns <see langword="true"/> when the request is allowed, <see langword="false"/> when throttled.
    /// </summary>
    Task<bool> TryAcquireAsync(string tenantId, CancellationToken ct);
}
