using Ardalis.Result;
using FluentAssertions;
using Moq;
using SpaceOS.Modules.Cutting.Application.Commands.CompleteJob;
using SpaceOS.Modules.Cutting.Application.Events;
using SpaceOS.Modules.Cutting.Domain.Aggregates;
using SpaceOS.Modules.Cutting.Domain.Interfaces;
using Xunit;

namespace SpaceOS.Modules.Cutting.Tests.Application;

public class CompleteJobCommandHandlerTests
{
    private static readonly Guid TenantId   = Guid.NewGuid();
    private static readonly Guid SheetId    = Guid.NewGuid();
    private static readonly DateTime Today  = DateTime.SpecifyKind(DateTime.UtcNow.Date, DateTimeKind.Utc);

    private readonly Mock<ICuttingRepository>     _repoMock      = new();
    private readonly Mock<ICuttingEventPublisher> _publisherMock = new();
    private readonly CompleteJobCommandHandler    _handler;

    public CompleteJobCommandHandlerTests()
    {
        _handler = new CompleteJobCommandHandler(_repoMock.Object, _publisherMock.Object);
    }

    private static CuttingJob MakeJob()
        => CuttingJob.Create(Guid.NewGuid(), Guid.NewGuid(), Today, "Normal", 2m);

    // ── Happy path ────────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_ValidJob_ReturnsSuccess()
    {
        var job = MakeJob();
        _repoMock.Setup(r => r.GetCuttingJobTrackedAsync(job.Id, default)).ReturnsAsync(job);

        var cmd = new CompleteJobCommand(job.Id, TenantId, SheetId, 91m, 0.5m);
        var result = await _handler.Handle(cmd, default);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_ValidJob_MarksStatusAsCut()
    {
        var job = MakeJob();
        _repoMock.Setup(r => r.GetCuttingJobTrackedAsync(job.Id, default)).ReturnsAsync(job);

        await _handler.Handle(new CompleteJobCommand(job.Id, TenantId, SheetId, 91m, 0.5m), default);

        job.Status.Should().Be("Cut");
    }

    [Fact]
    public async Task Handle_ValidJob_SavesAndPublishes()
    {
        var job = MakeJob();
        _repoMock.Setup(r => r.GetCuttingJobTrackedAsync(job.Id, default)).ReturnsAsync(job);

        await _handler.Handle(new CompleteJobCommand(job.Id, TenantId, SheetId, 91m, 0.5m), default);

        _repoMock.Verify(r => r.SaveChangesAsync(default), Times.Once);
        _publisherMock.Verify(p => p.PublishJobCompletedAsync(
            job.Id, TenantId, SheetId, It.IsAny<DateTime>(), 91m, 0.5m, default), Times.Once);
    }

    // ── Not found ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_JobNotFound_ReturnsNotFound()
    {
        _repoMock.Setup(r => r.GetCuttingJobTrackedAsync(It.IsAny<Guid>(), default))
                 .ReturnsAsync((CuttingJob?)null);

        var result = await _handler.Handle(
            new CompleteJobCommand(Guid.NewGuid(), TenantId, SheetId, 91m, 0.5m), default);

        result.Status.Should().Be(ResultStatus.NotFound);
    }

    [Fact]
    public async Task Handle_JobNotFound_DoesNotPublish()
    {
        _repoMock.Setup(r => r.GetCuttingJobTrackedAsync(It.IsAny<Guid>(), default))
                 .ReturnsAsync((CuttingJob?)null);

        await _handler.Handle(
            new CompleteJobCommand(Guid.NewGuid(), TenantId, SheetId, 91m, 0.5m), default);

        _publisherMock.Verify(
            p => p.PublishJobCompletedAsync(
                It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(),
                It.IsAny<DateTime>(), It.IsAny<decimal>(), It.IsAny<decimal>(), default),
            Times.Never);
    }

    // ── Domain state guard ────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_AlreadyCutJob_ReturnsInvalid()
    {
        var job = MakeJob();
        job.MarkAsCut();
        _repoMock.Setup(r => r.GetCuttingJobTrackedAsync(job.Id, default)).ReturnsAsync(job);

        var result = await _handler.Handle(
            new CompleteJobCommand(job.Id, TenantId, SheetId, 91m, 0.5m), default);

        result.Status.Should().Be(ResultStatus.Invalid);
        _publisherMock.Verify(
            p => p.PublishJobCompletedAsync(
                It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(),
                It.IsAny<DateTime>(), It.IsAny<decimal>(), It.IsAny<decimal>(), default),
            Times.Never);
    }
}
