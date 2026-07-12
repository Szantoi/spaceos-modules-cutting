using SpaceOS.Modules.Cutting.Execution.Domain.Enums;
using SpaceOS.Modules.Cutting.Execution.Domain.Interfaces;
using SpaceOS.Modules.Cutting.Execution.Domain.ValueObjects;

namespace SpaceOS.Modules.Cutting.Execution.Infrastructure.Security;

/// <summary>
/// Stub implementation of ICuttingProofPolicy (DEV/TEST only).
/// TODO: Replace with real proof validation in production.
/// </summary>
internal sealed class NullCuttingProofPolicy : ICuttingProofPolicy
{
    public ProofLevel MinimumLevel(Guid tenantId)
    {
        // Stub: minimum proof level
        return ProofLevel.HashOnly;
    }

    public bool IsValid(CompletionProof proof, Guid tenantId)
    {
        // Stub: always valid
        return true;
    }
}
