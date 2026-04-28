namespace SpaceOS.Modules.Cutting.Analytics.Infrastructure.Tpm;

/// <summary>
/// Provisions per-execution encryption keys, optionally backed by a hardware TPM.
/// When the TPM is unavailable, implementations fall back to a software KEK (P4-9 debt).
/// </summary>
public interface ITpmKeyProvisioner
{
    /// <summary>
    /// Attempts to provision a key for the given execution context.
    /// Returns <see langword="null"/> when the TPM is disabled — callers must fall back to AES-256 KEK.
    /// </summary>
    Task<byte[]?> TryProvisionKeyAsync(Guid tenantId, Guid executionId, CancellationToken ct);

    /// <summary>
    /// Indicates whether a hardware TPM is available in this environment.
    /// </summary>
    bool IsTpmAvailable { get; }
}
