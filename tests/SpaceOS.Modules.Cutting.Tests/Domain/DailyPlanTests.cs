using FluentAssertions;
using SpaceOS.Modules.Cutting.Domain.Aggregates;
using Xunit;

namespace SpaceOS.Modules.Cutting.Tests.Domain;

public class DailyPlanTests
{
    private static readonly DateTime TodayUtc = DateTime.SpecifyKind(DateTime.UtcNow.Date, DateTimeKind.Utc);

    [Fact]
    public void Create_ShouldSetDefaultCapacityOf8()
    {
        var daily = DailyPlan.Create(Guid.NewGuid(), TodayUtc);
        daily.AvailableCapacity.Should().Be(8m);
    }

    [Fact]
    public void Create_ShouldSetDateAsUtc()
    {
        var daily = DailyPlan.Create(Guid.NewGuid(), TodayUtc);
        daily.Date.Kind.Should().Be(DateTimeKind.Utc);
    }

    [Fact]
    public void AllocatedCapacity_WithNoJobs_ShouldBeZero()
    {
        var daily = DailyPlan.Create(Guid.NewGuid(), TodayUtc);
        daily.AllocatedCapacity.Should().Be(0m);
    }

    [Fact]
    public void UtilizationPercent_WithNoJobs_ShouldBeZero()
    {
        var daily = DailyPlan.Create(Guid.NewGuid(), TodayUtc);
        daily.UtilizationPercent.Should().Be(0m);
    }

    [Fact]
    public void AddJob_WithMatchingDailyPlanId_ShouldAddJob()
    {
        var daily = DailyPlan.Create(Guid.NewGuid(), TodayUtc);
        var job = CuttingJob.Create(daily.Id, Guid.NewGuid(), TodayUtc, "Normal", 2m);
        daily.AddJob(job);
        daily.Jobs.Should().HaveCount(1);
    }

    [Fact]
    public void AddJob_WithWrongDailyPlanId_ShouldThrow()
    {
        var daily = DailyPlan.Create(Guid.NewGuid(), TodayUtc);
        var job = CuttingJob.Create(Guid.NewGuid(), Guid.NewGuid(), TodayUtc, "Normal", 2m);
        var act = () => daily.AddJob(job);
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void AllocatedCapacity_AfterAddingJobs_ShouldSumEstimatedHours()
    {
        var daily = DailyPlan.Create(Guid.NewGuid(), TodayUtc);
        var job1 = CuttingJob.Create(daily.Id, Guid.NewGuid(), TodayUtc, "Normal", 2m);
        var job2 = CuttingJob.Create(daily.Id, Guid.NewGuid(), TodayUtc, "High", 3m);
        daily.AddJob(job1);
        daily.AddJob(job2);
        daily.AllocatedCapacity.Should().Be(5m);
    }

    [Fact]
    public void UtilizationPercent_With4HoursOf8_ShouldBe50Percent()
    {
        var daily = DailyPlan.Create(Guid.NewGuid(), TodayUtc);
        var job = CuttingJob.Create(daily.Id, Guid.NewGuid(), TodayUtc, "Normal", 4m);
        daily.AddJob(job);
        daily.UtilizationPercent.Should().Be(50m);
    }
}
