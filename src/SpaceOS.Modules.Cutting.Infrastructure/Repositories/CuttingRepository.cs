using Microsoft.EntityFrameworkCore;
using SpaceOS.Modules.Cutting.Domain.Aggregates;
using SpaceOS.Modules.Cutting.Domain.Enums;
using SpaceOS.Modules.Cutting.Domain.Interfaces;
using SpaceOS.Modules.Cutting.Infrastructure.Persistence;

namespace SpaceOS.Modules.Cutting.Infrastructure.Repositories;

public class CuttingRepository : ICuttingRepository
{
    private readonly CuttingDbContext _db;

    public CuttingRepository(CuttingDbContext db)
    {
        _db = db;
    }

    public async Task AddCuttingSheetAsync(CuttingSheet sheet, CancellationToken ct = default)
        => await _db.CuttingSheets.AddAsync(sheet, ct).ConfigureAwait(false);

    public async Task<CuttingSheet?> GetCuttingSheetByIdAsync(Guid id, CancellationToken ct = default)
        => await _db.CuttingSheets.AsNoTracking()
            .Include(s => s.Lines)
            .FirstOrDefaultAsync(s => s.Id == id, ct)
            .ConfigureAwait(false);

    public async Task<IReadOnlyList<CuttingSheet>> GetCuttingSheetsByTenantAsync(CancellationToken ct = default)
        => await _db.CuttingSheets.AsNoTracking()
            .Include(s => s.Lines)
            .ToListAsync(ct)
            .ConfigureAwait(false);

    public async Task AddDailyCuttingPlanAsync(DailyCuttingPlan plan, CancellationToken ct = default)
        => await _db.DailyCuttingPlans.AddAsync(plan, ct).ConfigureAwait(false);

    public async Task<DailyCuttingPlan?> GetDailyCuttingPlanByDateAsync(DateTime planDate, CancellationToken ct = default)
        => await _db.DailyCuttingPlans.AsNoTracking()
            .Include(p => p.Batches)
            .FirstOrDefaultAsync(p => p.PlanDate == planDate.Date, ct)
            .ConfigureAwait(false);

    public async Task AddCuttingExecutionAsync(CuttingExecution execution, CancellationToken ct = default)
        => await _db.CuttingExecutions.AddAsync(execution, ct).ConfigureAwait(false);

    public async Task<CuttingExecution?> GetExecutionBySheetIdAsync(Guid sheetId, CancellationToken ct = default)
        => await _db.CuttingExecutions.AsNoTracking()
            .FirstOrDefaultAsync(e => e.CuttingSheetId == sheetId, ct)
            .ConfigureAwait(false);

    public async Task<IReadOnlyList<CuttingExecution>> GetCompletedExecutionsInRangeAsync(DateTime from, DateTime to, CancellationToken ct = default)
        => await _db.CuttingExecutions.AsNoTracking()
            .Where(e => e.Status == ExecutionStatus.Completed && e.CompletedAt >= from && e.CompletedAt <= to)
            .ToListAsync(ct)
            .ConfigureAwait(false);

    public async Task SaveChangesAsync(CancellationToken ct = default)
        => await _db.SaveChangesAsync(ct).ConfigureAwait(false);
}
