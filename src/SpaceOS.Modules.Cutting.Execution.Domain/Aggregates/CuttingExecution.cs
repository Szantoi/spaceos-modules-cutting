using Ardalis.Result;
using SpaceOS.Modules.Cutting.Domain.Common;
using SpaceOS.Modules.Cutting.Execution.Domain.Entities;
using SpaceOS.Modules.Cutting.Execution.Domain.Enums;
using SpaceOS.Modules.Cutting.Execution.Domain.Events;
using SpaceOS.Modules.Cutting.Execution.Domain.Interfaces;
using SpaceOS.Modules.Cutting.Execution.Domain.Services;
using SpaceOS.Modules.Cutting.Execution.Domain.ValueObjects;

namespace SpaceOS.Modules.Cutting.Execution.Domain.Aggregates;

/// <summary>
/// Aggregate root for a cutting execution — tracks a 7-state FSM lifecycle from scheduling through completion or cancellation.
/// </summary>
public sealed class CuttingExecution : AggregateRoot
{
    private readonly List<ProgressEvent> _progressEvents = new();
    private readonly List<OffcutReport> _offcutReports = new();
    private readonly List<MilestoneSubscription> _milestones = new();

    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public Guid? BatchId { get; private set; }
    public Guid SheetId { get; private set; }
    public WorkerAssignment WorkerAssignment { get; private set; } = null!;
    public string MachineId { get; private set; } = string.Empty;
    public ScheduleWindow ScheduleWindow { get; private set; } = null!;
    public int TotalPanels { get; private set; }
    public int PanelsCompleted { get; private set; }
    public CuttingExecutionStatus Status { get; private set; }
    public DateTime ScheduledAt { get; private set; }
    public DateTime? StartedAt { get; private set; }
    public DateTime? CompletedAt { get; private set; }
    public DateTime? CancelledAt { get; private set; }
    public CancelReason? CancelReason { get; private set; }
    public CompletionProof? CompletionProof { get; private set; }
    public decimal OffcutAreaMm2 { get; private set; }
    public decimal TotalAreaMm2 { get; private set; }
    public bool WorkerConsentActive { get; private set; } = true;
    public int? Priority { get; private set; }

    public IReadOnlyList<ProgressEvent> ProgressEvents => _progressEvents.AsReadOnly();
    public IReadOnlyList<OffcutReport> OffcutReports => _offcutReports.AsReadOnly();
    public IReadOnlyList<MilestoneSubscription> Milestones => _milestones.AsReadOnly();

    private CuttingExecution() { }

    // ── Factory ────────────────────────────────────────────────────────────────

    /// <summary>
    /// Creates a new scheduled execution. Returns <see cref="Result.Invalid"/> when any required field is empty or invalid.
    /// </summary>
    public static Result<CuttingExecution> Schedule(
        Guid sheetId,
        WorkerAssignment workerAssignment,
        string machineId,
        ScheduleWindow scheduleWindow,
        int totalPanels,
        Guid tenantId)
    {
        if (sheetId == Guid.Empty)
            return Result<CuttingExecution>.Invalid(new ValidationError("SheetId must not be empty."));
        ArgumentNullException.ThrowIfNull(workerAssignment);
        if (string.IsNullOrWhiteSpace(machineId))
            return Result<CuttingExecution>.Invalid(new ValidationError("MachineId must not be empty."));
        ArgumentNullException.ThrowIfNull(scheduleWindow);
        if (totalPanels <= 0)
            return Result<CuttingExecution>.Invalid(new ValidationError("TotalPanels must be positive."));
        if (tenantId == Guid.Empty)
            return Result<CuttingExecution>.Invalid(new ValidationError("TenantId must not be empty."));

        var now = DateTime.UtcNow;
        var execution = new CuttingExecution
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            SheetId = sheetId,
            WorkerAssignment = workerAssignment,
            MachineId = machineId,
            ScheduleWindow = scheduleWindow,
            TotalPanels = totalPanels,
            Status = CuttingExecutionStatus.Scheduled,
            ScheduledAt = now
        };

        execution.RaiseDomainEvent(new CuttingExecutionScheduled(execution.Id, tenantId, sheetId, now));
        return Result<CuttingExecution>.Success(execution);
    }

    /// <summary>
    /// Creates a new scheduled execution with batch assignment. Returns <see cref="Result.Invalid"/> when any required field is empty or invalid.
    /// </summary>
    public static Result<CuttingExecution> ScheduleWithBatchAssignment(
        Guid batchId,
        Guid sheetId,
        WorkerAssignment workerAssignment,
        string machineId,
        ScheduleWindow scheduleWindow,
        int totalPanels,
        int priority,
        Guid tenantId)
    {
        if (batchId == Guid.Empty)
            return Result<CuttingExecution>.Invalid(new ValidationError("BatchId must not be empty."));
        if (sheetId == Guid.Empty)
            return Result<CuttingExecution>.Invalid(new ValidationError("SheetId must not be empty."));
        ArgumentNullException.ThrowIfNull(workerAssignment);
        if (string.IsNullOrWhiteSpace(machineId))
            return Result<CuttingExecution>.Invalid(new ValidationError("MachineId must not be empty."));
        ArgumentNullException.ThrowIfNull(scheduleWindow);
        if (totalPanels <= 0)
            return Result<CuttingExecution>.Invalid(new ValidationError("TotalPanels must be positive."));
        if (priority < 1 || priority > 10)
            return Result<CuttingExecution>.Invalid(new ValidationError("Priority must be between 1 and 10."));
        if (tenantId == Guid.Empty)
            return Result<CuttingExecution>.Invalid(new ValidationError("TenantId must not be empty."));

        var now = DateTime.UtcNow;
        var execution = new CuttingExecution
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            BatchId = batchId,
            SheetId = sheetId,
            WorkerAssignment = workerAssignment,
            MachineId = machineId,
            ScheduleWindow = scheduleWindow,
            TotalPanels = totalPanels,
            Priority = priority,
            Status = CuttingExecutionStatus.Scheduled,
            ScheduledAt = now
        };

        execution.RaiseDomainEvent(new CuttingExecutionScheduled(execution.Id, tenantId, sheetId, now));
        return Result<CuttingExecution>.Success(execution);
    }

    // ── FSM transitions ────────────────────────────────────────────────────────

    /// <summary>
    /// Transitions Scheduled → Started after validating the worker badge HMAC.
    /// </summary>
    public Result Start(Guid workerId, WorkerEventHmac badgeHmac, IWorkerSecurityPolicy securityPolicy, DateTime clock)
    {
        if (Status != CuttingExecutionStatus.Scheduled)
            return Result.Invalid(new ValidationError($"Cannot start execution in status {Status}."));

        ArgumentNullException.ThrowIfNull(badgeHmac);
        ArgumentNullException.ThrowIfNull(securityPolicy);

        if (!securityPolicy.ValidateProgressEventHmac(workerId, Id, Guid.Empty, badgeHmac))
            return Result.Invalid(new ValidationError("Badge HMAC validation failed."));

        Status = CuttingExecutionStatus.Started;
        StartedAt = clock;
        RaiseDomainEvent(new CuttingExecutionStarted(Id, TenantId, workerId, clock));
        return Result.Success();
    }

    /// <summary>
    /// Records a progress event. Idempotent on duplicate eventId. Transitions Started → InProgress on first event.
    /// </summary>
    public Result RecordProgress(
        Guid eventId,
        ProgressEventKind kind,
        int? panel,
        DateTime occurredAt,
        WorkerEventHmac eventHmac,
        IWorkerSecurityPolicy securityPolicy,
        DateTime clock)
    {
        if (Status is not (CuttingExecutionStatus.Started or CuttingExecutionStatus.InProgress))
            return Result.Invalid(new ValidationError($"Cannot record progress in status {Status}."));

        ArgumentNullException.ThrowIfNull(eventHmac);
        ArgumentNullException.ThrowIfNull(securityPolicy);

        // Idempotency — if already recorded, return success silently
        if (_progressEvents.Any(e => e.EventId == eventId))
            return Result.Success();

        if (!securityPolicy.ValidateProgressEventHmac(WorkerAssignment.WorkerId, Id, eventId, eventHmac))
            return Result.Invalid(new ValidationError("Progress event HMAC validation failed."));

        var progressEvent = ProgressEvent.Create(eventId, kind, panel, occurredAt, eventHmac.Value);
        _progressEvents.Add(progressEvent);

        if (Status == CuttingExecutionStatus.Started)
            Status = CuttingExecutionStatus.InProgress;

        if (kind == ProgressEventKind.PanelCompleted && panel.HasValue)
        {
            PanelsCompleted++;
            RaiseDomainEvent(new PanelCompleted(Id, TenantId, panel.Value, TotalPanels, clock));
        }

        RaiseDomainEvent(new ProgressRecorded(Id, TenantId, eventId, kind, occurredAt));
        return Result.Success();
    }

    /// <summary>
    /// Records an offcut produced during execution. Only allowed in Started or InProgress states.
    /// </summary>
    public Result RecordOffcut(OffcutEvent offcutEvent, DateTime clock)
    {
        if (Status is not (CuttingExecutionStatus.Started or CuttingExecutionStatus.InProgress))
            return Result.Invalid(new ValidationError($"Cannot record offcut in status {Status}."));

        ArgumentNullException.ThrowIfNull(offcutEvent);

        var offcutId = Guid.NewGuid();
        var report = OffcutReport.Create(offcutId, offcutEvent.MaterialId, offcutEvent.WidthMm, offcutEvent.HeightMm, clock);
        _offcutReports.Add(report);
        OffcutAreaMm2 += offcutEvent.AreaMm2;

        RaiseDomainEvent(new OffcutReported(Id, TenantId, offcutId, offcutEvent.AreaMm2, clock));
        return Result.Success();
    }

    /// <summary>
    /// Transitions InProgress → Completed, validates all panels done and proof meets policy.
    /// </summary>
    public Result Complete(CompletionProof proof, ICuttingProofPolicy proofPolicy, DateTime clock)
    {
        if (Status != CuttingExecutionStatus.InProgress)
            return Result.Invalid(new ValidationError($"Cannot complete execution in status {Status}."));

        ArgumentNullException.ThrowIfNull(proof);
        ArgumentNullException.ThrowIfNull(proofPolicy);

        if (PanelsCompleted < TotalPanels)
            return Result.Invalid(new ValidationError($"Cannot complete: {PanelsCompleted}/{TotalPanels} panels done."));

        if (!proofPolicy.IsValid(proof, TenantId))
            return Result.Invalid(new ValidationError("Completion proof does not meet the required level for this tenant."));

        CompletionProof = proof;
        Status = CuttingExecutionStatus.Completed;
        CompletedAt = clock;

        RaiseDomainEvent(new CuttingExecutionCompleted(Id, TenantId, SheetId, proof.Level, clock));
        RaiseDomainEvent(new CompletionProofCommitted(Id, TenantId, proof.Level, proof.ProofHash, clock));
        return Result.Success();
    }

    /// <summary>
    /// Cancels the execution from any non-terminal state.
    /// </summary>
    public Result Cancel(CancelReason reason, DateTime clock)
    {
        if (Status is CuttingExecutionStatus.Completed or CuttingExecutionStatus.Cancelled or CuttingExecutionStatus.Failed)
            return Result.Invalid(new ValidationError($"Cannot cancel execution in terminal status {Status}."));

        Status = CuttingExecutionStatus.Cancelled;
        CancelReason = reason;
        CancelledAt = clock;

        RaiseDomainEvent(new CuttingExecutionCancelled(Id, TenantId, reason, clock));
        return Result.Success();
    }

    /// <summary>
    /// Pure evaluation: runs each predicate and raises MilestoneReached for newly-met milestones.
    /// Does not persist state changes — caller must save.
    /// </summary>
    public Result EvaluateMilestones(IEnumerable<IMilestonePredicate> predicates, DateTime clock)
    {
        ArgumentNullException.ThrowIfNull(predicates);

        foreach (var predicate in predicates)
        {
            var subscription = _milestones.FirstOrDefault(m => m.Kind == predicate.Kind && m.Status == MilestoneStatus.Pending);
            if (subscription is null) continue;

            if (predicate.Evaluate(this, clock))
            {
                subscription.MarkMet(clock);
                RaiseDomainEvent(new MilestoneReached(Id, TenantId, subscription.MilestoneId, subscription.Kind, clock));
            }
        }
        return Result.Success();
    }

    /// <summary>Adds a milestone subscription to track.</summary>
    public void AddMilestone(Guid milestoneId, MilestoneKind kind, string configJson, int configVersion)
    {
        _milestones.Add(MilestoneSubscription.Create(milestoneId, kind, configJson, configVersion));
    }

    /// <summary>Sets the total material area (used for quality-check ratio).</summary>
    public void SetTotalAreaMm2(decimal totalAreaMm2)
    {
        if (totalAreaMm2 >= 0)
            TotalAreaMm2 = totalAreaMm2;
    }

    /// <summary>Marks worker consent as withdrawn (used during GDPR withdrawal flow).</summary>
    public void WithdrawWorkerConsent() => WorkerConsentActive = false;
}
