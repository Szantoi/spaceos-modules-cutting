using FluentAssertions;
using Moq;
using SpaceOS.Modules.Cutting.Execution.Domain.Aggregates;
using SpaceOS.Modules.Cutting.Execution.Domain.Enums;
using SpaceOS.Modules.Cutting.Execution.Domain.Events;
using SpaceOS.Modules.Cutting.Execution.Domain.Interfaces;
using SpaceOS.Modules.Cutting.Execution.Domain.ValueObjects;
using Xunit;

namespace SpaceOS.Modules.Cutting.Tests.Execution;

public class DomainEventTests
{
    private static IWorkerSecurityPolicy AlwaysValidPolicy()
    {
        var mock = new Mock<IWorkerSecurityPolicy>();
        mock.Setup(p => p.ValidateProgressEventHmac(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<WorkerEventHmac>()))
            .Returns(true);
        return mock.Object;
    }

    private static ICuttingProofPolicy AlwaysValidProofPolicy()
    {
        var mock = new Mock<ICuttingProofPolicy>();
        mock.Setup(p => p.IsValid(It.IsAny<CompletionProof>(), It.IsAny<Guid>())).Returns(true);
        return mock.Object;
    }

    private static WorkerEventHmac ValidHmac() =>
        WorkerEventHmac.Create(Convert.ToBase64String(new byte[32]), "v1").Value;

    private static CuttingExecution BuildScheduled()
    {
        var window = ScheduleWindow.Create(DateTime.UtcNow, DateTime.UtcNow.AddHours(2)).Value;
        var worker = WorkerAssignment.Create(Guid.NewGuid(), Guid.NewGuid()).Value;
        return CuttingExecution.Schedule(Guid.NewGuid(), worker, "M-01", window, 3, Guid.NewGuid()).Value;
    }

    [Fact]
    public void Schedule_RaisesExactlyOneScheduledEvent()
    {
        var execution = BuildScheduled();

        execution.DomainEvents.Should().ContainSingle(e => e is CuttingExecutionScheduled);
    }

    [Fact]
    public void PopDomainEvents_ClearsEventList()
    {
        var execution = BuildScheduled();

        execution.PopDomainEvents();

        execution.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void Start_RaisesStartedEvent_AfterScheduled()
    {
        var execution = BuildScheduled();
        execution.PopDomainEvents();

        execution.Start(execution.WorkerAssignment.WorkerId, ValidHmac(), AlwaysValidPolicy(), DateTime.UtcNow);

        execution.DomainEvents.Should().ContainSingle(e => e is CuttingExecutionStarted);
    }

    [Fact]
    public void RecordProgress_RaisesProgressRecordedEvent()
    {
        var execution = BuildScheduled();
        execution.Start(execution.WorkerAssignment.WorkerId, ValidHmac(), AlwaysValidPolicy(), DateTime.UtcNow);
        execution.PopDomainEvents();

        execution.RecordProgress(Guid.NewGuid(), ProgressEventKind.MaterialLoaded, null, DateTime.UtcNow, ValidHmac(), AlwaysValidPolicy(), DateTime.UtcNow);

        execution.DomainEvents.Should().Contain(e => e is ProgressRecorded);
    }

    [Fact]
    public void RecordProgress_PanelCompleted_RaisesBothProgressAndPanelCompletedEvents()
    {
        var execution = BuildScheduled();
        execution.Start(execution.WorkerAssignment.WorkerId, ValidHmac(), AlwaysValidPolicy(), DateTime.UtcNow);
        execution.PopDomainEvents();

        execution.RecordProgress(Guid.NewGuid(), ProgressEventKind.PanelCompleted, 1, DateTime.UtcNow, ValidHmac(), AlwaysValidPolicy(), DateTime.UtcNow);

        execution.DomainEvents.Should().Contain(e => e is ProgressRecorded);
        execution.DomainEvents.Should().Contain(e => e is PanelCompleted);
    }

    [Fact]
    public void Cancel_RaisesCancelledEvent()
    {
        var execution = BuildScheduled();
        execution.PopDomainEvents();

        execution.Cancel(CancelReason.OperatorCancelled, DateTime.UtcNow);

        execution.DomainEvents.Should().ContainSingle(e => e is CuttingExecutionCancelled);
    }

    [Fact]
    public void Complete_RaisesCompletedAndProofCommittedEvents()
    {
        var execution = BuildScheduled();
        execution.Start(execution.WorkerAssignment.WorkerId, ValidHmac(), AlwaysValidPolicy(), DateTime.UtcNow);
        for (var i = 1; i <= 3; i++)
            execution.RecordProgress(Guid.NewGuid(), ProgressEventKind.PanelCompleted, i, DateTime.UtcNow, ValidHmac(), AlwaysValidPolicy(), DateTime.UtcNow);
        execution.PopDomainEvents();

        execution.Complete(CompletionProof.CreateHashOnly("hash").Value, AlwaysValidProofPolicy(), DateTime.UtcNow);

        execution.DomainEvents.Should().Contain(e => e is CuttingExecutionCompleted);
        execution.DomainEvents.Should().Contain(e => e is CompletionProofCommitted);
    }

    [Fact]
    public void RecordOffcut_RaisesOffcutReportedEvent()
    {
        var execution = BuildScheduled();
        execution.Start(execution.WorkerAssignment.WorkerId, ValidHmac(), AlwaysValidPolicy(), DateTime.UtcNow);
        execution.PopDomainEvents();
        var offcut = OffcutEvent.Create(Guid.NewGuid(), 100m, 200m).Value;

        execution.RecordOffcut(offcut, DateTime.UtcNow);

        execution.DomainEvents.Should().ContainSingle(e => e is OffcutReported);
    }

    [Fact]
    public void ScheduledEvent_ContainsCorrectSheetId()
    {
        var sheetId = Guid.NewGuid();
        var window = ScheduleWindow.Create(DateTime.UtcNow, DateTime.UtcNow.AddHours(2)).Value;
        var worker = WorkerAssignment.Create(Guid.NewGuid(), Guid.NewGuid()).Value;
        var execution = CuttingExecution.Schedule(sheetId, worker, "M-01", window, 1, Guid.NewGuid()).Value;

        var evt = execution.DomainEvents.OfType<CuttingExecutionScheduled>().Single();
        evt.SheetId.Should().Be(sheetId);
    }

    [Fact]
    public void MultiplePopCalls_EachReturnEmptyAfterFirstPop()
    {
        var execution = BuildScheduled();

        var first = execution.PopDomainEvents();
        var second = execution.PopDomainEvents();

        first.Should().NotBeEmpty();
        second.Should().BeEmpty();
    }
}
