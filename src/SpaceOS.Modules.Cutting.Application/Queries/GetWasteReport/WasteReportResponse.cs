namespace SpaceOS.Modules.Cutting.Application.Queries.GetWasteReport;

public sealed record WasteReportResponse(
    decimal TotalWasteAreaCm2,
    decimal AverageWastePerExecution,
    int ExecutionCount);
