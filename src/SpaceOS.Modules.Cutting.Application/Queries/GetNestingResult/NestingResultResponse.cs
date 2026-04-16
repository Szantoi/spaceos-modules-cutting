namespace SpaceOS.Modules.Cutting.Application.Queries.GetNestingResult;

public sealed record NestingResultResponse(
    Guid SheetId,
    string OrderReference,
    IReadOnlyList<NestingGroupResponse> Groups,
    int TotalParts,
    IReadOnlyList<PanelAssignmentResponse>? PanelAssignments = null);

public sealed record PanelAssignmentResponse(
    Guid PanelStockId,
    string MaterialType,
    int PanelWidthMm,
    int PanelHeightMm,
    IReadOnlyList<PlacedPartResponse> PlacedParts,
    int WasteAreaMm2,
    decimal UtilizationPercent);

public sealed record PlacedPartResponse(
    string PartName,
    int X,
    int Y,
    int WidthMm,
    int HeightMm,
    bool IsRotated);

public sealed record NestingGroupResponse(
    string MaterialType,
    decimal ThicknessMm,
    IReadOnlyList<NestingLineResponse> Lines);

public sealed record NestingLineResponse(
    string PartName,
    decimal WidthMm,
    decimal HeightMm,
    int Quantity);
