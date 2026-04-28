namespace SpaceOS.Modules.Cutting.Analytics.Infrastructure.RateLimiter;

/// <summary>General-purpose rate limiter abstraction.</summary>
public interface IRateLimiter
{
    /// <summary>
    /// Attempts to acquire a token for the given <paramref name="key"/>.
    /// Returns <see langword="true"/> when the request is allowed, <see langword="false"/> when throttled.
    /// </summary>
    Task<bool> TryAcquireAsync(string key, CancellationToken ct);
}
