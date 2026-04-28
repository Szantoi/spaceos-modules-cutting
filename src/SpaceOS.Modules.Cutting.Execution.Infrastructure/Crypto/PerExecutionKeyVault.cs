using System.Collections.Concurrent;
using System.Security.Cryptography;
using SpaceOS.Modules.Cutting.Execution.Domain.ValueObjects;

namespace SpaceOS.Modules.Cutting.Execution.Infrastructure.Crypto;

/// <summary>
/// In-process AES-256 per-execution key vault backed by a concurrent dictionary.
/// Production deployments should replace this with an HSM or cloud KMS binding.
/// </summary>
public sealed class PerExecutionKeyVault : IPerExecutionKeyVault
{
    // Stores key bytes — zeroed on erase
    private readonly ConcurrentDictionary<Guid, byte[]> _store = new();

    /// <inheritdoc />
    public Task<ExecutionKey> GenerateKeyAsync(Guid executionId, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        var key = ExecutionKey.Generate();
        // Store a copy so erase can zero it in-place
        var bytes = (byte[])key.KeyBytes.Clone();
        _store[executionId] = bytes;
        return Task.FromResult(key);
    }

    /// <inheritdoc />
    public Task<ExecutionKey?> GetKeyAsync(Guid executionId, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        if (_store.TryGetValue(executionId, out var bytes))
        {
            var result = ExecutionKey.Create((byte[])bytes.Clone());
            return Task.FromResult<ExecutionKey?>(result.IsSuccess ? result.Value : null);
        }

        return Task.FromResult<ExecutionKey?>(null);
    }

    /// <inheritdoc />
    public Task EraseAsync(Guid executionId, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        if (_store.TryRemove(executionId, out var bytes))
        {
            CryptographicOperations.ZeroMemory(bytes);
        }

        return Task.CompletedTask;
    }
}
