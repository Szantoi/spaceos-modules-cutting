using FluentAssertions;
using SpaceOS.Modules.Cutting.Domain.Aggregates;
using SpaceOS.Modules.Cutting.Domain.Entities;
using SpaceOS.Modules.Cutting.Domain.Services;
using Xunit;

namespace SpaceOS.Modules.Cutting.Tests.Domain;

public class AreaCapacityModelTests
{
    private static readonly DateOnly Today = DateOnly.FromDateTime(DateTime.UtcNow.Date);

    private static CuttingJob MakeJob(decimal estimatedHours, decimal widthMm = 0m, decimal heightMm = 0m)
        => CuttingJob.Create(Guid.NewGuid(), Guid.NewGuid(),
            Today.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc), "Normal", estimatedHours, widthMm, heightMm);

    private static DaySlot MakeSlot(decimal capacityHours = 8m)
        => DaySlot.Create(Guid.NewGuid(), Today, capacityHours);

    [Fact]
    public void ModelId_ShouldBeAreaV1()
    {
        var model = new AreaCapacityModel();
        model.ModelId.Should().Be("area-v1");
    }

    [Fact]
    public void ComputeJobCost_WithZeroDimensions_FallsBackToEstimatedTimeHours()
    {
        var model = new AreaCapacityModel();
        var job = MakeJob(estimatedHours: 3m, widthMm: 0m, heightMm: 0m);

        var cost = model.ComputeJobCost(job);

        cost.Should().Be(3m, "fallback to EstimatedTimeHours when no geometry is provided");
    }

    [Fact]
    public void ComputeJobCost_WithDimensions_UsesAreaFormula()
    {
        // 1000mm × 500mm = 0.5 m², throughput 2.5 m²/h → cost = 0.5 / 2.5 = 0.2h
        var model = new AreaCapacityModel();
        var job = MakeJob(estimatedHours: 1m, widthMm: 1000m, heightMm: 500m);

        var cost = model.ComputeJobCost(job);

        cost.Should().BeApproximately(0.2m, 0.001m);
    }

    [Fact]
    public void ComputeJobCost_WithOnlyWidthZero_FallsBackToEstimatedTimeHours()
    {
        var model = new AreaCapacityModel();
        var job = MakeJob(estimatedHours: 2m, widthMm: 0m, heightMm: 500m);

        var cost = model.ComputeJobCost(job);

        cost.Should().Be(2m, "width=0 triggers fallback");
    }

    [Fact]
    public void HasCapacity_WhenSufficientCapacity_ReturnsTrue()
    {
        var model = new AreaCapacityModel();
        var slot = MakeSlot(capacityHours: 8m);
        var job = MakeJob(estimatedHours: 2m);

        model.HasCapacity(slot, job).Should().BeTrue();
    }

    [Fact]
    public void HasCapacity_WhenInsufficientCapacity_ReturnsFalse()
    {
        var model = new AreaCapacityModel();
        var slot = MakeSlot(capacityHours: 8m);
        var job = MakeJob(estimatedHours: 9m);

        model.HasCapacity(slot, job).Should().BeFalse();
    }

    [Fact]
    public void HasCapacity_WhenExactlyAtCapacity_ReturnsTrue()
    {
        var model = new AreaCapacityModel();
        var slot = MakeSlot(capacityHours: 8m);
        var job = MakeJob(estimatedHours: 8m);

        model.HasCapacity(slot, job).Should().BeTrue("exact fit is allowed");
    }

    [Fact]
    public void ComputeCapacityHours_DelegatesToSlotCapacityHours()
    {
        var model = new AreaCapacityModel();
        var slot = MakeSlot(capacityHours: 12m);

        model.ComputeCapacityHours(slot).Should().Be(12m);
    }

    [Fact]
    public void HasCapacity_AfterPartialAllocation_ReflectsUsedCapacity()
    {
        var model = new AreaCapacityModel();
        var slot = DaySlot.Create(Guid.NewGuid(), Today, 8m);
        var firstJob = CuttingJob.Create(slot.Id, Guid.NewGuid(),
            Today.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc), "Normal", 6m);
        slot.AddJob(firstJob, model);

        var secondJob = MakeJob(estimatedHours: 3m);
        model.HasCapacity(slot, secondJob).Should().BeFalse("only 2h remaining after first job");
    }
}
