using FluentAssertions;
using SpaceOS.Modules.Cutting.Application.Strategies;
using SpaceOS.Modules.Cutting.Domain.Aggregates;
using SpaceOS.Modules.Cutting.Domain.Services;
using Xunit;

namespace SpaceOS.Modules.Cutting.Tests.Application.Strategies;

public class CustomStrategyTests
{
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly DateTime TodayUtc = DateTime.SpecifyKind(DateTime.UtcNow.Date, DateTimeKind.Utc);

    private static CuttingJob MakeJob(decimal hours = 2m)
        => CuttingJob.Create(Guid.Empty, Guid.NewGuid(), TodayUtc, "Normal", hours);

    [Fact]
    public async Task ScheduleJobsAsync_DelegatesToMaxCut()
    {
        var custom  = new CustomStrategy();
        var maxcut  = new MaxCutStrategy();
        var plan    = CuttingPlan.Create(TenantId, TodayUtc, 7, "custom");
        var slots   = plan.DaySlots.ToList();
        var jobs    = slots.Select(_ => MakeJob(7.28m)).ToList();

        var customResult = (await custom.ScheduleJobsAsync(jobs, slots, default)).ToList();
        var maxcutResult = (await maxcut.ScheduleJobsAsync(jobs, slots, default)).ToList();

        customResult.Should().HaveCount(maxcutResult.Count,
            "CustomStrategy delegates to MaxCutStrategy in v1");
    }

    [Fact]
    public void GetLabel_ReturnsCorrectName()
    {
        var strategy = new CustomStrategy();
        strategy.GetLabel().Should().Be("Custom (Tenant-Specific)");
    }

    [Fact]
    public async Task ValidateAsync_DelegatesToMaxCut()
    {
        var strategy = new CustomStrategy();
        var plan     = CuttingPlan.Create(TenantId, TodayUtc, 7, "custom");

        var result = await strategy.ValidateAsync(plan, default);

        result.IsValid.Should().BeTrue("MaxCut validation passes for a valid plan");
    }

    [Fact]
    public async Task CalculateYield_DelegatesToMaxCut()
    {
        var custom = new CustomStrategy();
        var maxcut = new MaxCutStrategy();
        var plan   = CuttingPlan.Create(TenantId, TodayUtc, 7, "custom");
        var slots  = plan.DaySlots.ToList();

        var jobs = slots.Select(_ => MakeJob(4m)).ToList();
        var scheduled = (await custom.ScheduleJobsAsync(jobs, slots, default)).ToList();
        foreach (var job in scheduled)
            slots.First(d => d.Id == job.DaySlotId).AddJob(job, new AreaCapacityModel());

        var customYield = custom.CalculateYield(plan, slots);
        var maxcutYield = maxcut.CalculateYield(plan, slots);

        customYield.Should().Be(maxcutYield, "CustomStrategy delegates yield calc to MaxCutStrategy");
    }
}
