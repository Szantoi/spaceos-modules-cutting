namespace SpaceOS.Modules.Cutting.Execution.Application.DTOs;

public sealed record ExecutionDto(
    Guid Id,
    Guid TenantId,
    Guid SheetId,
    string Status,
    int PanelsCompleted,
    int TotalPanels,
    DateTime? StartedAt,
    DateTime? CompletedAt);

public sealed record ExecutionSummaryDto(
    Guid Id,
    string Status,
    DateTime ScheduledAt,
    int PanelsCompleted,
    int TotalPanels);

public sealed record ProgressEventDto(
    Guid EventId,
    string Kind,
    int? PanelNumber,
    DateTime OccurredAt);

public sealed record MilestoneDto(
    Guid MilestoneId,
    string Kind,
    string Status,
    DateTime? ReachedAt);

public sealed record CompletionProofDto(
    string Level,
    string ProofHash,
    DateTime CommittedAt);

public sealed record WorkerConsentStatusDto(
    Guid WorkerId,
    bool IsActive,
    DateTime? WithdrawnAt);
