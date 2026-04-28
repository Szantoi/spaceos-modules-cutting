using FluentAssertions;
using SpaceOS.Modules.Cutting.Analytics.Domain.Aggregates;
using SpaceOS.Modules.Cutting.Analytics.Domain.ValueObjects;
using Xunit;

namespace SpaceOS.Modules.Cutting.Tests.Analytics.Domain;

public class AnalyticsRebuildJobTests
{
    private static readonly Guid TenantId = Guid.NewGuid();

    [Fact]
    public void Create_SetsPendingStatus()
    {
        var job = AnalyticsRebuildJob.Create(TenantId);
        job.Status.Should().Be(RebuildJobStatus.Pending);
    }

    [Fact]
    public void Start_FromPending_TransitionsToRunning()
    {
        var job = AnalyticsRebuildJob.Create(TenantId);
        var result = job.Start(10);
        result.IsSuccess.Should().BeTrue();
        job.Status.Should().Be(RebuildJobStatus.Running);
        job.TotalChunks.Should().Be(10);
        job.StartedAt.Should().NotBeNull();
    }

    [Fact]
    public void Start_WhenAlreadyRunning_ReturnsInvalid()
    {
        var job = AnalyticsRebuildJob.Create(TenantId);
        job.Start(5);
        var result = job.Start(5);
        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public void Start_WhenCompleted_ReturnsInvalid()
    {
        var job = AnalyticsRebuildJob.Create(TenantId);
        job.Start(1);
        job.Complete();
        var result = job.Start(1);
        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public void RecordChunkProgress_IncrementsProcessedChunks()
    {
        var job = AnalyticsRebuildJob.Create(TenantId);
        job.Start(5);
        job.RecordChunkProgress();
        job.RecordChunkProgress();
        job.ProcessedChunks.Should().Be(2);
    }

    [Fact]
    public void RecordChunkProgress_WhenPending_ReturnsInvalid()
    {
        var job = AnalyticsRebuildJob.Create(TenantId);
        var result = job.RecordChunkProgress();
        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public void Complete_FromRunning_SetsCompletedStatus()
    {
        var job = AnalyticsRebuildJob.Create(TenantId);
        job.Start(3);
        var result = job.Complete();
        result.IsSuccess.Should().BeTrue();
        job.Status.Should().Be(RebuildJobStatus.Completed);
        job.CompletedAt.Should().NotBeNull();
    }

    [Fact]
    public void Complete_FromPending_ReturnsInvalid()
    {
        var job = AnalyticsRebuildJob.Create(TenantId);
        var result = job.Complete();
        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public void Fail_SetsFailedStatusAndErrorMessage()
    {
        var job = AnalyticsRebuildJob.Create(TenantId);
        job.Start(2);
        var result = job.Fail("Timeout");
        result.IsSuccess.Should().BeTrue();
        job.Status.Should().Be(RebuildJobStatus.Failed);
        job.ErrorMessage.Should().Be("Timeout");
        job.CompletedAt.Should().NotBeNull();
    }

    [Fact]
    public void Fail_WhenAlreadyCompleted_ReturnsInvalid()
    {
        var job = AnalyticsRebuildJob.Create(TenantId);
        job.Start(1);
        job.Complete();
        var result = job.Fail("late error");
        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public void Create_EmptyTenantId_ThrowsArgumentException()
    {
        var act = () => AnalyticsRebuildJob.Create(Guid.Empty);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void RecordChunkProgress_MultipleCallsAccumulateCorrectly()
    {
        var job = AnalyticsRebuildJob.Create(TenantId);
        job.Start(10);
        for (var i = 0; i < 7; i++) job.RecordChunkProgress();
        job.ProcessedChunks.Should().Be(7);
    }
}
