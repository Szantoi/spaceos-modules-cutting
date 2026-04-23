namespace SpaceOS.Modules.Cutting.Domain.Entities;

/// <summary>
/// Snapshot of the nesting result for a CuttingPlan.
/// Persisted at nesting-time; consumed by RegisterOffcutsOnPlanFrozenHandler.
/// NestingResultJson stores serialized WastePieces and metrics.
/// </summary>
public sealed class PlanNestingSnapshot
{
    public Guid Id { get; private set; }
    public Guid CuttingPlanId { get; private set; }
    public Guid TenantId { get; private set; }
    public string NestingResultJson { get; private set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; private set; }

    private PlanNestingSnapshot() { }

    public static PlanNestingSnapshot Create(Guid planId, Guid tenantId, string nestingResultJson)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nestingResultJson, nameof(nestingResultJson));
        return new PlanNestingSnapshot
        {
            Id = Guid.NewGuid(),
            CuttingPlanId = planId,
            TenantId = tenantId,
            NestingResultJson = nestingResultJson,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }
}
