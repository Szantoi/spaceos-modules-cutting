namespace SpaceOS.Modules.Cutting.Application.Queries.GetExecutionStatus;

public sealed record ExecutionStatusResponse(
    Guid SheetId,
    string Status,
    DateTime? StartedAt,
    DateTime? CompletedAt,
    decimal WasteAreaCm2);
