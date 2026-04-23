using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SpaceOS.Modules.Cutting.Domain.Aggregates;
using SpaceOS.Modules.Cutting.Domain.Enums;
using SpaceOS.Modules.Cutting.Infrastructure.Persistence;
using SpaceOS.Modules.Cutting.Infrastructure.Repositories;
using Xunit;

namespace SpaceOS.Modules.Cutting.Tests.Infrastructure;

public class CuttingPlanRepositoryTests : IDisposable
{
    private readonly CuttingDbContext _db;
    private readonly CuttingRepository _repo;
    private readonly Guid _tenantId = Guid.NewGuid();
    private static readonly DateTime TodayUtc = DateTime.SpecifyKind(DateTime.UtcNow.Date, DateTimeKind.Utc);

    public CuttingPlanRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<CuttingDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new CuttingDbContext(options);
        _repo = new CuttingRepository(_db);
    }

    private CuttingPlan CreatePlan(DateTime? date = null, int planDays = 14)
        => CuttingPlan.Create(_tenantId, date ?? TodayUtc, planDays, "maxcut-v1");

    [Fact]
    public async Task AddCuttingPlan_ShouldPersist()
    {
        var plan = CreatePlan();
        await _repo.AddCuttingPlanAsync(plan);
        await _repo.SaveChangesAsync();

        var found = await _repo.GetCuttingPlanByIdAsync(plan.Id);
        found.Should().NotBeNull();
        found!.StrategyId.Should().Be("maxcut-v1");
    }

    [Fact]
    public async Task AddCuttingPlan_ShouldPersistDaySlots()
    {
        var plan = CreatePlan(planDays: 14);
        await _repo.AddCuttingPlanAsync(plan);
        await _repo.SaveChangesAsync();

        var found = await _repo.GetCuttingPlanByIdAsync(plan.Id);
        found!.DaySlots.Should().HaveCount(14);
    }

    [Fact]
    public async Task GetCuttingPlanById_NotExisting_ShouldReturnNull()
    {
        var found = await _repo.GetCuttingPlanByIdAsync(Guid.NewGuid());
        found.Should().BeNull();
    }

    [Fact]
    public async Task GetAllCuttingPlans_ShouldReturnAllForTenant()
    {
        var plan1 = CreatePlan(TodayUtc);
        var plan2 = CreatePlan(TodayUtc.AddDays(14), 7);

        await _repo.AddCuttingPlanAsync(plan1);
        await _repo.AddCuttingPlanAsync(plan2);
        await _repo.SaveChangesAsync();

        var all = await _repo.GetAllCuttingPlansAsync();
        all.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetAllCuttingPlans_ShouldBeOrderedByPlanDateDescending()
    {
        var planEarlier = CreatePlan(TodayUtc, 7);
        var planLater = CreatePlan(TodayUtc.AddDays(14), 7);

        await _repo.AddCuttingPlanAsync(planEarlier);
        await _repo.AddCuttingPlanAsync(planLater);
        await _repo.SaveChangesAsync();

        var all = await _repo.GetAllCuttingPlansAsync();
        all[0].PlanDate.Should().BeOnOrAfter(all[1].PlanDate);
    }

    [Fact]
    public async Task TenantIsolation_TwoTenants_ShouldBeFilteredByTenantId()
    {
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();

        var planA = CuttingPlan.Create(tenantA, TodayUtc, 7, "fifo");
        var planB = CuttingPlan.Create(tenantB, TodayUtc, 7, "fifo");

        await _repo.AddCuttingPlanAsync(planA);
        await _repo.AddCuttingPlanAsync(planB);
        await _repo.SaveChangesAsync();

        var tenantAPlans = await _db.CuttingPlans.AsNoTracking().Where(p => p.TenantId == tenantA).ToListAsync();
        var tenantBPlans = await _db.CuttingPlans.AsNoTracking().Where(p => p.TenantId == tenantB).ToListAsync();

        tenantAPlans.Should().ContainSingle(p => p.Id == planA.Id);
        tenantBPlans.Should().ContainSingle(p => p.Id == planB.Id);
        tenantAPlans.Should().NotContain(p => p.TenantId == tenantB);
    }

    [Fact]
    public async Task GetCuttingPlanTracked_ShouldAllowStatusUpdate()
    {
        var plan = CreatePlan();
        await _repo.AddCuttingPlanAsync(plan);
        await _repo.SaveChangesAsync();

        var tracked = await _repo.GetCuttingPlanTrackedAsync(plan.Id);
        tracked.Should().NotBeNull();
#pragma warning disable CS0618
        tracked!.UpdateStatus(CuttingPlanStatus.Published);
#pragma warning restore CS0618
        await _repo.SaveChangesAsync();

        var reloaded = await _repo.GetCuttingPlanByIdAsync(plan.Id);
        reloaded!.Status.Should().Be(CuttingPlanStatus.Published);
    }

    public void Dispose() => _db.Dispose();
}
