using FluentAssertions;
using SpaceOS.Modules.Cutting.Domain.Aggregates;
using Xunit;

namespace SpaceOS.Modules.Cutting.Tests.Domain;

public class CuttingPlanTests
{
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly DateTime TodayUtc = DateTime.SpecifyKind(DateTime.UtcNow.Date, DateTimeKind.Utc);

    // --- Creation ---

    [Fact]
    public void Create_WithValidData_ShouldBeDraft()
    {
        var plan = CuttingPlan.Create(TenantId, TodayUtc, 14, "maxcut-v1");
        plan.Status.Should().Be("Draft");
        plan.Id.Should().NotBeEmpty();
    }

    [Fact]
    public void Create_WithValidData_ShouldGenerateDailyPlans()
    {
        var plan = CuttingPlan.Create(TenantId, TodayUtc, 14, "maxcut-v1");
        plan.DailyPlans.Should().HaveCount(14);
    }

    [Fact]
    public void Create_DailyPlans_ShouldHaveConsecutiveDates()
    {
        var plan = CuttingPlan.Create(TenantId, TodayUtc, 7, "fifo");
        for (int i = 0; i < 7; i++)
            plan.DailyPlans[i].Date.Should().Be(TodayUtc.AddDays(i));
    }

    [Fact]
    public void Create_DailyPlans_ShouldHaveDefaultCapacityOf8()
    {
        var plan = CuttingPlan.Create(TenantId, TodayUtc, 7, "fifo");
        plan.DailyPlans.Should().OnlyContain(d => d.AvailableCapacity == 8m);
    }

    [Fact]
    public void Create_WithMaxDays_ShouldGenerate90DailyPlans()
    {
        var plan = CuttingPlan.Create(TenantId, TodayUtc, 90, "priority");
        plan.DailyPlans.Should().HaveCount(90);
    }

    [Fact]
    public void Create_WithMinDays_ShouldGenerate7DailyPlans()
    {
        var plan = CuttingPlan.Create(TenantId, TodayUtc, 7, "custom");
        plan.DailyPlans.Should().HaveCount(7);
    }

    [Fact]
    public void Create_ShouldSetUtcDateKind()
    {
        var plan = CuttingPlan.Create(TenantId, TodayUtc, 7, "fifo");
        plan.PlanDate.Kind.Should().Be(DateTimeKind.Utc);
    }

    // --- Validation ---

    [Fact]
    public void Create_WithEmptyTenantId_ShouldThrow()
    {
        var act = () => CuttingPlan.Create(Guid.Empty, TodayUtc, 14, "maxcut-v1");
        act.Should().Throw<ArgumentException>().WithParameterName("tenantId");
    }

    [Fact]
    public void Create_WithPlanDaysBelow7_ShouldThrow()
    {
        var act = () => CuttingPlan.Create(TenantId, TodayUtc, 6, "maxcut-v1");
        act.Should().Throw<ArgumentException>().WithParameterName("planDays");
    }

    [Fact]
    public void Create_WithPlanDaysAbove90_ShouldThrow()
    {
        var act = () => CuttingPlan.Create(TenantId, TodayUtc, 91, "maxcut-v1");
        act.Should().Throw<ArgumentException>().WithParameterName("planDays");
    }

    [Fact]
    public void Create_WithEmptyStrategyId_ShouldThrow()
    {
        var act = () => CuttingPlan.Create(TenantId, TodayUtc, 14, "");
        act.Should().Throw<ArgumentException>().WithParameterName("strategyId");
    }

    [Fact]
    public void Create_WithWhitespaceStrategyId_ShouldThrow()
    {
        var act = () => CuttingPlan.Create(TenantId, TodayUtc, 14, "   ");
        act.Should().Throw<ArgumentException>().WithParameterName("strategyId");
    }

    [Fact]
    public void Create_WithPastPlanDate_ShouldThrow()
    {
        var yesterday = TodayUtc.AddDays(-1);
        var act = () => CuttingPlan.Create(TenantId, yesterday, 14, "maxcut-v1");
        act.Should().Throw<ArgumentException>().WithParameterName("planDate");
    }

    // --- Status transitions ---

    [Fact]
    public void UpdateStatus_ToDraft_ShouldSucceed()
    {
        var plan = CuttingPlan.Create(TenantId, TodayUtc, 14, "maxcut-v1");
        plan.UpdateStatus("Draft");
        plan.Status.Should().Be("Draft");
    }

    [Fact]
    public void UpdateStatus_ToApproved_ShouldSucceed()
    {
        var plan = CuttingPlan.Create(TenantId, TodayUtc, 14, "maxcut-v1");
        plan.UpdateStatus("Approved");
        plan.Status.Should().Be("Approved");
    }

    [Fact]
    public void UpdateStatus_ToInProgress_ShouldSucceed()
    {
        var plan = CuttingPlan.Create(TenantId, TodayUtc, 14, "maxcut-v1");
        plan.UpdateStatus("InProgress");
        plan.Status.Should().Be("InProgress");
    }

    [Fact]
    public void UpdateStatus_ToClosed_ShouldSucceed()
    {
        var plan = CuttingPlan.Create(TenantId, TodayUtc, 14, "maxcut-v1");
        plan.UpdateStatus("Closed");
        plan.Status.Should().Be("Closed");
    }

    [Fact]
    public void UpdateStatus_ToInvalidValue_ShouldThrow()
    {
        var plan = CuttingPlan.Create(TenantId, TodayUtc, 14, "maxcut-v1");
        var act = () => plan.UpdateStatus("Unknown");
        act.Should().Throw<ArgumentException>().WithParameterName("newStatus");
    }

    [Fact]
    public void UpdateStatus_ShouldUpdateUpdatedAt()
    {
        var plan = CuttingPlan.Create(TenantId, TodayUtc, 7, "fifo");
        var before = plan.UpdatedAt;
        plan.UpdateStatus("Approved");
        plan.UpdatedAt.Should().BeOnOrAfter(before);
    }

    // --- Immutability ---

    [Fact]
    public void CuttingPlan_ShouldHaveNoPublicSetters()
    {
        typeof(CuttingPlan).GetProperties()
            .Where(p => p.CanWrite && p.GetSetMethod()?.IsPublic == true)
            .Should().BeEmpty("CuttingPlan must have no public setters");
    }

    [Fact]
    public void DailyPlan_ShouldHaveNoPublicSetters()
    {
        typeof(DailyPlan).GetProperties()
            .Where(p => p.CanWrite && p.GetSetMethod()?.IsPublic == true)
            .Should().BeEmpty("DailyPlan must have no public setters");
    }

    [Fact]
    public void CuttingJob_ShouldHaveNoPublicSetters()
    {
        typeof(CuttingJob).GetProperties()
            .Where(p => p.CanWrite && p.GetSetMethod()?.IsPublic == true)
            .Should().BeEmpty("CuttingJob must have no public setters");
    }
}
