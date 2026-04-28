namespace SpaceOS.Modules.Cutting.Analytics.Domain.Interfaces;

/// <summary>Projects waste/consumption events into <c>DailyMaterialUsage</c> read-models.</summary>
public interface IMaterialUsageProjector
{
    /// <summary>Upserts the metric row for the given material/date; idempotent via event dedup.</summary>
    Task ProjectAsync(
        Guid tenantId, string materialCode, DateOnly date,
        decimal totalAreaMm2, decimal wasteAreaMm2, int offcutCount,
        Guid eventId, CancellationToken ct);
}
