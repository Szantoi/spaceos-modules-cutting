using Ardalis.Result;
using SpaceOS.Modules.Cutting.Infrastructure.Adapters.FileSystem;
using SpaceOS.Modules.Cutting.Infrastructure.Adapters.Resilience;

namespace SpaceOS.Modules.Cutting.Infrastructure.Adapters.Transport;

/// <summary>
/// Transport that invokes a local CLI tool to process cutting data.
/// SEC-05: arguments are built as a structured list, never concatenated into a shell command string.
/// </summary>
internal sealed class CliWrapperTransport : IExternalAdapterTransport
{
    private readonly ITenantAdapterStorage _storage;
    private readonly IBoundedSubprocessRunner _runner;
    private readonly TimeProvider _clock;

    public CliWrapperTransport(
        ITenantAdapterStorage storage,
        IBoundedSubprocessRunner runner,
        TimeProvider clock)
    {
        ArgumentNullException.ThrowIfNull(storage);
        ArgumentNullException.ThrowIfNull(runner);
        ArgumentNullException.ThrowIfNull(clock);
        _storage = storage;
        _runner = runner;
        _clock = clock;
    }

    public string TransportName => "cli-wrapper";

    public async Task<Result<TransportSubmitResult>> SubmitAsync(AdapterPayload payload, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(payload);

        if (!payload.Metadata.TryGetValue("tenantId", out var tenantIdStr) ||
            !Guid.TryParse(tenantIdStr, out var tenantId))
            return Result<TransportSubmitResult>.Invalid(new ValidationError("Metadata 'tenantId' is required."));

        if (!payload.Metadata.TryGetValue("adapterName", out var adapterName))
            return Result<TransportSubmitResult>.Invalid(new ValidationError("Metadata 'adapterName' is required."));

        if (!payload.Metadata.TryGetValue("executable", out var executable))
            return Result<TransportSubmitResult>.Invalid(new ValidationError("Metadata 'executable' is required."));

        var correlationId = Guid.NewGuid().ToString("N");

        await _storage.WriteToInboxAsync(tenantId, adapterName, correlationId, payload.Content, ct)
            .ConfigureAwait(false);

        // SEC-05: build arguments as a list — no string concatenation
        var arguments = BuildArguments(
            tenantId, adapterName, correlationId,
            _storage.GetTenantRoot(tenantId, adapterName));

        var req = new BoundedSubprocessRequest(
            Executable: executable,
            Arguments: arguments,
            Timeout: TimeSpan.FromMinutes(5),
            MaxMemoryMb: 512,
            WorkingDirectory: _storage.GetTenantRoot(tenantId, adapterName));

        var result = await _runner.RunAsync(req, ct).ConfigureAwait(false);

        if (result.TimedOut)
            return Result<TransportSubmitResult>.Error("CLI adapter timed out.");

        if (result.ExitCode != 0)
            return Result<TransportSubmitResult>.Error($"CLI adapter exited with code {result.ExitCode}.");

        return Result<TransportSubmitResult>.Success(
            new TransportSubmitResult(Guid.NewGuid(), correlationId, _clock.GetUtcNow()));
    }

    public async Task<Result<AdapterPayload>> PollResultAsync(string correlationId, CancellationToken ct)
    {
        var parts = correlationId.Split('|');
        if (parts.Length == 3 &&
            Guid.TryParse(parts[0], out var tenantId) &&
            !string.IsNullOrWhiteSpace(parts[1]))
        {
            var bytes = await _storage.TryReadFromOutboxAsync(tenantId, parts[1], parts[2], ct)
                .ConfigureAwait(false);

            return bytes is null
                ? Result<AdapterPayload>.NotFound($"No result for '{correlationId}'.")
                : Result<AdapterPayload>.Success(
                    new AdapterPayload("application/octet-stream", bytes, new Dictionary<string, string>()));
        }

        return Result<AdapterPayload>.NotFound($"Result not ready for '{correlationId}'.");
    }

    public async Task<Result> PingAsync(CancellationToken ct)
    {
        await Task.Yield();
        return Result.Success();
    }

    /// <summary>
    /// SEC-05: Returns a structured argument list — never concatenates into a shell string.
    /// </summary>
    private static IReadOnlyList<string> BuildArguments(
        Guid tenantId, string adapterName, string correlationId, string workingDir) =>
        new List<string>
        {
            "--tenant-id", tenantId.ToString("N"),
            "--adapter", adapterName,
            "--correlation-id", correlationId,
            "--work-dir", workingDir
        }.AsReadOnly();
}
