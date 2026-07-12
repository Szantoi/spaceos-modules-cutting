using Microsoft.EntityFrameworkCore;
using SpaceOS.Modules.Cutting.Domain.Aggregates;
using SpaceOS.Modules.Cutting.Domain.Entities;
using SpaceOS.Modules.Cutting.Domain.Enums;
using SpaceOS.Modules.Cutting.Domain.Interfaces;
using SpaceOS.Modules.Cutting.Domain.ValueObjects;
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

    public async Task<IReadOnlyList<DailyCuttingPlan>> GetAllDailyCuttingPlansAsync(CancellationToken ct = default)
        => await _db.DailyCuttingPlans.AsNoTracking()
            .Include(p => p.Batches)
            .OrderByDescending(p => p.PlanDate)
            .ToListAsync(ct)
            .ConfigureAwait(false);

    public async Task AddCuttingPlanAsync(CuttingPlan plan, CancellationToken ct = default)
        => await _db.CuttingPlans.AddAsync(plan, ct).ConfigureAwait(false);

    public async Task<CuttingPlan?> GetCuttingPlanByIdAsync(Guid planId, CancellationToken ct = default)
        => await _db.CuttingPlans.AsNoTracking()
            .Include(p => p.DaySlots)
                .ThenInclude(d => d.Jobs)
            .FirstOrDefaultAsync(p => p.Id == planId, ct)
            .ConfigureAwait(false);

    public async Task<CuttingPlan?> GetCuttingPlanTrackedAsync(Guid planId, CancellationToken ct = default)
        => await _db.CuttingPlans
            .FirstOrDefaultAsync(p => p.Id == planId, ct)
            .ConfigureAwait(false);

    public async Task<IReadOnlyList<CuttingPlan>> GetAllCuttingPlansAsync(CancellationToken ct = default)
        => await _db.CuttingPlans.AsNoTracking()
            .Include(p => p.DaySlots)
            .OrderByDescending(p => p.PlanDate)
            .ToListAsync(ct)
            .ConfigureAwait(false);

    public async Task<CuttingJob?> GetCuttingJobTrackedAsync(Guid jobId, CancellationToken ct = default)
        => await _db.CuttingJobs
            .FirstOrDefaultAsync(j => j.Id == jobId, ct)
            .ConfigureAwait(false);

    public async Task<IReadOnlyList<DaySlot>> GetOpenSlotsBeforeDateAsync(DateOnly date, CancellationToken ct = default)
        => await _db.DaySlots
            .Where(s => s.Status == DaySlotStatus.Open && s.SlotDate < date)
            .ToListAsync(ct)
            .ConfigureAwait(false);

    public async Task<IReadOnlyList<DaySlot>> GetOpenSlotsOrderedByDateAsync(CancellationToken ct = default)
        => await _db.DaySlots
            .Include(s => s.Jobs)
            .Where(s => s.Status == DaySlotStatus.Open)
            .OrderBy(s => s.SlotDate)
            .ToListAsync(ct)
            .ConfigureAwait(false);

    public async Task<bool> HasJobsForOrderAsync(Guid orderId, CancellationToken ct = default)
        => await _db.CuttingJobs
            .AnyAsync(j => j.OrderId == orderId, ct)
            .ConfigureAwait(false);

    public async Task AddBatchAssignmentAsync(BatchAssignment assignment, CancellationToken ct = default)
        => await _db.BatchAssignments.AddAsync(assignment, ct).ConfigureAwait(false);

    public async Task<BatchAssignment?> GetBatchAssignmentAsync(Guid batchId, DateOnly planDate, CancellationToken ct = default)
        => await _db.BatchAssignments.AsNoTracking()
            .FirstOrDefaultAsync(a => a.BatchId == batchId && a.PlanDate == planDate, ct)
            .ConfigureAwait(false);

    public async Task<CuttingBatch?> GetCuttingBatchByIdAsync(Guid batchId, CancellationToken ct = default)
        => await _db.CuttingBatches.AsNoTracking()
            .FirstOrDefaultAsync(b => b.Id == batchId, ct)
            .ConfigureAwait(false);

    // PublicQuoteRequest (Q3 Track A - MSG-BACKEND-078)
    public async Task AddPublicQuoteRequestAsync(PublicQuoteRequest quoteRequest, CancellationToken ct = default)
        => await _db.PublicQuoteRequests.AddAsync(quoteRequest, ct).ConfigureAwait(false);

    public async Task<PublicQuoteRequest?> GetPublicQuoteRequestByIdAsync(Guid id, CancellationToken ct = default)
        => await _db.PublicQuoteRequests.AsNoTracking()
            .FirstOrDefaultAsync(q => q.Id == id, ct)
            .ConfigureAwait(false);

    // PricingRule (Q3 Track B - MSG-BACKEND-031)
    public async Task AddPricingRuleAsync(PricingRule pricingRule, CancellationToken ct = default)
        => await _db.PricingRules.AddAsync(pricingRule, ct).ConfigureAwait(false);

    public async Task<PricingRule?> GetPricingRuleByIdAsync(Guid id, CancellationToken ct = default)
        => await _db.PricingRules.AsNoTracking()
            .Include(pr => pr.QuantityBreakpoints)
            .Include(pr => pr.LeadTimeAdjustments)
            .Include(pr => pr.MaterialSurcharges)
            .FirstOrDefaultAsync(pr => pr.Id == id, ct)
            .ConfigureAwait(false);

    public async Task<IReadOnlyList<PricingRule>> GetActivePricingRulesBySupplerAndCategoryAsync(Guid supplierId, string productCategory, CancellationToken ct = default)
        => await _db.PricingRules.AsNoTracking()
            .Include(pr => pr.QuantityBreakpoints)
            .Include(pr => pr.LeadTimeAdjustments)
            .Include(pr => pr.MaterialSurcharges)
            .Where(pr => pr.SupplierId == supplierId
                && pr.ProductCategory == productCategory
                && pr.Status == PricingRuleStatus.Active)
            .ToListAsync(ct)
            .ConfigureAwait(false);

    public async Task SaveChangesAsync(CancellationToken ct = default)
        => await _db.SaveChangesAsync(ct).ConfigureAwait(false);

    public async Task<(int CuttingSheets, int DailyCuttingPlans)> DeleteByTenantAsync(Guid tenantId, CancellationToken ct = default)
    {
        var plans = await _db.DailyCuttingPlans
            .Where(p => p.TenantId == tenantId)
            .ToListAsync(ct).ConfigureAwait(false);
        _db.DailyCuttingPlans.RemoveRange(plans);

        var sheets = await _db.CuttingSheets
            .Where(s => s.TenantId == tenantId)
            .ToListAsync(ct).ConfigureAwait(false);
        _db.CuttingSheets.RemoveRange(sheets);

        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
        return (sheets.Count, plans.Count);
    }
}
