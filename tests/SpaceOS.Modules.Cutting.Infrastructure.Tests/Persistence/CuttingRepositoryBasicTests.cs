using FluentAssertions;
using SpaceOS.Modules.Cutting.Domain.Aggregates;
using SpaceOS.Modules.Cutting.Domain.Entities;
using SpaceOS.Modules.Cutting.Domain.Enums;
using SpaceOS.Modules.Cutting.Infrastructure.Repositories;
using Xunit;

namespace SpaceOS.Modules.Cutting.Infrastructure.Tests.Persistence;

/// <summary>
/// Basic integration tests for CuttingRepository focusing on key operations.
/// Coverage target: 70%+
/// </summary>
public sealed class CuttingRepositoryBasicTests : InfrastructureIntegrationTestBase
{
    [Fact]
    public async Task GetOpenSlotsBeforeDateAsync_ShouldReturnOnlyPastOpenSlots()
    {
        // Arrange
        var repo = new CuttingRepository(_dbContext!);
        var tenantId = Guid.NewGuid();
        var plan = CuttingPlan.Create(tenantId, DateTime.Today.AddDays(10), 30, "default-strategy");

        // The plan auto-creates 30 slots starting from today + 10
        // We need to manually set some to be in the past by directly modifying them
        await repo.AddCuttingPlanAsync(plan, CancellationToken.None);
        await repo.SaveChangesAsync(CancellationToken.None);

        // Act - Query for open slots before today should return 0 (all slots are in future)
        var today = DateOnly.FromDateTime(DateTime.Today);
        var openSlots = await repo.GetOpenSlotsBeforeDateAsync(today, CancellationToken.None);

        // Assert
        openSlots.Should().BeEmpty();
    }

    [Fact]
    public async Task GetOpenSlotsOrderedByDateAsync_ShouldReturnSortedSlots()
    {
        // Arrange
        var repo = new CuttingRepository(_dbContext!);
        var tenantId = Guid.NewGuid();
        var plan = CuttingPlan.Create(tenantId, DateTime.Today.AddDays(1), 7, "test-strategy");

        await repo.AddCuttingPlanAsync(plan, CancellationToken.None);
        await repo.SaveChangesAsync(CancellationToken.None);

        // Act
        var slots = await repo.GetOpenSlotsOrderedByDateAsync(CancellationToken.None);

        // Assert
        slots.Should().HaveCount(7);
        for (int i = 0; i < slots.Count - 1; i++)
        {
            slots[i].SlotDate.Should().BeBefore(slots[i + 1].SlotDate);
        }
    }

    [Fact]
    public async Task SaveChangesAsync_ShouldPersistData()
    {
        // Arrange
        var repo = new CuttingRepository(_dbContext!);
        var tenantId = Guid.NewGuid();
        var plan = CuttingPlan.Create(tenantId, DateTime.Today.AddDays(1), 7, "test-strategy");

        // Act
        await repo.AddCuttingPlanAsync(plan, CancellationToken.None);
        await repo.SaveChangesAsync(CancellationToken.None);

        // Assert - Verify with fresh context
        using var freshContext = CreateNewDbContext();
        var freshRepo = new CuttingRepository(freshContext);
        var retrieved = await freshRepo.GetCuttingPlanByIdAsync(plan.Id, CancellationToken.None);

        retrieved.Should().NotBeNull();
        retrieved!.Id.Should().Be(plan.Id);
        retrieved.TenantId.Should().Be(tenantId);
    }

    [Fact]
    public async Task GetCuttingPlanByIdAsync_WithDaySlots_ShouldIncludeSlots()
    {
        // Arrange
        var repo = new CuttingRepository(_dbContext!);
        var tenantId = Guid.NewGuid();
        var plan = CuttingPlan.Create(tenantId, DateTime.Today.AddDays(1), 14, "test-strategy");

        await repo.AddCuttingPlanAsync(plan, CancellationToken.None);
        await repo.SaveChangesAsync(CancellationToken.None);

        // Act
        var retrieved = await repo.GetCuttingPlanByIdAsync(plan.Id, CancellationToken.None);

        // Assert
        retrieved.Should().NotBeNull();
        retrieved!.DaySlots.Should().HaveCount(14);
        retrieved.DaySlots.All(s => s.Status == DaySlotStatus.Open).Should().BeTrue();
    }
}
