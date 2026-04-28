using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using SpaceOS.Modules.Cutting.Execution.Application.Commands.CancelExecution;
using SpaceOS.Modules.Cutting.Execution.Application.Commands.CompleteExecution;
using SpaceOS.Modules.Cutting.Execution.Application.Commands.RecordOffcut;
using SpaceOS.Modules.Cutting.Execution.Application.Commands.RecordProgress;
using SpaceOS.Modules.Cutting.Execution.Application.Commands.ScheduleExecution;
using SpaceOS.Modules.Cutting.Execution.Application.Commands.StartExecution;
using SpaceOS.Modules.Cutting.Execution.Application.Queries.GetExecution;
using SpaceOS.Modules.Cutting.Execution.Application.Queries.GetProgress;
using SpaceOS.Modules.Cutting.Execution.Domain.Enums;
using SpaceOS.Modules.Cutting.Execution.Domain.Interfaces;
using SpaceOS.Modules.Cutting.Execution.Infrastructure.Persistence.Repositories;
using SpaceOS.Modules.Cutting.Infrastructure.Outbox;
using SpaceOS.Modules.Cutting.Infrastructure.Persistence;
using Xunit;

namespace SpaceOS.Modules.Cutting.Tests.Execution.Integration;

/// <summary>
/// Integration tests that exercise the full Schedule → Start → RecordProgress → Complete lifecycle
/// using InMemory EF Core.
/// </summary>
public class ExecutionLifecycleTests : IDisposable
{
    private readonly CuttingDbContext _db;
    private readonly CuttingExecutionRepository _repo;
    private static readonly Guid TenantId = Guid.NewGuid();

    public ExecutionLifecycleTests()
    {
        var options = new DbContextOptionsBuilder<CuttingDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new CuttingDbContext(options);
        _repo = new CuttingExecutionRepository(_db);
    }

    [Fact]
    public async Task Schedule_Persists_ScheduledExecution()
    {
        var handler = new ScheduleExecutionCommandHandler(_repo);
        var command = new ScheduleExecutionCommand(
            TenantId, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            "CNC-01", DateTime.UtcNow, DateTime.UtcNow.AddHours(1), 3);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var found = await _repo.GetByIdAsync(result.Value, CancellationToken.None);
        found.Should().NotBeNull();
        found!.Status.Should().Be(CuttingExecutionStatus.Scheduled);
    }

    [Fact]
    public async Task Start_AfterSchedule_TransitionsToStarted()
    {
        var workerId = Guid.NewGuid();
        var scheduleResult = await ScheduleExecutionAsync(workerId: workerId);
        var executionId = scheduleResult.Value;

        var securityPolicy = new Mock<IWorkerSecurityPolicy>();
        securityPolicy.Setup(p => p.ValidateProgressEventHmac(
            It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<SpaceOS.Modules.Cutting.Execution.Domain.ValueObjects.WorkerEventHmac>()))
            .Returns(true);

        var startHandler = new StartExecutionCommandHandler(_repo, securityPolicy.Object);
        var hmac = Convert.ToBase64String(new byte[32]);
        var startCmd = new StartExecutionCommand(executionId, TenantId, workerId, hmac, "v1");
        var startResult = await startHandler.Handle(startCmd, CancellationToken.None);

        startResult.IsSuccess.Should().BeTrue();
        var found = await _repo.GetByIdAsync(executionId, CancellationToken.None);
        found!.Status.Should().Be(CuttingExecutionStatus.Started);
    }

    [Fact]
    public async Task RecordProgress_AfterStart_TransitionsToInProgress()
    {
        var workerId = Guid.NewGuid();
        var executionId = (await ScheduleExecutionAsync(workerId: workerId)).Value;
        await StartExecutionAsync(executionId, workerId);

        var securityPolicy = new Mock<IWorkerSecurityPolicy>();
        securityPolicy.Setup(p => p.ValidateProgressEventHmac(
            It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<SpaceOS.Modules.Cutting.Execution.Domain.ValueObjects.WorkerEventHmac>()))
            .Returns(true);

        var progressHandler = new RecordProgressCommandHandler(_repo, securityPolicy.Object);
        var hmac = Convert.ToBase64String(new byte[32]);
        var progressCmd = new RecordProgressCommand(
            executionId, TenantId, Guid.NewGuid(), ProgressEventKind.PanelCompleted, 1,
            DateTime.UtcNow, hmac, "v1");

        var result = await progressHandler.Handle(progressCmd, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var found = await _repo.GetByIdAsync(executionId, CancellationToken.None);
        found!.Status.Should().Be(CuttingExecutionStatus.InProgress);
    }

    [Fact]
    public async Task Cancel_FromScheduled_TransitionsToCancelled()
    {
        var executionId = (await ScheduleExecutionAsync()).Value;

        var cancelHandler = new CancelExecutionCommandHandler(_repo);
        var cancelCmd = new CancelExecutionCommand(executionId, TenantId, CancelReason.OperatorCancelled);
        var result = await cancelHandler.Handle(cancelCmd, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var found = await _repo.GetByIdAsync(executionId, CancellationToken.None);
        found!.Status.Should().Be(CuttingExecutionStatus.Cancelled);
    }

    [Fact]
    public async Task Cancel_FromTerminalState_ReturnsInvalid()
    {
        var executionId = (await ScheduleExecutionAsync()).Value;

        var cancelHandler = new CancelExecutionCommandHandler(_repo);
        // Cancel once
        await cancelHandler.Handle(new CancelExecutionCommand(executionId, TenantId, CancelReason.OperatorCancelled), CancellationToken.None);

        // Try to cancel again
        var result = await cancelHandler.Handle(new CancelExecutionCommand(executionId, TenantId, CancelReason.OperatorCancelled), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task RecordOffcut_DuringInProgress_AddsOffcutReport()
    {
        var workerId = Guid.NewGuid();
        var executionId = (await ScheduleExecutionAsync(workerId: workerId)).Value;
        await StartExecutionAsync(executionId, workerId);
        await RecordProgressAsync(executionId); // transitions to InProgress

        var offcutHandler = new RecordOffcutCommandHandler(_repo);
        var cmd = new RecordOffcutCommand(executionId, TenantId, Guid.NewGuid(), 300m, 200m);
        var result = await offcutHandler.Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var found = await _repo.GetByIdWithProgressAsync(executionId, CancellationToken.None);
        found!.OffcutReports.Should().HaveCount(1);
        found.OffcutAreaMm2.Should().Be(300m * 200m);
    }

    [Fact]
    public async Task GetExecution_AfterSchedule_ReturnsExecutionDto()
    {
        var executionId = (await ScheduleExecutionAsync()).Value;

        var queryHandler = new GetExecutionQueryHandler(_repo);
        var result = await queryHandler.Handle(new GetExecutionQuery(executionId, TenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Id.Should().Be(executionId);
        result.Value.Status.Should().Be("Scheduled");
    }

    [Fact]
    public async Task GetExecution_NotFound_ReturnsNotFound()
    {
        var queryHandler = new GetExecutionQueryHandler(_repo);
        var result = await queryHandler.Handle(new GetExecutionQuery(Guid.NewGuid(), TenantId), CancellationToken.None);

        result.Status.Should().Be(Ardalis.Result.ResultStatus.NotFound);
    }

    [Fact]
    public async Task GetProgress_ReturnsProgressEvents()
    {
        var workerId = Guid.NewGuid();
        var executionId = (await ScheduleExecutionAsync(workerId: workerId)).Value;
        await StartExecutionAsync(executionId, workerId);
        await RecordProgressAsync(executionId);

        var queryHandler = new GetProgressQueryHandler(_repo);
        var result = await queryHandler.Handle(new GetProgressQuery(executionId, TenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(1);
    }

    [Fact]
    public async Task DuplicateEventId_IsIdempotent()
    {
        var workerId = Guid.NewGuid();
        var executionId = (await ScheduleExecutionAsync(workerId: workerId)).Value;
        await StartExecutionAsync(executionId, workerId);

        var eventId = Guid.NewGuid();
        var securityPolicy = new Mock<IWorkerSecurityPolicy>();
        securityPolicy.Setup(p => p.ValidateProgressEventHmac(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<SpaceOS.Modules.Cutting.Execution.Domain.ValueObjects.WorkerEventHmac>()))
            .Returns(true);

        var handler = new RecordProgressCommandHandler(_repo, securityPolicy.Object);
        var hmac = Convert.ToBase64String(new byte[32]);
        var cmd = new RecordProgressCommand(executionId, TenantId, eventId, ProgressEventKind.PanelCompleted, 1, DateTime.UtcNow, hmac, "v1");

        // Send twice with same eventId
        var r1 = await handler.Handle(cmd, CancellationToken.None);
        var r2 = await handler.Handle(cmd, CancellationToken.None);

        r1.IsSuccess.Should().BeTrue();
        r2.IsSuccess.Should().BeTrue("duplicate events are idempotent");

        var found = await _repo.GetByIdWithProgressAsync(executionId, CancellationToken.None);
        found!.ProgressEvents.Should().HaveCount(1, "duplicate event should not be double-recorded");
    }

    // ── Helpers ─────────────────────────────────────────────────────────────────

    private async Task<Ardalis.Result.Result<Guid>> ScheduleExecutionAsync(Guid? workerId = null, int totalPanels = 3)
    {
        var handler = new ScheduleExecutionCommandHandler(_repo);
        var cmd = new ScheduleExecutionCommand(
            TenantId, Guid.NewGuid(), workerId ?? Guid.NewGuid(), Guid.NewGuid(),
            "CNC-01", DateTime.UtcNow, DateTime.UtcNow.AddHours(1), totalPanels);
        return await handler.Handle(cmd, CancellationToken.None);
    }

    private async Task StartExecutionAsync(Guid executionId, Guid workerId)
    {
        var securityPolicy = new Mock<IWorkerSecurityPolicy>();
        securityPolicy.Setup(p => p.ValidateProgressEventHmac(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<SpaceOS.Modules.Cutting.Execution.Domain.ValueObjects.WorkerEventHmac>()))
            .Returns(true);

        var handler = new StartExecutionCommandHandler(_repo, securityPolicy.Object);
        var hmac = Convert.ToBase64String(new byte[32]);
        await handler.Handle(new StartExecutionCommand(executionId, TenantId, workerId, hmac, "v1"), CancellationToken.None);
    }

    private async Task RecordProgressAsync(Guid executionId, int panel = 1)
    {
        var securityPolicy = new Mock<IWorkerSecurityPolicy>();
        securityPolicy.Setup(p => p.ValidateProgressEventHmac(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<SpaceOS.Modules.Cutting.Execution.Domain.ValueObjects.WorkerEventHmac>()))
            .Returns(true);

        var handler = new RecordProgressCommandHandler(_repo, securityPolicy.Object);
        var hmac = Convert.ToBase64String(new byte[32]);
        var cmd = new RecordProgressCommand(executionId, TenantId, Guid.NewGuid(), ProgressEventKind.PanelCompleted, panel, DateTime.UtcNow, hmac, "v1");
        await handler.Handle(cmd, CancellationToken.None);
    }

    public void Dispose() => _db.Dispose();
}
