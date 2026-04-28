using Ardalis.Result;
using FluentAssertions;
using MediatR;
using Moq;
using SpaceOS.Modules.Cutting.Execution.Application.Commands.CancelExecution;
using SpaceOS.Modules.Cutting.Execution.Application.Commands.CompleteExecution;
using SpaceOS.Modules.Cutting.Execution.Application.Commands.EvaluateMilestones;
using SpaceOS.Modules.Cutting.Execution.Application.Commands.RecordOffcut;
using SpaceOS.Modules.Cutting.Execution.Application.Commands.RecordProgress;
using SpaceOS.Modules.Cutting.Execution.Application.Commands.ScheduleExecution;
using SpaceOS.Modules.Cutting.Execution.Application.Commands.StartExecution;
using SpaceOS.Modules.Cutting.Execution.Application.Commands.WithdrawWorkerConsent;
using SpaceOS.Modules.Cutting.Execution.Application.Entities;
using SpaceOS.Modules.Cutting.Execution.Application.Ports;
using SpaceOS.Modules.Cutting.Execution.Application.Services;
using SpaceOS.Modules.Cutting.Execution.Domain.Aggregates;
using SpaceOS.Modules.Cutting.Execution.Domain.Enums;
using SpaceOS.Modules.Cutting.Execution.Domain.Interfaces;
using SpaceOS.Modules.Cutting.Execution.Domain.ValueObjects;
using Xunit;

namespace SpaceOS.Modules.Cutting.Tests.Execution;

public class CommandHandlerTests
{
    private static IWorkerSecurityPolicy AlwaysValidSecurityPolicy()
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

    private static CuttingExecution BuildScheduled(int total = 3)
    {
        var worker = WorkerAssignment.Create(Guid.NewGuid(), Guid.NewGuid()).Value;
        var window = ScheduleWindow.Create(DateTime.UtcNow, DateTime.UtcNow.AddHours(2)).Value;
        return CuttingExecution.Schedule(Guid.NewGuid(), worker, "M-01", window, total, Guid.NewGuid()).Value;
    }

    private static CuttingExecution BuildInProgress()
    {
        var policy = AlwaysValidSecurityPolicy();
        var hmac = WorkerEventHmac.Create(Convert.ToBase64String(new byte[32]), "v1").Value;
        var exec = BuildScheduled();
        exec.Start(exec.WorkerAssignment.WorkerId, hmac, policy, DateTime.UtcNow);
        exec.RecordProgress(Guid.NewGuid(), ProgressEventKind.PanelStarted, null, DateTime.UtcNow, hmac, policy, DateTime.UtcNow);
        return exec;
    }

    // ── ScheduleExecution ──────────────────────────────────────────────────────

    [Fact]
    public async Task ScheduleExecutionHandler_ValidCommand_ReturnsSuccessWithId()
    {
        var repo = new Mock<ICuttingExecutionRepository>();
        repo.Setup(r => r.AddAsync(It.IsAny<CuttingExecution>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        repo.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var handler = new ScheduleExecutionCommandHandler(repo.Object);
        var cmd = new ScheduleExecutionCommand(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            "M-01", DateTime.UtcNow, DateTime.UtcNow.AddHours(2), 5);

        var result = await handler.Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public async Task ScheduleExecutionHandler_EmptyTenantId_ReturnsFail()
    {
        var repo = new Mock<ICuttingExecutionRepository>();
        var handler = new ScheduleExecutionCommandHandler(repo.Object);
        var cmd = new ScheduleExecutionCommand(
            Guid.Empty, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            "M-01", DateTime.UtcNow, DateTime.UtcNow.AddHours(2), 5);

        var result = await handler.Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
    }

    // ── StartExecution ─────────────────────────────────────────────────────────

    [Fact]
    public async Task StartExecutionHandler_ValidExecution_ReturnsSuccess()
    {
        var execution = BuildScheduled();
        var repo = new Mock<ICuttingExecutionRepository>();
        repo.Setup(r => r.GetByIdAsync(execution.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(execution);
        repo.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var handler = new StartExecutionCommandHandler(repo.Object, AlwaysValidSecurityPolicy());
        var cmd = new StartExecutionCommand(
            execution.Id, execution.TenantId, execution.WorkerAssignment.WorkerId,
            Convert.ToBase64String(new byte[32]), "v1");

        var result = await handler.Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task StartExecutionHandler_NotFound_ReturnsNotFound()
    {
        var repo = new Mock<ICuttingExecutionRepository>();
        repo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((CuttingExecution?)null);

        var handler = new StartExecutionCommandHandler(repo.Object, AlwaysValidSecurityPolicy());
        var cmd = new StartExecutionCommand(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            Convert.ToBase64String(new byte[32]), "v1");

        var result = await handler.Handle(cmd, CancellationToken.None);

        result.Status.Should().Be(ResultStatus.NotFound);
    }

    // ── RecordProgress ─────────────────────────────────────────────────────────

    [Fact]
    public async Task RecordProgressHandler_ValidInProgress_ReturnsSuccess()
    {
        var execution = BuildInProgress();
        var repo = new Mock<ICuttingExecutionRepository>();
        repo.Setup(r => r.GetByIdWithProgressAsync(execution.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(execution);
        repo.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var handler = new RecordProgressCommandHandler(repo.Object, AlwaysValidSecurityPolicy());
        var cmd = new RecordProgressCommand(
            execution.Id, execution.TenantId, Guid.NewGuid(),
            ProgressEventKind.PanelCompleted, 2, DateTime.UtcNow,
            Convert.ToBase64String(new byte[32]), "v1");

        var result = await handler.Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task RecordProgressHandler_NotFound_ReturnsNotFound()
    {
        var repo = new Mock<ICuttingExecutionRepository>();
        repo.Setup(r => r.GetByIdWithProgressAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((CuttingExecution?)null);

        var handler = new RecordProgressCommandHandler(repo.Object, AlwaysValidSecurityPolicy());
        var cmd = new RecordProgressCommand(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            ProgressEventKind.PanelStarted, null, DateTime.UtcNow,
            Convert.ToBase64String(new byte[32]), "v1");

        var result = await handler.Handle(cmd, CancellationToken.None);

        result.Status.Should().Be(ResultStatus.NotFound);
    }

    // ── RecordOffcut ───────────────────────────────────────────────────────────

    [Fact]
    public async Task RecordOffcutHandler_ValidInProgress_ReturnsSuccess()
    {
        var execution = BuildInProgress();
        var repo = new Mock<ICuttingExecutionRepository>();
        repo.Setup(r => r.GetByIdAsync(execution.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(execution);
        repo.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var handler = new RecordOffcutCommandHandler(repo.Object);
        var cmd = new RecordOffcutCommand(execution.Id, execution.TenantId, Guid.NewGuid(), 300m, 200m);

        var result = await handler.Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task RecordOffcutHandler_Scheduled_ReturnsFail()
    {
        var execution = BuildScheduled();
        var repo = new Mock<ICuttingExecutionRepository>();
        repo.Setup(r => r.GetByIdAsync(execution.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(execution);

        var handler = new RecordOffcutCommandHandler(repo.Object);
        var cmd = new RecordOffcutCommand(execution.Id, execution.TenantId, Guid.NewGuid(), 300m, 200m);

        var result = await handler.Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
    }

    // ── CompleteExecution ──────────────────────────────────────────────────────

    [Fact]
    public async Task CompleteExecutionHandler_AllPanelsDone_ReturnsSuccess()
    {
        var policy = AlwaysValidSecurityPolicy();
        var hmac = WorkerEventHmac.Create(Convert.ToBase64String(new byte[32]), "v1").Value;
        var execution = BuildScheduled(total: 1);
        execution.Start(execution.WorkerAssignment.WorkerId, hmac, policy, DateTime.UtcNow);
        execution.RecordProgress(Guid.NewGuid(), ProgressEventKind.PanelCompleted, 1, DateTime.UtcNow, hmac, policy, DateTime.UtcNow);

        var repo = new Mock<ICuttingExecutionRepository>();
        repo.Setup(r => r.GetByIdWithProgressAsync(execution.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(execution);
        repo.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var handler = new CompleteExecutionCommandHandler(repo.Object, AlwaysValidProofPolicy());
        var cmd = new CompleteExecutionCommand(execution.Id, execution.TenantId, ProofLevel.HashOnly, "hash", null, null, null);

        var result = await handler.Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task CompleteExecutionHandler_NotEnoughPanels_ReturnsFail()
    {
        var execution = BuildInProgress(); // 3 total, 0 panels completed via PanelCompleted
        var repo = new Mock<ICuttingExecutionRepository>();
        repo.Setup(r => r.GetByIdWithProgressAsync(execution.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(execution);

        var handler = new CompleteExecutionCommandHandler(repo.Object, AlwaysValidProofPolicy());
        var cmd = new CompleteExecutionCommand(execution.Id, execution.TenantId, ProofLevel.HashOnly, "hash", null, null, null);

        var result = await handler.Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
    }

    // ── CancelExecution ────────────────────────────────────────────────────────

    [Fact]
    public async Task CancelExecutionHandler_Scheduled_ReturnsSuccess()
    {
        var execution = BuildScheduled();
        var repo = new Mock<ICuttingExecutionRepository>();
        repo.Setup(r => r.GetByIdAsync(execution.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(execution);
        repo.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var handler = new CancelExecutionCommandHandler(repo.Object);
        var cmd = new CancelExecutionCommand(execution.Id, execution.TenantId, CancelReason.OperatorCancelled);

        var result = await handler.Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task CancelExecutionHandler_NotFound_ReturnsNotFound()
    {
        var repo = new Mock<ICuttingExecutionRepository>();
        repo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((CuttingExecution?)null);

        var handler = new CancelExecutionCommandHandler(repo.Object);
        var cmd = new CancelExecutionCommand(Guid.NewGuid(), Guid.NewGuid(), CancelReason.OperatorCancelled);

        var result = await handler.Handle(cmd, CancellationToken.None);

        result.Status.Should().Be(ResultStatus.NotFound);
    }

    // ── EvaluateMilestones ─────────────────────────────────────────────────────

    [Fact]
    public async Task EvaluateMilestonesHandler_ValidExecution_ReturnsSuccess()
    {
        var execution = BuildInProgress();
        execution.AddMilestone(Guid.NewGuid(), MilestoneKind.TimeWindow, "{}", 1);

        var repo = new Mock<ICuttingExecutionRepository>();
        repo.Setup(r => r.GetByIdWithProgressAsync(execution.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(execution);
        repo.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var handler = new EvaluateMilestonesCommandHandler(repo.Object, new PredicateFactoryV1());
        var cmd = new EvaluateMilestonesCommand(execution.Id, execution.TenantId);

        var result = await handler.Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task EvaluateMilestonesHandler_NotFound_ReturnsNotFound()
    {
        var repo = new Mock<ICuttingExecutionRepository>();
        repo.Setup(r => r.GetByIdWithProgressAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((CuttingExecution?)null);

        var handler = new EvaluateMilestonesCommandHandler(repo.Object, new PredicateFactoryV1());
        var cmd = new EvaluateMilestonesCommand(Guid.NewGuid(), Guid.NewGuid());

        var result = await handler.Handle(cmd, CancellationToken.None);

        result.Status.Should().Be(ResultStatus.NotFound);
    }

    // ── WithdrawWorkerConsent ──────────────────────────────────────────────────

    [Fact]
    public async Task WithdrawWorkerConsentHandler_ValidCommand_ReturnsWithdrawalId()
    {
        var consentRepo = new Mock<IConsentWithdrawalRepository>();
        consentRepo.Setup(r => r.SaveAsync(It.IsAny<ConsentWithdrawal>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var publisher = new Mock<IPublisher>();
        publisher.Setup(p => p.Publish(It.IsAny<object>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var handler = new WithdrawWorkerConsentCommandHandler(consentRepo.Object, publisher.Object);
        var cmd = new WithdrawWorkerConsentCommand(Guid.NewGuid(), Guid.NewGuid(), ConsentScope.AllExecutions);

        var result = await handler.Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public async Task WithdrawWorkerConsentHandler_SavesWithdrawalToRepository()
    {
        ConsentWithdrawal? savedWithdrawal = null;
        var consentRepo = new Mock<IConsentWithdrawalRepository>();
        consentRepo.Setup(r => r.SaveAsync(It.IsAny<ConsentWithdrawal>(), It.IsAny<CancellationToken>()))
            .Callback<ConsentWithdrawal, CancellationToken>((w, _) => savedWithdrawal = w)
            .Returns(Task.CompletedTask);
        var publisher = new Mock<IPublisher>();
        publisher.Setup(p => p.Publish(It.IsAny<object>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var tenantId = Guid.NewGuid();
        var workerId = Guid.NewGuid();
        var handler = new WithdrawWorkerConsentCommandHandler(consentRepo.Object, publisher.Object);
        var cmd = new WithdrawWorkerConsentCommand(tenantId, workerId, ConsentScope.SpecificTenant);

        await handler.Handle(cmd, CancellationToken.None);

        savedWithdrawal.Should().NotBeNull();
        savedWithdrawal!.TenantId.Should().Be(tenantId);
        savedWithdrawal.WorkerId.Should().Be(workerId);
    }
}
