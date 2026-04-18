namespace SpaceOS.Modules.Cutting.Application.Queries.GetDailyCuttingPlan;

public sealed record DailyCuttingPlanResponse(
    Guid Id,
    string Name,
    DateTime PlanDate,
    string Status,
    IReadOnlyList<CuttingBatchResponse> Batches);

public sealed record CuttingBatchResponse(
    string MaterialType,
    decimal ThicknessMm,
    IReadOnlyList<Guid> SheetIds);
