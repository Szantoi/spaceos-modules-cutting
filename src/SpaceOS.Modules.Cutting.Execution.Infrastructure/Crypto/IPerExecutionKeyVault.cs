using SpaceOS.Modules.Cutting.Execution.Domain.ValueObjects;

namespace SpaceOS.Modules.Cutting.Execution.Infrastructure.Crypto;

/// <summary>Port for per-execution AES-256 key management.</summary>
public interface IPerExecutionKeyVault
{
    /// <summary>Generates and stores a new AES-256 key for the given execution.</summary>
    Task<ExecutionKey> GenerateKeyAsync(Guid executionId, CancellationToken ct);

    /// <summary>Returns the key for the execution, or null if not found.</summary>
    Task<ExecutionKey?> GetKeyAsync(Guid executionId, CancellationToken ct);

    /// <summary>Securely zeroes and removes the key from the store.</summary>
    Task EraseAsync(Guid executionId, CancellationToken ct);
}
