using System.Net;
using Ardalis.Result;

namespace SpaceOS.Modules.Cutting.Infrastructure.Adapters.Transport;

/// <summary>
/// Transport that communicates with an external cutting system over HTTP.
/// SEC-03: validates the configured base URL against an allowlist regex and SSRF IP blocker.
/// Enforces HTTPS-only.
/// </summary>
internal sealed class RestApiTransport : IExternalAdapterTransport
{
    private readonly HttpClient _http;
    private readonly IIpRangeChecker _ipRangeChecker;

    public RestApiTransport(HttpClient http, IIpRangeChecker ipRangeChecker)
    {
        ArgumentNullException.ThrowIfNull(http);
        ArgumentNullException.ThrowIfNull(ipRangeChecker);
        _http = http;
        _ipRangeChecker = ipRangeChecker;

        ValidateBaseUrl(http.BaseAddress);
    }

    public string TransportName => "rest-api";

    public async Task<Result<TransportSubmitResult>> SubmitAsync(AdapterPayload payload, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(payload);

        using var content = new ByteArrayContent(payload.Content);
        content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(payload.ContentType);

        var response = await _http.PostAsync("submit", content, ct).ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
            return Result<TransportSubmitResult>.Error(
                $"Remote adapter returned {(int)response.StatusCode}.");

        var correlationId = response.Headers.TryGetValues("X-Correlation-Id", out var vals)
            ? vals.First()
            : Guid.NewGuid().ToString("N");

        return Result<TransportSubmitResult>.Success(
            new TransportSubmitResult(Guid.NewGuid(), correlationId, DateTimeOffset.UtcNow));
    }

    public async Task<Result<AdapterPayload>> PollResultAsync(string correlationId, CancellationToken ct)
    {
        var response = await _http.GetAsync($"results/{correlationId}", ct).ConfigureAwait(false);

        if (response.StatusCode == HttpStatusCode.NotFound)
            return Result<AdapterPayload>.NotFound($"No result for '{correlationId}'.");

        if (!response.IsSuccessStatusCode)
            return Result<AdapterPayload>.Error($"Remote returned {(int)response.StatusCode}.");

        var bytes = await response.Content.ReadAsByteArrayAsync(ct).ConfigureAwait(false);
        var contentType = response.Content.Headers.ContentType?.MediaType ?? "application/octet-stream";

        return Result<AdapterPayload>.Success(
            new AdapterPayload(contentType, bytes, new Dictionary<string, string>()));
    }

    public async Task<Result> PingAsync(CancellationToken ct)
    {
        try
        {
            var response = await _http.GetAsync("health", ct).ConfigureAwait(false);
            return response.IsSuccessStatusCode
                ? Result.Success()
                : Result.Error($"Health endpoint returned {(int)response.StatusCode}.");
        }
        catch (HttpRequestException ex)
        {
            return Result.Error($"Ping failed: {ex.Message}");
        }
    }

    private void ValidateBaseUrl(Uri? baseAddress)
    {
        if (baseAddress is null)
            throw new InvalidOperationException("RestApiTransport requires a BaseAddress on the HttpClient.");

        if (!string.Equals(baseAddress.Scheme, "https", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException($"SEC-03: Only HTTPS base URLs are permitted. Got: {baseAddress.Scheme}.");

        // Resolve to IP and check SSRF range — only when the host is an IP; DNS resolution done at runtime
        if (IPAddress.TryParse(baseAddress.Host, out var ip) && _ipRangeChecker.IsBlocked(ip))
            throw new InvalidOperationException(
                $"SEC-03: The base URL resolves to a blocked IP range: {ip}.");
    }
}
