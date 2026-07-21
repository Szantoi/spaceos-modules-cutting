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
        new(@"^[A-Za-z0-9\-]{1,100}$", RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly Regex AdapterNamePattern =
        new(@"^[A-Za-z0-9][A-Za-z0-9_-]{0,63}$", RegexOptions.Compiled | RegexOptions.CultureInvariant);

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

    public string GetTenantRoot(Guid tenantId, string adapterName)
    {
        ValidateAdapterName(adapterName);

        var tenantBase = Path.GetFullPath(Path.Combine(BaseRoot, tenantId.ToString("N")));
        var adapterRoot = Path.GetFullPath(Path.Combine(tenantBase, adapterName));
        EnsurePathIsBelowRoot(adapterRoot, tenantBase);
        return adapterRoot;
    }

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

    private static void ValidateAdapterName(string adapterName)
    {
        if (string.IsNullOrWhiteSpace(adapterName) || !AdapterNamePattern.IsMatch(adapterName))
        {
            throw new ArgumentException(
                "AdapterName must be 1-64 ASCII letters, digits, underscores, or hyphens and start with a letter or digit.",
                nameof(adapterName));
        }
    }

    private static string BuildSafePath(string directory, string fileName)
    {
        var fullPath = Path.GetFullPath(Path.Combine(directory, fileName));
        var rootFull = Path.GetFullPath(directory);

        EnsurePathIsBelowRoot(fullPath, rootFull);

        return fullPath;
    }

    private static void EnsurePathIsBelowRoot(string path, string root)
    {
        var normalizedRoot = Path.GetFullPath(root)
            .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
            + Path.DirectorySeparatorChar;
        var normalizedPath = Path.GetFullPath(path);
        var comparison = OperatingSystem.IsWindows()
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;

        if (!normalizedPath.StartsWith(normalizedRoot, comparison))
            throw new InvalidOperationException($"Path traversal detected: '{normalizedPath}' is outside '{root}'.");
    }

    private static void RejectSymlink(string path)
    {
        var info = new FileInfo(path);
        if (info.LinkTarget is not null)
            throw new InvalidOperationException($"Symlinks are not permitted: '{path}'.");
    }
}
