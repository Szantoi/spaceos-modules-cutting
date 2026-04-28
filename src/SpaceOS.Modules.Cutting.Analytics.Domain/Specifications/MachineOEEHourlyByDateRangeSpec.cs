using Ardalis.Specification;
using SpaceOS.Modules.Cutting.Analytics.Domain.ReadModels;

namespace SpaceOS.Modules.Cutting.Analytics.Domain.Specifications;

/// <summary>Filters <see cref="MachineOEEHourly"/> by tenant, UTC time range, and optional machine.</summary>
public sealed class MachineOEEHourlyByDateRangeSpec : Specification<MachineOEEHourly>
{
    /// <param name="tenantId">Owning tenant.</param>
    /// <param name="from">Inclusive UTC start.</param>
    /// <param name="to">Inclusive UTC end.</param>
    /// <param name="machineId">Optional machine filter; null means all machines.</param>
    public MachineOEEHourlyByDateRangeSpec(Guid tenantId, DateTime from, DateTime to, string? machineId)
    {
        Query.Where(m => m.TenantId == tenantId && m.HourSlot >= from && m.HourSlot <= to);
        if (machineId is not null) Query.Where(m => m.MachineId == machineId);
        Query.OrderBy(m => m.HourSlot).ThenBy(m => m.MachineId);
    }
}
