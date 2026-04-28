namespace SpaceOS.Modules.Cutting.Analytics.Infrastructure.Tpm;

/// <summary>
/// Software-only implementation of <see cref="ITpmKeyProvisioner"/>.
/// Returns <see langword="null"/> to signal callers to use the AES-256 KEK from <c>MasterKekProvider</c>.
/// Throws <see cref="NotSupportedException"/> if <see cref="TpmFallbackPolicy.TpmEnabled"/> is <see langword="true"/>
/// but no hardware TPM is present — guards against misconfiguration.
/// </summary>
public sealed class KekFallbackProvisioner(TpmFallbackPolicy policy) : ITpmKeyProvisioner
{
    /// <inheritdoc/>
    public bool IsTpmAvailable => false;  // Stub: no hardware TPM in this environment.

    /// <inheritdoc/>
    public Task<byte[]?> TryProvisionKeyAsync(Guid tenantId, Guid executionId, CancellationToken ct)
    {
        if (policy.TpmEnabled)
            throw new NotSupportedException(
                "TpmFallbackPolicy.TpmEnabled is true, but no hardware TPM is available in this environment. " +
                "Disable TpmEnabled or deploy to a TPM 2.0 host.");

        // null → caller uses AES-256 KEK from MasterKekProvider.
        return Task.FromResult<byte[]?>(null);
    }
}
