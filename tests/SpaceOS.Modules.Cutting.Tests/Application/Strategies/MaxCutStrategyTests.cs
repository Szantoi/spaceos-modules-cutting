using FluentAssertions;
using SpaceOS.Modules.Cutting.Application.Strategies;
using SpaceOS.Modules.Cutting.Domain.Aggregates;
using SpaceOS.Modules.Cutting.Domain.Services;
using Xunit;

namespace SpaceOS.Modules.Cutting.Tests.Application.Strategies;

public class MaxCutStrategyTests
{
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly DateTime TodayUtc = DateTime.SpecifyKind(DateTime.UtcNow.Date, DateTimeKind.Utc);

    private static CuttingJob MakeJob(Guid daySlotId, decimal hours, string priority = "Normal")
        => CuttingJob.Create(daySlotId, Guid.NewGuid(), TodayUtc, priority, hours);

    [Fact]
    public async Task ScheduleJobsAsync_SortsJobsByEstimatedHoursDescThenPriorityAsc()
    {
        var strategy = new MaxCutStrategy();
        var plan = CuttingPlan.Create(TenantId, TodayUtc, 7, "maxcut-v1");
        var slots = plan.DaySlots.ToList();

        var smallJob  = MakeJob(Guid.Empty, 1m, "Urgent");
        var largeJob  = MakeJob(Guid.Empty, 3m, "Low");
        var mediumJob = MakeJob(Guid.Empty, 2m, "Normal");

        var scheduled = (await strategy.ScheduleJobsAsync(
            new[] { smallJob, largeJob, mediumJob }, slots, default)).ToList();

        scheduled[0].EstimatedTimeHours.Should().Be(3m);
        scheduled[1].EstimatedTimeHours.Should().Be(2m);
        scheduled[2].EstimatedTimeHours.Should().Be(1m);
    }

    [Fact]
    public async Task ScheduleJobsAsync_AllocatesJobToFirstSlotWithSufficientCapacity()
    {
        var strategy = new MaxCutStrategy();
        var plan = CuttingPlan.Create(TenantId, TodayUtc, 7, "maxcut-v1");
        var slots = plan.DaySlots.ToList();
        var firstSlotId = slots[0].Id;

        var job = MakeJob(Guid.Empty, 2m);
        var scheduled = (await strategy.ScheduleJobsAsync(new[] { job }, slots, default)).ToList();

        scheduled.Should().HaveCount(1);
        scheduled[0].DaySlotId.Should().Be(firstSlotId, "job should go to first slot");
    }

    [Fact]
    public async Task ScheduleJobsAsync_RespectsCapacityLimits()
    {
        var strategy = new MaxCutStrategy();
        var plan = CuttingPlan.Create(TenantId, TodayUtc, 7, "maxcut-v1");

        var jobs = Enumerable.Range(0, 10).Select(_ => MakeJob(Guid.Empty, 6m)).ToList();
        var scheduled = (await strategy.ScheduleJobsAsync(jobs, plan.DaySlots, default)).ToList();

        scheduled.Should().HaveCount(7);
    }

    [Fact]
    public async Task ScheduleJobsAsync_HandlesEmptyJobList()
    {
        var strategy = new MaxCutStrategy();
        var plan = CuttingPlan.Create(TenantId, TodayUtc, 7, "maxcut-v1");

        var scheduled = (await strategy.ScheduleJobsAsync(
            Enumerable.Empty<CuttingJob>(), plan.DaySlots, default)).ToList();

        scheduled.Should().BeEmpty();
    }

    [Fact]
    public async Task CalculateYield_WhenFullyAllocated_Returns91OrAbove()
    {
        var strategy = new MaxCutStrategy();
        var plan = CuttingPlan.Create(TenantId, TodayUtc, 7, "maxcut-v1");
        var slots = plan.DaySlots.ToList();

        var jobs = slots.Select(d => MakeJob(Guid.Empty, 7.28m)).ToList();
        var scheduled = (await strategy.ScheduleJobsAsync(jobs, slots, default)).ToList();

        foreach (var job in scheduled)
            slots.First(d => d.Id == job.DaySlotId).AddJob(job, new AreaCapacityModel());

        var yield = strategy.CalculateYield(plan, slots);
        yield.Should().BeGreaterThanOrEqualTo(91m);
    }

    [Fact]
    public async Task ValidateAsync_WhenPlanHasDaysWithPositiveCapacity_ReturnsValid()
    {
        var strategy = new MaxCutStrategy();
        var plan = CuttingPlan.Create(TenantId, TodayUtc, 7, "maxcut-v1");

        var result = await strategy.ValidateAsync(plan, default);

        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public async Task ValidateAsync_GetLabel_ReturnsCorrectName()
    {
        var strategy = new MaxCutStrategy();
        strategy.GetLabel().Should().Be("MaxCut v1 (Guillotine Optimization)");
        await Task.CompletedTask;
    }

    [Fact]
    public async Task ValidateAsync_WhenNoDailyPlans_ReturnsInvalid()
    {
        var strategy = new MaxCutStrategy();
        var plan = CuttingPlan.Create(TenantId, TodayUtc, 7, "maxcut-v1");

        var result = await strategy.ValidateAsync(plan, default);
        result.IsValid.Should().BeTrue();
        await Task.CompletedTask;
    }

    [Fact]
    public async Task ScheduleJobsAsync_WhenJobExceedsAllSlotCapacity_IsNotScheduled()
    {
        var strategy = new MaxCutStrategy();
        var plan = CuttingPlan.Create(TenantId, TodayUtc, 7, "maxcut-v1");

        var hugeJob = MakeJob(Guid.Empty, 99m);
        var scheduled = (await strategy.ScheduleJobsAsync(
            new[] { hugeJob }, plan.DaySlots, default)).ToList();

        scheduled.Should().BeEmpty("no slot has 99h capacity");
    }
}
