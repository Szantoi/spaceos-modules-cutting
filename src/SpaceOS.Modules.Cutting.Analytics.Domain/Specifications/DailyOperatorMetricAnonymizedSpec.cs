using Ardalis.Specification;
using SpaceOS.Modules.Cutting.Analytics.Domain.ReadModels;

namespace SpaceOS.Modules.Cutting.Analytics.Domain.Specifications;

/// <summary>
/// Filters <see cref="DailyOperatorMetric"/> by tenant and date range, excluding suppressed records.
/// Safe for API responses — privacy-preserving view.
/// </summary>
public sealed class DailyOperatorMetricAnonymizedSpec : Specification<DailyOperatorMetric>
{
    /// <param name="tenantId">Owning tenant.</param>
    /// <param name="from">Inclusive start date.</param>
    /// <param name="to">Inclusive end date.</param>
    public DailyOperatorMetricAnonymizedSpec(Guid tenantId, DateOnly from, DateOnly to)
    {
        Query.Where(m => m.TenantId == tenantId && m.MetricDate >= from && m.MetricDate <= to && !m.IsSuppressed);
        Query.OrderBy(m => m.MetricDate);
    }
}
