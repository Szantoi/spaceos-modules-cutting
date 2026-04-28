namespace SpaceOS.Modules.Cutting.Domain.Entities;

/// <summary>
/// Snapshot of the nesting result for a CuttingPlan.
/// Persisted at publish/nesting-time; consumed by RegisterOffcutsOnPlanFrozenHandler.
/// </summary>
public sealed class PlanNestingSnapshot
{
    public Guid Id { get; private set; }
    public Guid CuttingPlanId { get; private set; }
    public Guid TenantId { get; private set; }
    public string NestingResultJson { get; private set; } = string.Empty;
    public string PlacementsJson { get; private set; } = string.Empty;
    public decimal YieldPercent { get; private set; }
    public long WasteAreaMm2 { get; private set; }
    public string Algorithm { get; private set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; private set; }

    private PlanNestingSnapshot() { }

    public static PlanNestingSnapshot Create(
        Guid planId,
        Guid tenantId,
        string nestingResultJson,
        string placementsJson = "",
        decimal yieldPercent = 0m,
        long wasteAreaMm2 = 0,
        string algorithm = "")
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nestingResultJson, nameof(nestingResultJson));
        return new PlanNestingSnapshot
        {
            Id = Guid.NewGuid(),
            CuttingPlanId = planId,
            TenantId = tenantId,
            NestingResultJson = nestingResultJson,
            PlacementsJson = placementsJson ?? string.Empty,
            YieldPercent = yieldPercent,
            WasteAreaMm2 = wasteAreaMm2,
            Algorithm = algorithm ?? string.Empty,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }
}
