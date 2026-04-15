namespace SpaceOS.Modules.Cutting.Contracts.Dtos;

/// <summary>A complete cutting sheet submitted to the optimisation engine for nesting.</summary>
public sealed record CuttingSheetDto(
    Guid Id,
    Guid TenantId,
    Guid SourceOrderId,
    IReadOnlyList<CuttingLineDto> Lines,
    string MaterialType,
    DateTime CreatedAt);
