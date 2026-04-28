using Ardalis.Specification;
using SpaceOS.Modules.Cutting.Analytics.Domain.ReadModels;

namespace SpaceOS.Modules.Cutting.Analytics.Domain.Specifications;

/// <summary>Filters <see cref="DailyExecutionMetric"/> by tenant, date range, and optional machine.</summary>
public sealed class DailyExecutionMetricByDateRangeSpec : Specification<DailyExecutionMetric>
{
    /// <param name="tenantId">Owning tenant.</param>
    /// <param name="from">Inclusive start date.</param>
    /// <param name="to">Inclusive end date.</param>
    /// <param name="machineId">Optional machine filter; null means all machines.</param>
    public DailyExecutionMetricByDateRangeSpec(Guid tenantId, DateOnly from, DateOnly to, string? machineId)
    {
        Query.Where(m => m.TenantId == tenantId && m.MetricDate >= from && m.MetricDate <= to);
        if (machineId is not null) Query.Where(m => m.MachineId == machineId);
        Query.OrderBy(m => m.MetricDate).ThenBy(m => m.MachineId);
    }
}
