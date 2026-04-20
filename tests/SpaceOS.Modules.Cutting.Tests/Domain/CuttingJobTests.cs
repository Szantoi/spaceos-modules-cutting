using FluentAssertions;
using SpaceOS.Modules.Cutting.Domain.Aggregates;
using Xunit;

namespace SpaceOS.Modules.Cutting.Tests.Domain;

public class CuttingJobTests
{
    private static readonly DateTime TodayUtc = DateTime.SpecifyKind(DateTime.UtcNow.Date, DateTimeKind.Utc);

    [Fact]
    public void Create_WithValidData_ShouldBePending()
    {
        var job = CuttingJob.Create(Guid.NewGuid(), Guid.NewGuid(), TodayUtc, "Normal", 2m);
        job.Status.Should().Be("Pending");
    }

    [Fact]
    public void Create_WithEmptyOrderId_ShouldThrow()
    {
        var act = () => CuttingJob.Create(Guid.NewGuid(), Guid.Empty, TodayUtc, "Normal", 2m);
        act.Should().Throw<ArgumentException>().WithParameterName("orderId");
    }

    [Fact]
    public void Create_WithZeroEstimatedHours_ShouldThrow()
    {
        var act = () => CuttingJob.Create(Guid.NewGuid(), Guid.NewGuid(), TodayUtc, "Normal", 0m);
        act.Should().Throw<ArgumentException>().WithParameterName("estimatedTimeHours");
    }

    [Fact]
    public void Create_WithNegativeEstimatedHours_ShouldThrow()
    {
        var act = () => CuttingJob.Create(Guid.NewGuid(), Guid.NewGuid(), TodayUtc, "Normal", -1m);
        act.Should().Throw<ArgumentException>().WithParameterName("estimatedTimeHours");
    }

    [Fact]
    public void Create_WithInvalidPriority_ShouldThrow()
    {
        var act = () => CuttingJob.Create(Guid.NewGuid(), Guid.NewGuid(), TodayUtc, "Critical", 1m);
        act.Should().Throw<ArgumentException>().WithParameterName("priority");
    }

    [Theory]
    [InlineData("Urgent")]
    [InlineData("High")]
    [InlineData("Normal")]
    [InlineData("Low")]
    public void Create_WithValidPriority_ShouldSucceed(string priority)
    {
        var job = CuttingJob.Create(Guid.NewGuid(), Guid.NewGuid(), TodayUtc, priority, 1m);
        job.Priority.Should().Be(priority);
    }

    [Fact]
    public void Create_ShouldSetScheduledDateAsUtc()
    {
        var job = CuttingJob.Create(Guid.NewGuid(), Guid.NewGuid(), TodayUtc, "Normal", 1m);
        job.ScheduledDate.Kind.Should().Be(DateTimeKind.Utc);
    }

    // ── MarkAsCut ─────────────────────────────────────────────────────────────

    [Fact]
    public void MarkAsCut_FromPending_ShouldTransitionToCut()
    {
        var job = CuttingJob.Create(Guid.NewGuid(), Guid.NewGuid(), TodayUtc, "Normal", 2m);

        job.MarkAsCut();

        job.Status.Should().Be("Cut");
    }

    [Fact]
    public void MarkAsCut_AlreadyCut_ShouldThrowInvalidOperationException()
    {
        var job = CuttingJob.Create(Guid.NewGuid(), Guid.NewGuid(), TodayUtc, "Normal", 2m);
        job.MarkAsCut();

        var act = () => job.MarkAsCut();

        act.Should().Throw<InvalidOperationException>().WithMessage("*already*");
    }
}
