namespace SpaceOS.Modules.Cutting.Contracts.Dtos;

/// <summary>Current execution status of a cutting sheet on the machine.</summary>
public sealed record CuttingExecutionDto(
    Guid SheetId,
    string Status,
    DateTime? StartedAt,
    DateTime? CompletedAt,
    string? ErrorMessage);
