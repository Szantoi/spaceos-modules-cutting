using MediatR;
using SpaceOS.Modules.Cutting.Domain.Common;
using SpaceOS.Modules.Cutting.Execution.Domain.Enums;

namespace SpaceOS.Modules.Cutting.Execution.Domain.Events;

public sealed record CuttingExecutionScheduled(
    Guid ExecutionId, Guid TenantId, Guid SheetId, DateTime ScheduledAt) : IDomainEvent, INotification;

public sealed record CuttingExecutionStarted(
    Guid ExecutionId, Guid TenantId, Guid WorkerId, DateTime StartedAt) : IDomainEvent, INotification;

public sealed record PanelCompleted(
    Guid ExecutionId, Guid TenantId, int PanelNumber, int TotalPanels, DateTime CompletedAt) : IDomainEvent, INotification;

public sealed record ProgressRecorded(
    Guid ExecutionId, Guid TenantId, Guid EventId, ProgressEventKind Kind, DateTime OccurredAt) : IDomainEvent, INotification;

public sealed record OffcutReported(
    Guid ExecutionId, Guid TenantId, Guid OffcutId, decimal AreaMm2, DateTime OccurredAt) : IDomainEvent, INotification;

public sealed record MilestoneReached(
    Guid ExecutionId, Guid TenantId, Guid MilestoneId, MilestoneKind Kind, DateTime ReachedAt) : IDomainEvent, INotification;

public sealed record CuttingExecutionCompleted(
    Guid ExecutionId, Guid TenantId, Guid SheetId, ProofLevel ProofLevel, DateTime CompletedAt) : IDomainEvent, INotification;

public sealed record CuttingExecutionCancelled(
    Guid ExecutionId, Guid TenantId, CancelReason Reason, DateTime CancelledAt) : IDomainEvent, INotification;

public sealed record CompletionProofCommitted(
    Guid ExecutionId, Guid TenantId, ProofLevel Level, string ProofHash, DateTime CommittedAt) : IDomainEvent, INotification;

public sealed record WorkerConsentWithdrawalRequested(
    Guid TenantId, Guid WorkerId, ConsentScope Scope, DateTime RequestedAt) : IDomainEvent, INotification;

public sealed record WorkerConsentWithdrawalCompleted(
    Guid TenantId, Guid WorkerId, int PhotosReprocessed, DateTime CompletedAt) : IDomainEvent, INotification;

public sealed record CompletionProofRefreshedAfterConsentWithdrawal(
    Guid ExecutionId, Guid TenantId, string NewProofHash, DateTime RefreshedAt) : IDomainEvent, INotification;
