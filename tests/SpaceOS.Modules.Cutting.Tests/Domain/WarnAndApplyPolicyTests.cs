using FluentAssertions;
using SpaceOS.Modules.Cutting.Domain.Aggregates;
using SpaceOS.Modules.Cutting.Domain.Entities;
using SpaceOS.Modules.Cutting.Domain.Services;
using Xunit;

namespace SpaceOS.Modules.Cutting.Tests.Domain;

public class WarnAndApplyPolicyTests
{
    private static readonly DateOnly Today = DateOnly.FromDateTime(DateTime.UtcNow.Date);
    private readonly AreaCapacityModel _capacityModel = new();

    private static DaySlot MakeOpenSlot(decimal capacityHours = 8m)
        => DaySlot.Create(Guid.NewGuid(), Today, capacityHours);

    private static CuttingJob MakeJob(Guid daySlotId, decimal estimatedHours = 2m)
        => CuttingJob.Create(daySlotId, Guid.NewGuid(),
            Today.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc), "Normal", estimatedHours);

    [Fact]
    public void PolicyId_ShouldBeWarnAndApplyV1()
    {
        var policy = new WarnAndApplyPolicy(_capacityModel);
        policy.PolicyId.Should().Be("warn-and-apply-v1");
    }

    [Fact]
    public void Evaluate_WhenSlotOpenAndHasCapacity_ReturnsCanRescheduleFalse()
    {
        var policy = new WarnAndApplyPolicy(_capacityModel);
        var slot = MakeOpenSlot();
        var job = MakeJob(slot.Id, 2m);

        var decision = policy.Evaluate(job, slot);

        decision.CanReschedule.Should().BeFalse();
        decision.TargetSlot.Should().Be(slot);
    }

    [Fact]
    public void Evaluate_WhenSlotLocked_ReturnsCanRescheduleTrue()
    {
        var policy = new WarnAndApplyPolicy(_capacityModel);
        var slot = MakeOpenSlot();
        slot.Lock();
        var job = MakeJob(slot.Id, 2m);

        var decision = policy.Evaluate(job, slot);

        decision.CanReschedule.Should().BeTrue();
        decision.TargetSlot.Should().BeNull();
        decision.Reason.Should().Contain("Locked");
    }

    [Fact]
    public void Evaluate_WhenSlotClosed_ReturnsCanRescheduleTrue()
    {
        var policy = new WarnAndApplyPolicy(_capacityModel);
        var slot = MakeOpenSlot();
        slot.Lock();
        slot.CloseSlot();
        var job = MakeJob(slot.Id, 2m);

        var decision = policy.Evaluate(job, slot);

        decision.CanReschedule.Should().BeTrue();
        decision.TargetSlot.Should().BeNull();
        decision.Reason.Should().Contain("Closed");
    }

    [Fact]
    public void Evaluate_WhenSlotOpenButInsufficientCapacity_ReturnsCanRescheduleTrue()
    {
        var policy = new WarnAndApplyPolicy(_capacityModel);
        var slot = MakeOpenSlot(capacityHours: 3m);
        var job = MakeJob(slot.Id, 5m); // 5h doesn't fit in 3h slot

        var decision = policy.Evaluate(job, slot);

        decision.CanReschedule.Should().BeTrue();
        decision.TargetSlot.Should().BeNull();
        decision.Reason.Should().Contain("insufficient capacity");
    }

    [Fact]
    public void Apply_WhenAvailableSlotExists_ReschedulesJobToThatSlot()
    {
        var policy = new WarnAndApplyPolicy(_capacityModel);
        var lockedSlot = MakeOpenSlot();
        lockedSlot.Lock();

        var openSlot = MakeOpenSlot(8m);
        var job = MakeJob(lockedSlot.Id, 2m);

        policy.Apply(job, new[] { lockedSlot, openSlot });

        job.DaySlotId.Should().Be(openSlot.Id, "job should be rescheduled to first available open slot");
        job.Status.Should().NotBe("Warning");
    }

    [Fact]
    public void Apply_WhenNoSlotAvailable_MarksJobAsWarning()
    {
        var policy = new WarnAndApplyPolicy(_capacityModel);
        var slot = MakeOpenSlot(capacityHours: 1m);
        slot.Lock();
        var job = MakeJob(slot.Id, 5m);

        policy.Apply(job, new[] { slot });

        job.Status.Should().Be("Warning");
    }

    [Fact]
    public void Apply_SkipsLockedSlotsAndPicksFirstOpen()
    {
        var policy = new WarnAndApplyPolicy(_capacityModel);
        var locked1 = MakeOpenSlot(); locked1.Lock();
        var locked2 = MakeOpenSlot(); locked2.Lock();
        var open    = MakeOpenSlot(8m);
        var job = MakeJob(locked1.Id, 2m);

        policy.Apply(job, new[] { locked1, locked2, open });

        job.DaySlotId.Should().Be(open.Id);
    }

    [Fact]
    public void Apply_SkipsSlotWithInsufficientCapacity()
    {
        var policy = new WarnAndApplyPolicy(_capacityModel);
        var tinySlot = MakeOpenSlot(capacityHours: 1m); // too small for 5h job
        var bigSlot  = MakeOpenSlot(capacityHours: 8m);
        var job = MakeJob(Guid.NewGuid(), 5m);

        policy.Apply(job, new[] { tinySlot, bigSlot });

        job.DaySlotId.Should().Be(bigSlot.Id, "tiny slot skipped, big slot used");
    }

    [Fact]
    public void RescheduleTo_UpdatesDaySlotId()
    {
        var newSlotId = Guid.NewGuid();
        var job = MakeJob(Guid.NewGuid(), 2m);

        job.RescheduleTo(newSlotId);

        job.DaySlotId.Should().Be(newSlotId);
    }

    [Fact]
    public void MarkAsWarning_SetsStatusToWarning()
    {
        var job = MakeJob(Guid.NewGuid(), 2m);

        job.MarkAsWarning();

        job.Status.Should().Be("Warning");
    }
}
