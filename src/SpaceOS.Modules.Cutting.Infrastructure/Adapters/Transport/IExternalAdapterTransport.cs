using Ardalis.Result;

namespace SpaceOS.Modules.Cutting.Infrastructure.Adapters.Transport;

/// <summary>
/// Low-level transport mechanism for communicating with an external cutting system adapter.
/// </summary>
public interface IExternalAdapterTransport
{
    /// <summary>Unique name for this transport (e.g. "file-exchange", "rest-api", "cli-wrapper").</summary>
    string TransportName { get; }

    /// <summary>Submits a payload to the external system.</summary>
    Task<Result<TransportSubmitResult>> SubmitAsync(AdapterPayload payload, CancellationToken ct);

    /// <summary>
    /// Polls for a result. Returns <see cref="Result{T}.NotFound"/> when the result is not yet ready.
    /// </summary>
    Task<Result<AdapterPayload>> PollResultAsync(string correlationId, CancellationToken ct);

    /// <summary>Performs a connectivity/health check for the external system.</summary>
    Task<Result> PingAsync(CancellationToken ct);
}
