using FluentAssertions;
using Moq;
using SpaceOS.Modules.Cutting.Execution.Domain.Aggregates;
using SpaceOS.Modules.Cutting.Execution.Domain.Enums;
using SpaceOS.Modules.Cutting.Execution.Domain.Events;
using SpaceOS.Modules.Cutting.Execution.Domain.Interfaces;
using SpaceOS.Modules.Cutting.Execution.Domain.ValueObjects;
using Xunit;

namespace SpaceOS.Modules.Cutting.Tests.Execution;

public class CuttingExecutionFsmTests
{
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly Guid SheetId = Guid.NewGuid();

    private static WorkerAssignment ValidWorker() =>
        WorkerAssignment.Create(Guid.NewGuid(), Guid.NewGuid()).Value;

    private static ScheduleWindow ValidWindow() =>
        ScheduleWindow.Create(DateTime.UtcNow, DateTime.UtcNow.AddHours(4)).Value;

    private static CuttingExecution ScheduledExecution(int totalPanels = 5)
    {
        var result = CuttingExecution.Schedule(SheetId, ValidWorker(), "M-01", ValidWindow(), totalPanels, TenantId);
        result.IsSuccess.Should().BeTrue();
        return result.Value;
    }

    private static IWorkerSecurityPolicy AlwaysValidPolicy()
    {
        var mock = new Mock<IWorkerSecurityPolicy>();
        mock.Setup(p => p.ValidateProgressEventHmac(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<WorkerEventHmac>()))
            .Returns(true);
        return mock.Object;
    }

    private static IWorkerSecurityPolicy AlwaysInvalidPolicy()
    {
        var mock = new Mock<IWorkerSecurityPolicy>();
        mock.Setup(p => p.ValidateProgressEventHmac(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<WorkerEventHmac>()))
            .Returns(false);
        return mock.Object;
    }

    private static ICuttingProofPolicy AlwaysValidProofPolicy()
    {
        var mock = new Mock<ICuttingProofPolicy>();
        mock.Setup(p => p.IsValid(It.IsAny<CompletionProof>(), It.IsAny<Guid>())).Returns(true);
        mock.Setup(p => p.MinimumLevel(It.IsAny<Guid>())).Returns(ProofLevel.HashOnly);
        return mock.Object;
    }

    private static WorkerEventHmac ValidHmac() =>
        WorkerEventHmac.Create(Convert.ToBase64String(new byte[32]), "v1").Value;

    // ── Schedule ──────────────────────────────────────────────────────────────

    [Fact]
    public void Schedule_WithValidData_ReturnsSuccess()
    {
        var result = CuttingExecution.Schedule(SheetId, ValidWorker(), "M-01", ValidWindow(), 5, TenantId);

        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be(CuttingExecutionStatus.Scheduled);
    }

    [Fact]
    public void Schedule_WithEmptyTenantId_ReturnsFail()
    {
        var result = CuttingExecution.Schedule(SheetId, ValidWorker(), "M-01", ValidWindow(), 5, Guid.Empty);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public void Schedule_WithEmptySheetId_ReturnsFail()
    {
        var result = CuttingExecution.Schedule(Guid.Empty, ValidWorker(), "M-01", ValidWindow(), 5, TenantId);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public void Schedule_WithZeroTotalPanels_ReturnsFail()
    {
        var result = CuttingExecution.Schedule(SheetId, ValidWorker(), "M-01", ValidWindow(), 0, TenantId);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public void Schedule_WithEmptyMachineId_ReturnsFail()
    {
        var result = CuttingExecution.Schedule(SheetId, ValidWorker(), "   ", ValidWindow(), 5, TenantId);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public void Schedule_RaisesCuttingExecutionScheduledEvent()
    {
        var result = CuttingExecution.Schedule(SheetId, ValidWorker(), "M-01", ValidWindow(), 5, TenantId);

        var events = result.Value.PopDomainEvents();
        events.Should().ContainSingle(e => e is CuttingExecutionScheduled);
    }

    // ── Start ──────────────────────────────────────────────────────────────────

    [Fact]
    public void Start_FromScheduled_ReturnsSuccess()
    {
        var execution = ScheduledExecution();

        var result = execution.Start(execution.WorkerAssignment.WorkerId, ValidHmac(), AlwaysValidPolicy(), DateTime.UtcNow);

        result.IsSuccess.Should().BeTrue();
        execution.Status.Should().Be(CuttingExecutionStatus.Started);
    }

    [Fact]
    public void Start_FromScheduled_RaisesStartedEvent()
    {
        var execution = ScheduledExecution();
        execution.PopDomainEvents(); // clear Schedule event

        execution.Start(execution.WorkerAssignment.WorkerId, ValidHmac(), AlwaysValidPolicy(), DateTime.UtcNow);

        var events = execution.PopDomainEvents();
        events.Should().ContainSingle(e => e is CuttingExecutionStarted);
    }

    [Fact]
    public void Start_WhenHmacValidationFails_ReturnsFail()
    {
        var execution = ScheduledExecution();

        var result = execution.Start(execution.WorkerAssignment.WorkerId, ValidHmac(), AlwaysInvalidPolicy(), DateTime.UtcNow);

        result.IsSuccess.Should().BeFalse();
        execution.Status.Should().Be(CuttingExecutionStatus.Scheduled);
    }

    [Fact]
    public void Start_FromCompleted_ReturnsFail()
    {
        var execution = ScheduledExecution(totalPanels: 1);
        execution.Start(execution.WorkerAssignment.WorkerId, ValidHmac(), AlwaysValidPolicy(), DateTime.UtcNow);
        execution.RecordProgress(Guid.NewGuid(), ProgressEventKind.PanelCompleted, 1, DateTime.UtcNow, ValidHmac(), AlwaysValidPolicy(), DateTime.UtcNow);
        execution.Complete(CompletionProof.CreateHashOnly("abc123").Value, AlwaysValidProofPolicy(), DateTime.UtcNow);

        var result = execution.Start(execution.WorkerAssignment.WorkerId, ValidHmac(), AlwaysValidPolicy(), DateTime.UtcNow);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public void Start_FromInProgress_ReturnsFail()
    {
        var execution = ScheduledExecution();
        execution.Start(execution.WorkerAssignment.WorkerId, ValidHmac(), AlwaysValidPolicy(), DateTime.UtcNow);
        execution.RecordProgress(Guid.NewGuid(), ProgressEventKind.PanelStarted, null, DateTime.UtcNow, ValidHmac(), AlwaysValidPolicy(), DateTime.UtcNow);

        var result = execution.Start(execution.WorkerAssignment.WorkerId, ValidHmac(), AlwaysValidPolicy(), DateTime.UtcNow);

        result.IsSuccess.Should().BeFalse();
    }

    // ── RecordProgress ─────────────────────────────────────────────────────────

    [Fact]
    public void RecordProgress_FromStarted_TransitionsToInProgress()
    {
        var execution = ScheduledExecution();
        execution.Start(execution.WorkerAssignment.WorkerId, ValidHmac(), AlwaysValidPolicy(), DateTime.UtcNow);

        execution.RecordProgress(Guid.NewGuid(), ProgressEventKind.PanelStarted, null, DateTime.UtcNow, ValidHmac(), AlwaysValidPolicy(), DateTime.UtcNow);

        execution.Status.Should().Be(CuttingExecutionStatus.InProgress);
    }

    [Fact]
    public void RecordProgress_DuplicateEventId_ReturnsSuccess_Idempotent()
    {
        var execution = ScheduledExecution();
        execution.Start(execution.WorkerAssignment.WorkerId, ValidHmac(), AlwaysValidPolicy(), DateTime.UtcNow);
        var eventId = Guid.NewGuid();

        execution.RecordProgress(eventId, ProgressEventKind.PanelStarted, null, DateTime.UtcNow, ValidHmac(), AlwaysValidPolicy(), DateTime.UtcNow);
        var result = execution.RecordProgress(eventId, ProgressEventKind.PanelStarted, null, DateTime.UtcNow, ValidHmac(), AlwaysValidPolicy(), DateTime.UtcNow);

        result.IsSuccess.Should().BeTrue();
        execution.ProgressEvents.Should().HaveCount(1);
    }

    [Fact]
    public void RecordProgress_WrongStatus_ReturnsFail()
    {
        var execution = ScheduledExecution();

        var result = execution.RecordProgress(Guid.NewGuid(), ProgressEventKind.PanelStarted, null, DateTime.UtcNow, ValidHmac(), AlwaysValidPolicy(), DateTime.UtcNow);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public void RecordProgress_PanelCompleted_IncrementsPanelsCompleted()
    {
        var execution = ScheduledExecution(totalPanels: 3);
        execution.Start(execution.WorkerAssignment.WorkerId, ValidHmac(), AlwaysValidPolicy(), DateTime.UtcNow);

        execution.RecordProgress(Guid.NewGuid(), ProgressEventKind.PanelCompleted, 1, DateTime.UtcNow, ValidHmac(), AlwaysValidPolicy(), DateTime.UtcNow);

        execution.PanelsCompleted.Should().Be(1);
    }

    [Fact]
    public void RecordProgress_PanelCompleted_RaisesPanelCompletedEvent()
    {
        var execution = ScheduledExecution(totalPanels: 3);
        execution.Start(execution.WorkerAssignment.WorkerId, ValidHmac(), AlwaysValidPolicy(), DateTime.UtcNow);
        execution.PopDomainEvents();

        execution.RecordProgress(Guid.NewGuid(), ProgressEventKind.PanelCompleted, 1, DateTime.UtcNow, ValidHmac(), AlwaysValidPolicy(), DateTime.UtcNow);

        var events = execution.PopDomainEvents();
        events.Should().Contain(e => e is PanelCompleted);
    }

    // ── Complete ───────────────────────────────────────────────────────────────

    [Fact]
    public void Complete_AllPanelsDone_ReturnsSuccess()
    {
        var execution = ScheduledExecution(totalPanels: 1);
        execution.Start(execution.WorkerAssignment.WorkerId, ValidHmac(), AlwaysValidPolicy(), DateTime.UtcNow);
        execution.RecordProgress(Guid.NewGuid(), ProgressEventKind.PanelCompleted, 1, DateTime.UtcNow, ValidHmac(), AlwaysValidPolicy(), DateTime.UtcNow);

        var result = execution.Complete(CompletionProof.CreateHashOnly("hash").Value, AlwaysValidProofPolicy(), DateTime.UtcNow);

        result.IsSuccess.Should().BeTrue();
        execution.Status.Should().Be(CuttingExecutionStatus.Completed);
    }

    [Fact]
    public void Complete_NotAllPanelsDone_ReturnsFail()
    {
        var execution = ScheduledExecution(totalPanels: 5);
        execution.Start(execution.WorkerAssignment.WorkerId, ValidHmac(), AlwaysValidPolicy(), DateTime.UtcNow);
        execution.RecordProgress(Guid.NewGuid(), ProgressEventKind.PanelCompleted, 1, DateTime.UtcNow, ValidHmac(), AlwaysValidPolicy(), DateTime.UtcNow);

        var result = execution.Complete(CompletionProof.CreateHashOnly("hash").Value, AlwaysValidProofPolicy(), DateTime.UtcNow);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public void Complete_FromScheduled_ReturnsFail()
    {
        var execution = ScheduledExecution();

        var result = execution.Complete(CompletionProof.CreateHashOnly("hash").Value, AlwaysValidProofPolicy(), DateTime.UtcNow);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public void Complete_RaisesCompletedAndProofCommittedEvents()
    {
        var execution = ScheduledExecution(totalPanels: 1);
        execution.Start(execution.WorkerAssignment.WorkerId, ValidHmac(), AlwaysValidPolicy(), DateTime.UtcNow);
        execution.RecordProgress(Guid.NewGuid(), ProgressEventKind.PanelCompleted, 1, DateTime.UtcNow, ValidHmac(), AlwaysValidPolicy(), DateTime.UtcNow);
        execution.PopDomainEvents();

        execution.Complete(CompletionProof.CreateHashOnly("hash").Value, AlwaysValidProofPolicy(), DateTime.UtcNow);

        var events = execution.PopDomainEvents();
        events.Should().Contain(e => e is CuttingExecutionCompleted);
        events.Should().Contain(e => e is CompletionProofCommitted);
    }

    // ── Cancel ─────────────────────────────────────────────────────────────────

    [Fact]
    public void Cancel_FromScheduled_ReturnsSuccess()
    {
        var execution = ScheduledExecution();

        var result = execution.Cancel(CancelReason.OperatorCancelled, DateTime.UtcNow);

        result.IsSuccess.Should().BeTrue();
        execution.Status.Should().Be(CuttingExecutionStatus.Cancelled);
    }

    [Fact]
    public void Cancel_FromStarted_ReturnsSuccess()
    {
        var execution = ScheduledExecution();
        execution.Start(execution.WorkerAssignment.WorkerId, ValidHmac(), AlwaysValidPolicy(), DateTime.UtcNow);

        var result = execution.Cancel(CancelReason.MachineFault, DateTime.UtcNow);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void Cancel_FromInProgress_ReturnsSuccess()
    {
        var execution = ScheduledExecution();
        execution.Start(execution.WorkerAssignment.WorkerId, ValidHmac(), AlwaysValidPolicy(), DateTime.UtcNow);
        execution.RecordProgress(Guid.NewGuid(), ProgressEventKind.PanelStarted, null, DateTime.UtcNow, ValidHmac(), AlwaysValidPolicy(), DateTime.UtcNow);

        var result = execution.Cancel(CancelReason.MaterialShortage, DateTime.UtcNow);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void Cancel_FromCompleted_ReturnsFail()
    {
        var execution = ScheduledExecution(totalPanels: 1);
        execution.Start(execution.WorkerAssignment.WorkerId, ValidHmac(), AlwaysValidPolicy(), DateTime.UtcNow);
        execution.RecordProgress(Guid.NewGuid(), ProgressEventKind.PanelCompleted, 1, DateTime.UtcNow, ValidHmac(), AlwaysValidPolicy(), DateTime.UtcNow);
        execution.Complete(CompletionProof.CreateHashOnly("h").Value, AlwaysValidProofPolicy(), DateTime.UtcNow);

        var result = execution.Cancel(CancelReason.OperatorCancelled, DateTime.UtcNow);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public void Cancel_FromCancelled_ReturnsFail()
    {
        var execution = ScheduledExecution();
        execution.Cancel(CancelReason.OperatorCancelled, DateTime.UtcNow);

        var result = execution.Cancel(CancelReason.OperatorCancelled, DateTime.UtcNow);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public void Cancel_RaisesCancelledEvent()
    {
        var execution = ScheduledExecution();
        execution.PopDomainEvents();

        execution.Cancel(CancelReason.SystemCancelled, DateTime.UtcNow);

        var events = execution.PopDomainEvents();
        events.Should().ContainSingle(e => e is CuttingExecutionCancelled);
    }

    [Fact]
    public void Cancel_StoresCancelReasonAndTimestamp()
    {
        var execution = ScheduledExecution();
        var now = DateTime.UtcNow;

        execution.Cancel(CancelReason.MaterialShortage, now);

        execution.CancelReason.Should().Be(CancelReason.MaterialShortage);
        execution.CancelledAt.Should().BeCloseTo(now, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void RecordOffcut_WhenStarted_ReturnsSuccess()
    {
        var execution = ScheduledExecution();
        execution.Start(execution.WorkerAssignment.WorkerId, ValidHmac(), AlwaysValidPolicy(), DateTime.UtcNow);
        var offcut = OffcutEvent.Create(Guid.NewGuid(), 300m, 200m).Value;

        var result = execution.RecordOffcut(offcut, DateTime.UtcNow);

        result.IsSuccess.Should().BeTrue();
        execution.OffcutReports.Should().HaveCount(1);
    }

    [Fact]
    public void RecordOffcut_WhenScheduled_ReturnsFail()
    {
        var execution = ScheduledExecution();
        var offcut = OffcutEvent.Create(Guid.NewGuid(), 300m, 200m).Value;

        var result = execution.RecordOffcut(offcut, DateTime.UtcNow);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public void RecordOffcut_AccumulatesAreaMm2()
    {
        var execution = ScheduledExecution();
        execution.Start(execution.WorkerAssignment.WorkerId, ValidHmac(), AlwaysValidPolicy(), DateTime.UtcNow);
        var offcut1 = OffcutEvent.Create(Guid.NewGuid(), 300m, 200m).Value;
        var offcut2 = OffcutEvent.Create(Guid.NewGuid(), 100m, 100m).Value;

        execution.RecordOffcut(offcut1, DateTime.UtcNow);
        execution.RecordOffcut(offcut2, DateTime.UtcNow);

        execution.OffcutAreaMm2.Should().Be(300m * 200m + 100m * 100m);
    }
}
