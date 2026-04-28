using Ardalis.Specification;
using SpaceOS.Modules.Cutting.Analytics.Domain.ReadModels;

namespace SpaceOS.Modules.Cutting.Analytics.Domain.Specifications;

/// <summary>Filters <see cref="DailyMaterialUsage"/> by tenant, date range, and optional material code.</summary>
public sealed class DailyMaterialUsageByDateRangeSpec : Specification<DailyMaterialUsage>
{
    /// <param name="tenantId">Owning tenant.</param>
    /// <param name="from">Inclusive start date.</param>
    /// <param name="to">Inclusive end date.</param>
    /// <param name="materialCode">Optional material filter; null means all materials.</param>
    public DailyMaterialUsageByDateRangeSpec(Guid tenantId, DateOnly from, DateOnly to, string? materialCode)
    {
        Query.Where(m => m.TenantId == tenantId && m.UsageDate >= from && m.UsageDate <= to);
        if (materialCode is not null) Query.Where(m => m.MaterialCode == materialCode);
        Query.OrderBy(m => m.UsageDate).ThenBy(m => m.MaterialCode);
    }
}
