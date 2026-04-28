namespace SpaceOS.Modules.Cutting.Analytics.Infrastructure.Tpm;

/// <summary>
/// Controls whether hardware TPM key provisioning is attempted.
/// Defaults to <see langword="false"/> (software-KEK-only) — P4-9 tech-debt closure.
/// </summary>
public sealed class TpmFallbackPolicy
{
    /// <summary>
    /// When <see langword="false"/> (default), key provisioning uses the software AES-256 KEK path.
    /// Set to <see langword="true"/> only on hosts confirmed to have a TPM 2.0 chip.
    /// </summary>
    public bool TpmEnabled { get; init; } = false;

    /// <summary>The production-safe default: TPM disabled, software KEK active.</summary>
    public static TpmFallbackPolicy Default => new();
}
