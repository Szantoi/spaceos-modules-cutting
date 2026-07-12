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

    // CuttingPlan (planning aggregate)
    Task AddCuttingPlanAsync(CuttingPlan plan, CancellationToken ct = default);
    Task<CuttingPlan?> GetCuttingPlanByIdAsync(Guid planId, CancellationToken ct = default);
    Task<CuttingPlan?> GetCuttingPlanTrackedAsync(Guid planId, CancellationToken ct = default);
    Task<IReadOnlyList<CuttingPlan>> GetAllCuttingPlansAsync(CancellationToken ct = default);
    Task<CuttingJob?> GetCuttingJobTrackedAsync(Guid jobId, CancellationToken ct = default);

    /// <summary>Returns all Open DaySlots whose SlotDate is before <paramref name="date"/>.</summary>
    Task<IReadOnlyList<DaySlot>> GetOpenSlotsBeforeDateAsync(DateOnly date, CancellationToken ct = default);

    /// <summary>Returns all Open DaySlots ordered by SlotDate ascending (for job scheduling).</summary>
    Task<IReadOnlyList<DaySlot>> GetOpenSlotsOrderedByDateAsync(CancellationToken ct = default);

    /// <summary>Checks if any CuttingJobs already exist for the given OrderId.</summary>
    Task<bool> HasJobsForOrderAsync(Guid orderId, CancellationToken ct = default);

    // BatchAssignment
    /// <summary>Adds a new BatchAssignment record.</summary>
    Task AddBatchAssignmentAsync(BatchAssignment assignment, CancellationToken ct = default);

    /// <summary>Returns a BatchAssignment if one already exists for the given batch and date.</summary>
    Task<BatchAssignment?> GetBatchAssignmentAsync(Guid batchId, DateOnly planDate, CancellationToken ct = default);

    /// <summary>Gets a CuttingBatch by its ID (includes validation that it exists).</summary>
    Task<CuttingBatch?> GetCuttingBatchByIdAsync(Guid batchId, CancellationToken ct = default);

    // PublicQuoteRequest (Q3 Track A - MSG-BACKEND-078)
    /// <summary>Adds a new PublicQuoteRequest (public API quote request).</summary>
    Task AddPublicQuoteRequestAsync(PublicQuoteRequest quoteRequest, CancellationToken ct = default);

    /// <summary>Gets a PublicQuoteRequest by its ID.</summary>
    Task<PublicQuoteRequest?> GetPublicQuoteRequestByIdAsync(Guid id, CancellationToken ct = default);

    // PricingRule (Q3 Track B - MSG-BACKEND-031)
    /// <summary>Adds a new PricingRule.</summary>
    Task AddPricingRuleAsync(PricingRule pricingRule, CancellationToken ct = default);

    /// <summary>Gets a PricingRule by its ID (includes related entities: breakpoints, adjustments, surcharges).</summary>
    Task<PricingRule?> GetPricingRuleByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>Gets all active PricingRules for a specific supplier and product category.</summary>
    Task<IReadOnlyList<PricingRule>> GetActivePricingRulesBySupplerAndCategoryAsync(Guid supplierId, string productCategory, CancellationToken ct = default);

    Task SaveChangesAsync(CancellationToken ct = default);

    // Internal / test-reset operations
    Task<(int CuttingSheets, int DailyCuttingPlans)> DeleteByTenantAsync(Guid tenantId, CancellationToken ct = default);
}
