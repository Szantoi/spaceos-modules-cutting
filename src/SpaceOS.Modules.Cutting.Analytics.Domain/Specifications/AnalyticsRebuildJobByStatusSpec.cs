using Ardalis.Specification;
using SpaceOS.Modules.Cutting.Analytics.Domain.Aggregates;
using SpaceOS.Modules.Cutting.Analytics.Domain.ValueObjects;

namespace SpaceOS.Modules.Cutting.Analytics.Domain.Specifications;

/// <summary>
/// Filters <see cref="AnalyticsRebuildJob"/> by tenant and exact status,
/// ordered by most recent request first.
/// </summary>
public sealed class AnalyticsRebuildJobByStatusSpec : Specification<AnalyticsRebuildJob>
{
    /// <param name="tenantId">Owning tenant.</param>
    /// <param name="status">Required status to match.</param>
    public AnalyticsRebuildJobByStatusSpec(Guid tenantId, RebuildJobStatus status)
    {
        Query.Where(j => j.TenantId == tenantId && j.Status == status);
        Query.OrderByDescending(j => j.RequestedAt);
    }
}
