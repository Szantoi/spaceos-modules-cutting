namespace SpaceOS.Modules.Cutting.Execution.Domain.Interfaces;

/// <summary>Port for checking whether progress recording is allowed without worker consent for a given tenant.</summary>
public interface ICuttingProgressPolicy
{
    bool AllowsProgressWithoutConsent(Guid tenantId);
}
