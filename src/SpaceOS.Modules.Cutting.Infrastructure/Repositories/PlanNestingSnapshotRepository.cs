using Microsoft.EntityFrameworkCore;
using SpaceOS.Modules.Cutting.Domain.Entities;
using SpaceOS.Modules.Cutting.Domain.Interfaces;
using SpaceOS.Modules.Cutting.Infrastructure.Persistence;

namespace SpaceOS.Modules.Cutting.Infrastructure.Repositories;

internal sealed class PlanNestingSnapshotRepository : IPlanNestingSnapshotRepository
{
    private readonly CuttingDbContext _db;

    public PlanNestingSnapshotRepository(CuttingDbContext db) => _db = db;

    public async Task<PlanNestingSnapshot?> GetByPlanAsync(Guid planId, CancellationToken ct)
        => await _db.PlanNestingSnapshots
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.CuttingPlanId == planId, ct)
            .ConfigureAwait(false);

    public async Task AddAsync(PlanNestingSnapshot snapshot, CancellationToken ct)
        => await _db.PlanNestingSnapshots.AddAsync(snapshot, ct).ConfigureAwait(false);

    public async Task SaveChangesAsync(CancellationToken ct)
        => await _db.SaveChangesAsync(ct).ConfigureAwait(false);
}
