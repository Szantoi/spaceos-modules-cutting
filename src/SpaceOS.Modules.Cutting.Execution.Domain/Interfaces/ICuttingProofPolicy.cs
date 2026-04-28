using SpaceOS.Modules.Cutting.Execution.Domain.Enums;
using SpaceOS.Modules.Cutting.Execution.Domain.ValueObjects;

namespace SpaceOS.Modules.Cutting.Execution.Domain.Interfaces;

/// <summary>Port for evaluating completion proof requirements per tenant.</summary>
public interface ICuttingProofPolicy
{
    ProofLevel MinimumLevel(Guid tenantId);
    bool IsValid(CompletionProof proof, Guid tenantId);
}
