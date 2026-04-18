using SpaceOS.Modules.Cutting.Domain.Aggregates;

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

    Task SaveChangesAsync(CancellationToken ct = default);

    // Internal / test-reset operations
    Task<(int CuttingSheets, int DailyCuttingPlans)> DeleteByTenantAsync(Guid tenantId, CancellationToken ct = default);
}
