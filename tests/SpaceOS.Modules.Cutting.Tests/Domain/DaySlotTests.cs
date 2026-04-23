using FluentAssertions;
using SpaceOS.Modules.Cutting.Domain.Aggregates;
using SpaceOS.Modules.Cutting.Domain.Entities;
using SpaceOS.Modules.Cutting.Domain.Enums;
using SpaceOS.Modules.Cutting.Domain.Services;
using Xunit;

namespace SpaceOS.Modules.Cutting.Tests.Domain;

public class DaySlotTests
{
    private static readonly DateOnly Today = DateOnly.FromDateTime(DateTime.UtcNow.Date);

    [Fact]
    public void Create_ShouldSetDefaultCapacityOf8()
    {
        var slot = DaySlot.Create(Guid.NewGuid(), Today);
        slot.CapacityHours.Should().Be(8m);
    }

    [Fact]
    public void Create_ShouldHaveOpenStatus()
    {
        var slot = DaySlot.Create(Guid.NewGuid(), Today);
        slot.Status.Should().Be(DaySlotStatus.Open);
    }

    [Fact]
    public void Create_ShouldSetSlotDate()
    {
        var slot = DaySlot.Create(Guid.NewGuid(), Today);
        slot.SlotDate.Should().Be(Today);
    }

    [Fact]
    public void UsedCapacityHours_WithNoJobs_ShouldBeZero()
    {
        var slot = DaySlot.Create(Guid.NewGuid(), Today);
        slot.UsedCapacityHours.Should().Be(0m);
    }

    [Fact]
    public void UtilizationPercent_WithNoJobs_ShouldBeZero()
    {
        var slot = DaySlot.Create(Guid.NewGuid(), Today);
        slot.UtilizationPercent.Should().Be(0m);
    }

    [Fact]
    public void AddJob_WithMatchingDaySlotId_ShouldAddJobAndUpdateUsedCapacity()
    {
        var slot = DaySlot.Create(Guid.NewGuid(), Today);
        var job = CuttingJob.Create(slot.Id, Guid.NewGuid(),
            Today.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc), "Normal", 2m);

        var result = slot.AddJob(job, new AreaCapacityModel());

        result.IsSuccess.Should().BeTrue();
        slot.Jobs.Should().HaveCount(1);
        slot.UsedCapacityHours.Should().Be(2m);
    }

    [Fact]
    public void AddJob_WithWrongDaySlotId_ShouldReturnInvalid()
    {
        var slot = DaySlot.Create(Guid.NewGuid(), Today);
        var job = CuttingJob.Create(Guid.NewGuid(), Guid.NewGuid(),
            Today.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc), "Normal", 2m);

        var result = slot.AddJob(job, new AreaCapacityModel());

        result.IsSuccess.Should().BeFalse();
        slot.Jobs.Should().BeEmpty();
    }

    [Fact]
    public void AddJob_ExceedingCapacity_ShouldReturnInvalid()
    {
        var slot = DaySlot.Create(Guid.NewGuid(), Today);
        var job = CuttingJob.Create(slot.Id, Guid.NewGuid(),
            Today.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc), "Normal", 9m);

        var result = slot.AddJob(job, new AreaCapacityModel());

        result.IsSuccess.Should().BeFalse();
        slot.UsedCapacityHours.Should().Be(0m);
    }

    [Fact]
    public void UsedCapacityHours_AfterAddingJobs_ShouldSumEstimatedHours()
    {
        var slot = DaySlot.Create(Guid.NewGuid(), Today);
        var job1 = CuttingJob.Create(slot.Id, Guid.NewGuid(),
            Today.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc), "Normal", 2m);
        var job2 = CuttingJob.Create(slot.Id, Guid.NewGuid(),
            Today.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc), "High", 3m);

        slot.AddJob(job1, new AreaCapacityModel());
        slot.AddJob(job2, new AreaCapacityModel());

        slot.UsedCapacityHours.Should().Be(5m);
    }

    [Fact]
    public void UtilizationPercent_With4HoursOf8_ShouldBe50Percent()
    {
        var slot = DaySlot.Create(Guid.NewGuid(), Today);
        var job = CuttingJob.Create(slot.Id, Guid.NewGuid(),
            Today.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc), "Normal", 4m);
        slot.AddJob(job, new AreaCapacityModel());

        slot.UtilizationPercent.Should().Be(50m);
    }

    [Fact]
    public void Lock_FromOpen_ShouldSucceed()
    {
        var slot = DaySlot.Create(Guid.NewGuid(), Today);
        var result = slot.Lock();

        result.IsSuccess.Should().BeTrue();
        slot.Status.Should().Be(DaySlotStatus.Locked);
    }

    [Fact]
    public void Lock_WhenAlreadyLocked_ShouldReturnSuccessIdempotent()
    {
        var slot = DaySlot.Create(Guid.NewGuid(), Today);
        slot.Lock();

        var result = slot.Lock();

        result.IsSuccess.Should().BeTrue("Lock is idempotent — locking an already-Locked slot returns Success");
        slot.Status.Should().Be(DaySlotStatus.Locked);
    }

    [Fact]
    public void Lock_WhenClosed_ShouldReturnInvalid()
    {
        var slot = DaySlot.Create(Guid.NewGuid(), Today);
        slot.Lock();
        slot.CloseSlot();

        var result = slot.Lock();

        result.IsSuccess.Should().BeFalse("a Closed slot cannot be locked");
        slot.Status.Should().Be(DaySlotStatus.Closed);
    }

    [Fact]
    public void CloseSlot_FromLocked_ShouldSucceed()
    {
        var slot = DaySlot.Create(Guid.NewGuid(), Today);
        slot.Lock();
        var result = slot.CloseSlot();

        result.IsSuccess.Should().BeTrue();
        slot.Status.Should().Be(DaySlotStatus.Closed);
    }

    [Fact]
    public void CloseSlot_FromOpen_ShouldReturnInvalid()
    {
        var slot = DaySlot.Create(Guid.NewGuid(), Today);
        var result = slot.CloseSlot();

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public void Create_WithCustomCapacity_SetsCapacityHours()
    {
        var slot = DaySlot.Create(Guid.NewGuid(), Today, capacityHours: 12m);
        slot.CapacityHours.Should().Be(12m);
    }

    [Fact]
    public void Create_WithZeroCapacity_ShouldThrow()
    {
        var act = () => DaySlot.Create(Guid.NewGuid(), Today, capacityHours: 0m);
        act.Should().Throw<ArgumentException>().WithParameterName("capacityHours");
    }

    [Fact]
    public void Create_WithNegativeCapacity_ShouldThrow()
    {
        var act = () => DaySlot.Create(Guid.NewGuid(), Today, capacityHours: -1m);
        act.Should().Throw<ArgumentException>().WithParameterName("capacityHours");
    }

    [Fact]
    public void Create_WithEmptyCuttingPlanId_ShouldThrow()
    {
        var act = () => DaySlot.Create(Guid.Empty, Today);
        act.Should().Throw<ArgumentException>().WithParameterName("cuttingPlanId");
    }

    [Fact]
    public void Lock_ThenCloseSlot_ShouldReachClosedStatus()
    {
        var slot = DaySlot.Create(Guid.NewGuid(), Today);
        slot.Lock();
        slot.CloseSlot();

        slot.Status.Should().Be(DaySlotStatus.Closed);
    }

    [Fact]
    public void AddJob_WhenLocked_ShouldReturnInvalid()
    {
        var slot = DaySlot.Create(Guid.NewGuid(), Today);
        slot.Lock();
        var job = CuttingJob.Create(slot.Id, Guid.NewGuid(),
            Today.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc), "Normal", 2m);

        var result = slot.AddJob(job, new AreaCapacityModel());

        result.IsSuccess.Should().BeFalse("locked slots do not accept new jobs");
        slot.Jobs.Should().BeEmpty();
    }

    [Fact]
    public void AddJob_WhenClosed_ShouldReturnInvalid()
    {
        var slot = DaySlot.Create(Guid.NewGuid(), Today);
        slot.Lock();
        slot.CloseSlot();
        var job = CuttingJob.Create(slot.Id, Guid.NewGuid(),
            Today.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc), "Normal", 2m);

        var result = slot.AddJob(job, new AreaCapacityModel());

        result.IsSuccess.Should().BeFalse("closed slots do not accept new jobs");
        slot.Jobs.Should().BeEmpty();
    }
}
