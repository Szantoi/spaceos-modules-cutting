using SpaceOS.Modules.Cutting.Domain.Entities;

namespace SpaceOS.Modules.Cutting.Domain.Interfaces;

public interface IPlanNestingSnapshotRepository
{
    Task<PlanNestingSnapshot?> GetByPlanAsync(Guid planId, CancellationToken ct);
    Task AddAsync(PlanNestingSnapshot snapshot, CancellationToken ct);
    Task SaveChangesAsync(CancellationToken ct);
}
