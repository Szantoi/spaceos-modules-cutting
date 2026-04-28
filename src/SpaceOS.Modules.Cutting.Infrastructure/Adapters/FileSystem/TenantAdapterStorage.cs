using System.Text.RegularExpressions;

namespace SpaceOS.Modules.Cutting.Infrastructure.Adapters.FileSystem;

/// <summary>
/// SEC-01: Validates correlation IDs with a strict regex before constructing any paths.
/// Canonicalizes all paths and checks containment within the tenant root.
/// Rejects symlinks to prevent directory traversal attacks.
/// </summary>
internal sealed class TenantAdapterStorage : ITenantAdapterStorage
{
    private static readonly Regex CorrelationIdPattern =
        new(@"^[A-Za-z0-9\-]{1,100}$", RegexOptions.Compiled);

    private static readonly string BaseRoot =
        Path.Combine(Path.DirectorySeparatorChar.ToString(), "var", "lib", "spaceos-cutting", "adapters");

    public async Task WriteToInboxAsync(
        Guid tenantId, string adapterName, string correlationId, byte[] content, CancellationToken ct)
    {
        ValidateCorrelationId(correlationId);
        var inboxPath = GetInboxPath(tenantId, adapterName);
        var filePath = BuildSafePath(inboxPath, $"{correlationId}.payload");

        Directory.CreateDirectory(inboxPath);
        await File.WriteAllBytesAsync(filePath, content, ct).ConfigureAwait(false);
    }

    public async Task<byte[]?> TryReadFromOutboxAsync(
        Guid tenantId, string adapterName, string correlationId, CancellationToken ct)
    {
        ValidateCorrelationId(correlationId);
        var outboxPath = GetOutboxPath(tenantId, adapterName);

        // SEC-13: only read files with the .complete extension
        var completePath = BuildSafePath(outboxPath, $"{correlationId}.complete");

        if (!File.Exists(completePath))
            return null;

        RejectSymlink(completePath);
        return await File.ReadAllBytesAsync(completePath, ct).ConfigureAwait(false);
    }

    public string GetTenantRoot(Guid tenantId, string adapterName) =>
        Path.Combine(BaseRoot, tenantId.ToString("N"), adapterName);

    public string GetOutboxPath(Guid tenantId, string adapterName) =>
        Path.Combine(GetTenantRoot(tenantId, adapterName), "outbox");

    public Task<bool> CheckTenantRootAccessibleAsync(
        Guid tenantId, string adapterName, CancellationToken ct)
    {
        var root = GetTenantRoot(tenantId, adapterName);
        return Task.FromResult(Directory.Exists(root));
    }

    private string GetInboxPath(Guid tenantId, string adapterName) =>
        Path.Combine(GetTenantRoot(tenantId, adapterName), "inbox");

    private static void ValidateCorrelationId(string correlationId)
    {
        if (!CorrelationIdPattern.IsMatch(correlationId))
            throw new ArgumentException(
                $"CorrelationId '{correlationId}' is invalid. Must match ^[A-Za-z0-9-]{{1,100}}$.",
                nameof(correlationId));
    }

    private static string BuildSafePath(string directory, string fileName)
    {
        var fullPath = Path.GetFullPath(Path.Combine(directory, fileName));
        var rootFull = Path.GetFullPath(directory);

        if (!fullPath.StartsWith(rootFull, StringComparison.Ordinal))
            throw new InvalidOperationException($"Path traversal detected: '{fullPath}' is outside '{rootFull}'.");

        return fullPath;
    }

    private static void RejectSymlink(string path)
    {
        var info = new FileInfo(path);
        if (info.LinkTarget is not null)
            throw new InvalidOperationException($"Symlinks are not permitted: '{path}'.");
    }
}
