using FluentAssertions;
using SpaceOS.Modules.Cutting.Application.Strategies;
using SpaceOS.Modules.Cutting.Domain.Aggregates;
using SpaceOS.Modules.Cutting.Domain.Services;
using Xunit;

namespace SpaceOS.Modules.Cutting.Tests.Application.Strategies;

public class FIFOStrategyTests
{
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly DateTime TodayUtc = DateTime.SpecifyKind(DateTime.UtcNow.Date, DateTimeKind.Utc);

    private static CuttingJob MakeJob(DateTime scheduledDate, decimal hours = 2m, string priority = "Normal")
        => CuttingJob.Create(Guid.Empty, Guid.NewGuid(), scheduledDate, priority, hours);

    [Fact]
    public async Task ScheduleJobsAsync_SortsByScheduledDateAscending()
    {
        var strategy = new FIFOStrategy();
        var plan = CuttingPlan.Create(TenantId, TodayUtc, 7, "fifo");
        var slots = plan.DaySlots.ToList();

        var newerJob = MakeJob(TodayUtc.AddDays(5), 2m);
        var olderJob = MakeJob(TodayUtc, 2m);

        var scheduled = (await strategy.ScheduleJobsAsync(
            new[] { newerJob, olderJob }, slots, default)).ToList();

        scheduled[0].OrderId.Should().Be(olderJob.OrderId);
        scheduled[1].OrderId.Should().Be(newerJob.OrderId);
    }

    [Fact]
    public async Task ScheduleJobsAsync_AllocatesFIFOOrder()
    {
        var strategy = new FIFOStrategy();
        var plan = CuttingPlan.Create(TenantId, TodayUtc, 7, "fifo");

        var jobs = Enumerable.Range(0, 7)
            .Select(i => MakeJob(TodayUtc.AddDays(i), 4m))
            .ToList();

        var scheduled = (await strategy.ScheduleJobsAsync(jobs, plan.DaySlots, default)).ToList();

        scheduled.Should().HaveCount(7);
    }

    [Fact]
    public async Task CalculateYield_WhenPartiallyFilled_IsLowerThanMaxCut()
    {
        var strategy = new FIFOStrategy();
        var plan = CuttingPlan.Create(TenantId, TodayUtc, 7, "fifo");
        var slots = plan.DaySlots.ToList();

        var jobs = slots.Select(d =>
            MakeJob(d.SlotDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc), 4m)).ToList();
        var scheduled = (await strategy.ScheduleJobsAsync(jobs, slots, default)).ToList();

        foreach (var job in scheduled)
            slots.First(d => d.Id == job.DaySlotId).AddJob(job, new AreaCapacityModel());

        var yield = strategy.CalculateYield(plan, slots);
        yield.Should().BeGreaterThan(0m).And.BeLessThan(100m);
    }

    [Fact]
    public async Task ValidateAsync_WhenPlanIsValid_ReturnsValid()
    {
        var strategy = new FIFOStrategy();
        var plan = CuttingPlan.Create(TenantId, TodayUtc, 7, "fifo");

        var result = await strategy.ValidateAsync(plan, default);

        result.IsValid.Should().BeTrue();
        strategy.GetLabel().Should().Be("FIFO (First-In-First-Out)");
    }
}
