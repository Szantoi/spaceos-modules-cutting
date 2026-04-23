using SpaceOS.Modules.Cutting.Domain.Aggregates;
using SpaceOS.Modules.Cutting.Domain.Entities;

namespace SpaceOS.Modules.Cutting.Domain.Interfaces;

public interface ICuttingRepository
{
    // CuttingSheet (append-only reads — no update)
    Task AddCuttingSheetAsync(CuttingSheet sheet, CancellationToken ct = default);
    Task<CuttingSheet?> GetCuttingSheetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<CuttingSheet>> GetCuttingSheetsByTenantAsync(CancellationToken ct = default);

    // DailyCuttingPlan
    Task AddDailyCuttingPlanAsync(DailyCuttingPlan plan, CancellationToken ct = default);
    Task<DailyCuttingPlan?> GetDailyCuttingPlanByDateAsync(DateTime planDate, CancellationToken ct = default);
    Task<IReadOnlyList<DailyCuttingPlan>> GetAllDailyCuttingPlansAsync(CancellationToken ct = default);

    // CuttingExecution
    Task AddCuttingExecutionAsync(CuttingExecution execution, CancellationToken ct = default);
    Task<CuttingExecution?> GetExecutionBySheetIdAsync(Guid sheetId, CancellationToken ct = default);
    Task<IReadOnlyList<CuttingExecution>> GetCompletedExecutionsInRangeAsync(DateTime from, DateTime to, CancellationToken ct = default);

    // CuttingPlan (planning aggregate)
    Task AddCuttingPlanAsync(CuttingPlan plan, CancellationToken ct = default);
    Task<CuttingPlan?> GetCuttingPlanByIdAsync(Guid planId, CancellationToken ct = default);
    Task<CuttingPlan?> GetCuttingPlanTrackedAsync(Guid planId, CancellationToken ct = default);
    Task<IReadOnlyList<CuttingPlan>> GetAllCuttingPlansAsync(CancellationToken ct = default);
    Task<CuttingJob?> GetCuttingJobTrackedAsync(Guid jobId, CancellationToken ct = default);

    /// <summary>Returns all Open DaySlots whose SlotDate is before <paramref name="date"/>.</summary>
    Task<IReadOnlyList<DaySlot>> GetOpenSlotsBeforeDateAsync(DateOnly date, CancellationToken ct = default);

    Task SaveChangesAsync(CancellationToken ct = default);

    // Internal / test-reset operations
    Task<(int CuttingSheets, int DailyCuttingPlans)> DeleteByTenantAsync(Guid tenantId, CancellationToken ct = default);
}
