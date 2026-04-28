using Ardalis.Specification;
using SpaceOS.Modules.Cutting.Analytics.Domain.ReadModels;

namespace SpaceOS.Modules.Cutting.Analytics.Domain.Specifications;

/// <summary>
/// Filters <see cref="DailyOperatorMetric"/> by tenant and date range — includes suppressed records.
/// Internal use only (projector lookups). Use <see cref="DailyOperatorMetricAnonymizedSpec"/> for API responses.
/// </summary>
public sealed class DailyOperatorMetricByDateRangeSpec : Specification<DailyOperatorMetric>
{
    /// <param name="tenantId">Owning tenant.</param>
    /// <param name="from">Inclusive start date.</param>
    /// <param name="to">Inclusive end date.</param>
    public DailyOperatorMetricByDateRangeSpec(Guid tenantId, DateOnly from, DateOnly to)
    {
        Query.Where(m => m.TenantId == tenantId && m.MetricDate >= from && m.MetricDate <= to);
        Query.OrderBy(m => m.MetricDate);
    }
}
