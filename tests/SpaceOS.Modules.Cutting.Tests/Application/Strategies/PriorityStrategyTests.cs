using FluentAssertions;
using SpaceOS.Modules.Cutting.Application.Strategies;
using SpaceOS.Modules.Cutting.Domain.Aggregates;
using Xunit;

namespace SpaceOS.Modules.Cutting.Tests.Application.Strategies;

public class PriorityStrategyTests
{
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly DateTime TodayUtc = DateTime.SpecifyKind(DateTime.UtcNow.Date, DateTimeKind.Utc);

    private static CuttingJob MakeJob(string priority, decimal hours = 2m)
        => CuttingJob.Create(Guid.Empty, Guid.NewGuid(), TodayUtc, priority, hours);

    [Fact]
    public async Task ScheduleJobsAsync_SortsByPriorityAscThenDateAsc()
    {
        var strategy = new PriorityStrategy();
        var plan = CuttingPlan.Create(TenantId, TodayUtc, 7, "priority");
        var slots = plan.DailyPlans.ToList();

        var lowJob    = MakeJob("Low");
        var urgentJob = MakeJob("Urgent");
        var normalJob = MakeJob("Normal");

        var scheduled = (await strategy.ScheduleJobsAsync(
            new[] { lowJob, urgentJob, normalJob }, slots, default)).ToList();

        // Urgent = rank 1 → allocated first
        scheduled[0].Priority.Should().Be("Urgent");
        scheduled[1].Priority.Should().Be("Normal");
        scheduled[2].Priority.Should().Be("Low");
    }

    [Fact]
    public async Task ScheduleJobsAsync_UrgentJobsAllocatedFirst()
    {
        var strategy = new PriorityStrategy();
        var plan = CuttingPlan.Create(TenantId, TodayUtc, 7, "priority");

        // Limit capacity: each slot 8h, urgent job 7h
        var urgentJob  = MakeJob("Urgent",  7m);
        var regularJob = MakeJob("Normal",  7m);

        var scheduled = (await strategy.ScheduleJobsAsync(
            new[] { regularJob, urgentJob }, plan.DailyPlans, default)).ToList();

        // Both fit (different slots), but urgent should appear first
        scheduled.Should().HaveCount(2);
        scheduled[0].Priority.Should().Be("Urgent");
    }

    [Fact]
    public async Task CalculateYield_WhenAllocated_IsPositive()
    {
        var strategy = new PriorityStrategy();
        var plan = CuttingPlan.Create(TenantId, TodayUtc, 7, "priority");
        var slots = plan.DailyPlans.ToList();

        var jobs = slots.Select(_ => MakeJob("High", 6m)).ToList();
        var scheduled = (await strategy.ScheduleJobsAsync(jobs, slots, default)).ToList();

        foreach (var job in scheduled)
            slots.First(d => d.Id == job.DailyPlanId).AddJob(job);

        var yield = strategy.CalculateYield(plan, slots);
        yield.Should().BeGreaterThan(0m, "scheduled jobs consume capacity");
    }

    [Fact]
    public async Task ValidateAsync_WhenPlanIsValid_ReturnsValid()
    {
        var strategy = new PriorityStrategy();
        var plan = CuttingPlan.Create(TenantId, TodayUtc, 7, "priority");

        var result = await strategy.ValidateAsync(plan, default);

        result.IsValid.Should().BeTrue();
        strategy.GetLabel().Should().Be("Priority (By Due Date + Urgency)");
    }
}
