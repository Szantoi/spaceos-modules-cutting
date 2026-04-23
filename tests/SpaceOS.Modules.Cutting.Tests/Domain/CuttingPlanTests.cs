using FluentAssertions;
using SpaceOS.Modules.Cutting.Domain.Aggregates;
using SpaceOS.Modules.Cutting.Domain.Entities;
using SpaceOS.Modules.Cutting.Domain.Enums;
using SpaceOS.Modules.Cutting.Domain.Events;
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
        plan.Status.Should().Be(CuttingPlanStatus.Draft);
        plan.Id.Should().NotBeEmpty();
    }

    [Fact]
    public void Create_WithValidData_ShouldGenerateDaySlots()
    {
        var plan = CuttingPlan.Create(TenantId, TodayUtc, 14, "maxcut-v1");
        plan.DaySlots.Should().HaveCount(14);
    }

    [Fact]
    public void Create_DaySlots_ShouldHaveConsecutiveDates()
    {
        var plan = CuttingPlan.Create(TenantId, TodayUtc, 7, "fifo");
        for (int i = 0; i < 7; i++)
            plan.DaySlots[i].SlotDate.Should().Be(DateOnly.FromDateTime(TodayUtc.AddDays(i)));
    }

    [Fact]
    public void Create_DaySlots_ShouldHaveDefaultCapacityOf8()
    {
        var plan = CuttingPlan.Create(TenantId, TodayUtc, 7, "fifo");
        plan.DaySlots.Should().OnlyContain(d => d.CapacityHours == 8m);
    }

    [Fact]
    public void Create_WithMaxDays_ShouldGenerate90DaySlots()
    {
        var plan = CuttingPlan.Create(TenantId, TodayUtc, 90, "priority");
        plan.DaySlots.Should().HaveCount(90);
    }

    [Fact]
    public void Create_WithMinDays_ShouldGenerate7DaySlots()
    {
        var plan = CuttingPlan.Create(TenantId, TodayUtc, 7, "custom");
        plan.DaySlots.Should().HaveCount(7);
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

    // --- Status transitions (UpdateStatus — obsolete, kept for backwards-compat test coverage) ---

#pragma warning disable CS0618
    [Fact]
    public void UpdateStatus_ToDraft_ShouldSucceed()
    {
        var plan = CuttingPlan.Create(TenantId, TodayUtc, 14, "maxcut-v1");
        plan.UpdateStatus(CuttingPlanStatus.Draft);
        plan.Status.Should().Be(CuttingPlanStatus.Draft);
    }

    [Fact]
    public void UpdateStatus_ToPublished_ShouldSucceed()
    {
        var plan = CuttingPlan.Create(TenantId, TodayUtc, 14, "maxcut-v1");
        plan.UpdateStatus(CuttingPlanStatus.Published);
        plan.Status.Should().Be(CuttingPlanStatus.Published);
    }

    [Fact]
    public void UpdateStatus_ToFrozen_ShouldSucceed()
    {
        var plan = CuttingPlan.Create(TenantId, TodayUtc, 14, "maxcut-v1");
        plan.UpdateStatus(CuttingPlanStatus.Frozen);
        plan.Status.Should().Be(CuttingPlanStatus.Frozen);
    }

    [Fact]
    public void UpdateStatus_ToClosed_ShouldSucceed()
    {
        var plan = CuttingPlan.Create(TenantId, TodayUtc, 14, "maxcut-v1");
        plan.UpdateStatus(CuttingPlanStatus.Closed);
        plan.Status.Should().Be(CuttingPlanStatus.Closed);
    }

    [Fact]
    public void UpdateStatus_ShouldUpdateUpdatedAt()
    {
        var plan = CuttingPlan.Create(TenantId, TodayUtc, 7, "fifo");
        var before = plan.UpdatedAt;
        plan.UpdateStatus(CuttingPlanStatus.Published);
        plan.UpdatedAt.Should().BeOnOrAfter(before);
    }
#pragma warning restore CS0618

    // --- Enum integrity ---

    [Fact]
    public void CuttingPlanStatus_HasExpectedIntValues()
    {
        ((int)CuttingPlanStatus.Draft).Should().Be(0);
        ((int)CuttingPlanStatus.Published).Should().Be(1);
        ((int)CuttingPlanStatus.Frozen).Should().Be(2);
        ((int)CuttingPlanStatus.Closed).Should().Be(3);
    }

#pragma warning disable CS0618
    [Fact]
    public void UpdateStatus_AllEnumValues_AreAccepted()
    {
        var plan = CuttingPlan.Create(TenantId, TodayUtc, 7, "maxcut-v1");
        var values = Enum.GetValues<CuttingPlanStatus>();
        foreach (var v in values)
        {
            var act = () => plan.UpdateStatus(v);
            act.Should().NotThrow($"{v} must be a valid status transition");
        }
    }
#pragma warning restore CS0618

    // --- FSM: Publish ---

    [Fact]
    public void Publish_FromDraft_WithValidArgs_ShouldSucceed()
    {
        var plan = CuttingPlan.Create(TenantId, TodayUtc, 7, "maxcut-v1");
        var profileId = Guid.NewGuid();

        var result = plan.Publish(profileId);

        result.IsSuccess.Should().BeTrue();
        plan.Status.Should().Be(CuttingPlanStatus.Published);
        plan.ProfileSnapshotId.Should().Be(profileId);
    }

    [Fact]
    public void Publish_FromPublished_ShouldReturnInvalid()
    {
        var plan = CuttingPlan.Create(TenantId, TodayUtc, 7, "maxcut-v1");
        plan.Publish(Guid.NewGuid());

        var result = plan.Publish(Guid.NewGuid());

        result.IsSuccess.Should().BeFalse("can only publish Draft plans");
    }

    [Fact]
    public void Publish_WithEmptyProfileSnapshotId_ShouldReturnInvalid()
    {
        var plan = CuttingPlan.Create(TenantId, TodayUtc, 7, "maxcut-v1");

        var result = plan.Publish(Guid.Empty);

        result.IsSuccess.Should().BeFalse("ProfileSnapshotId is required");
    }

    [Fact]
    public void Publish_SetsProfileSnapshotId()
    {
        var plan = CuttingPlan.Create(TenantId, TodayUtc, 7, "maxcut-v1");
        var profileId = Guid.NewGuid();

        plan.Publish(profileId);

        plan.ProfileSnapshotId.Should().Be(profileId);
    }

    // --- FSM: Freeze ---

    [Fact]
    public void Freeze_FromPublished_ShouldSucceed()
    {
        var plan = CuttingPlan.Create(TenantId, TodayUtc, 7, "maxcut-v1");
        plan.Publish(Guid.NewGuid());

        var result = plan.Freeze();

        result.IsSuccess.Should().BeTrue();
        plan.Status.Should().Be(CuttingPlanStatus.Frozen);
    }

    [Fact]
    public void Freeze_FromDraft_ShouldReturnInvalid()
    {
        var plan = CuttingPlan.Create(TenantId, TodayUtc, 7, "maxcut-v1");

        var result = plan.Freeze();

        result.IsSuccess.Should().BeFalse("only Published plans can be frozen");
    }

    [Fact]
    public void Freeze_WhenNoOpenSlots_ShouldReturnInvalid()
    {
        var plan = CuttingPlan.Create(TenantId, TodayUtc, 7, "maxcut-v1");
        plan.Publish(Guid.NewGuid());
        // Lock all slots
        foreach (var slot in plan.DaySlots)
            slot.Lock();

        var result = plan.Freeze();

        result.IsSuccess.Should().BeFalse("needs at least one Open DaySlot");
    }

    // --- FSM: Close ---

    [Fact]
    public void Close_FromFrozen_WhenAllSlotsLocked_ShouldSucceed()
    {
        var plan = CuttingPlan.Create(TenantId, TodayUtc, 7, "maxcut-v1");
        plan.Publish(Guid.NewGuid());
        // Need at least one Open slot to Freeze...
        plan.Freeze();
        // Now lock all slots so Close can proceed
        foreach (var slot in plan.DaySlots)
            slot.Lock();

        var result = plan.Close();

        result.IsSuccess.Should().BeTrue();
        plan.Status.Should().Be(CuttingPlanStatus.Closed);
    }

    [Fact]
    public void Close_FromPublished_ShouldReturnInvalid()
    {
        var plan = CuttingPlan.Create(TenantId, TodayUtc, 7, "maxcut-v1");
        plan.Publish(Guid.NewGuid());

        var result = plan.Close();

        result.IsSuccess.Should().BeFalse("only Frozen plans can be closed");
    }

    [Fact]
    public void Close_WhenOpenSlotsExist_ShouldReturnInvalid()
    {
        var plan = CuttingPlan.Create(TenantId, TodayUtc, 7, "maxcut-v1");
        plan.Publish(Guid.NewGuid());
        plan.Freeze();
        // Leave slots Open — do not lock them

        var result = plan.Close();

        result.IsSuccess.Should().BeFalse("Open DaySlots must be Locked or Closed first");
    }

    [Fact]
    public void FSM_FullHappyPath_Draft_Published_Frozen_Closed()
    {
        var plan = CuttingPlan.Create(TenantId, TodayUtc, 7, "maxcut-v1");

        plan.Publish(Guid.NewGuid()).IsSuccess.Should().BeTrue();
        plan.Freeze().IsSuccess.Should().BeTrue();

        foreach (var slot in plan.DaySlots)
            slot.Lock();

        plan.Close().IsSuccess.Should().BeTrue();
        plan.Status.Should().Be(CuttingPlanStatus.Closed);
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
    public void DaySlot_ShouldHaveNoPublicSetters()
    {
        typeof(DaySlot).GetProperties()
            .Where(p => p.CanWrite && p.GetSetMethod()?.IsPublic == true)
            .Should().BeEmpty("DaySlot must have no public setters");
    }

    [Fact]
    public void CuttingJob_ShouldHaveNoPublicSetters()
    {
        typeof(CuttingJob).GetProperties()
            .Where(p => p.CanWrite && p.GetSetMethod()?.IsPublic == true)
            .Should().BeEmpty("CuttingJob must have no public setters");
    }

    // --- Domain event: CuttingPlanFrozen ---

    [Fact]
    public void Freeze_RaisesCuttingPlanFrozenDomainEvent()
    {
        var plan = CuttingPlan.Create(TenantId, TodayUtc, 7, "maxcut-v1");
        plan.Publish(Guid.NewGuid());

        plan.Freeze();

        var events = plan.DomainEvents;
        events.Should().ContainSingle(e => e is CuttingPlanFrozen,
            "Freeze() must raise exactly one CuttingPlanFrozen domain event");

        var frozen = (CuttingPlanFrozen)events.Single(e => e is CuttingPlanFrozen);
        frozen.PlanId.Should().Be(plan.Id);
        frozen.TenantId.Should().Be(TenantId);
        frozen.FrozenAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
    }
}
