using Ardalis.Specification;
using SpaceOS.Modules.Cutting.Analytics.Domain.Aggregates;
using SpaceOS.Modules.Cutting.Analytics.Domain.ValueObjects;

namespace SpaceOS.Modules.Cutting.Analytics.Domain.Specifications;

/// <summary>
/// Returns Pending or Running <see cref="AnalyticsRebuildJob"/> records for a tenant,
/// ordered by most recent request first.
/// </summary>
public sealed class AnalyticsRebuildJobActiveSpec : Specification<AnalyticsRebuildJob>
{
    /// <param name="tenantId">Owning tenant.</param>
    public AnalyticsRebuildJobActiveSpec(Guid tenantId)
    {
        Query.Where(j =>
            j.TenantId == tenantId &&
            (j.Status == RebuildJobStatus.Pending || j.Status == RebuildJobStatus.Running));
        Query.OrderByDescending(j => j.RequestedAt);
    }
}
