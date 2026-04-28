using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SpaceOS.Modules.Cutting.Domain.Aggregates;
using SpaceOS.Modules.Cutting.Domain.Entities;
using SpaceOS.Modules.Cutting.Infrastructure.Persistence;
using SpaceOS.Modules.Cutting.Infrastructure.Repositories;
using Xunit;

namespace SpaceOS.Modules.Cutting.Tests.Infrastructure;

public class CuttingRepositoryTests : IDisposable
{
    private readonly CuttingDbContext _db;
    private readonly CuttingRepository _repo;
    private readonly Guid _tenantId = Guid.NewGuid();

    public CuttingRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<CuttingDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new CuttingDbContext(options);
        _repo = new CuttingRepository(_db);
    }

    private CuttingSheet CreateSheet(string orderRef = "ORD-001")
    {
        var lines = new[] { CuttingLine.Create(Guid.NewGuid(), "Panel A", "MDF 18mm", 600, 400, 18, 2) };
        var sheet = CuttingSheet.Create(_tenantId, orderRef, lines);
        sheet.Submit();
        sheet.PopDomainEvents();
        return sheet;
    }

    [Fact]
    public async Task AddCuttingSheet_ShouldPersist()
    {
        var sheet = CreateSheet();
        await _repo.AddCuttingSheetAsync(sheet);
        await _repo.SaveChangesAsync();

        var found = await _repo.GetCuttingSheetByIdAsync(sheet.Id);
        found.Should().NotBeNull();
        found!.OrderReference.Should().Be("ORD-001");
    }

    [Fact]
    public async Task GetCuttingSheetById_WithLines_ShouldIncludeLines()
    {
        var sheet = CreateSheet();
        await _repo.AddCuttingSheetAsync(sheet);
        await _repo.SaveChangesAsync();

        var found = await _repo.GetCuttingSheetByIdAsync(sheet.Id);
        found!.Lines.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetCuttingSheetById_NotExisting_ShouldReturnNull()
    {
        var found = await _repo.GetCuttingSheetByIdAsync(Guid.NewGuid());
        found.Should().BeNull();
    }

    [Fact]
    public async Task AddDailyCuttingPlan_ShouldPersist()
    {
        var sheet = CreateSheet();
        await _repo.AddCuttingSheetAsync(sheet);
        await _repo.SaveChangesAsync();

        var batch = CuttingBatch.Create(Guid.NewGuid(), "MDF 18mm", 18m, new[] { sheet.Id });
        var plan = DailyCuttingPlan.Create(_tenantId, "Test Plan", DateTime.UtcNow.Date, new[] { batch });
        await _repo.AddDailyCuttingPlanAsync(plan);
        await _repo.SaveChangesAsync();

        var found = await _repo.GetDailyCuttingPlanByDateAsync(DateTime.UtcNow.Date);
        found.Should().NotBeNull();
        found!.Batches.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetDailyCuttingPlan_NotExisting_ShouldReturnNull()
    {
        var found = await _repo.GetDailyCuttingPlanByDateAsync(DateTime.UtcNow.Date.AddDays(-100));
        found.Should().BeNull();
    }

    [Fact]
    public void CuttingSheet_IsImmutable_StatusCannotBeDirectlyModified()
    {
        // Verify no public setters on CuttingSheet
        typeof(CuttingSheet).GetProperties()
            .Where(p => p.CanWrite && p.GetSetMethod()?.IsPublic == true)
            .Should().BeEmpty("CuttingSheet must be immutable (no public setters)");
    }

    [Fact]
    public async Task TenantIsolation_TwoTenants_ShouldHaveSeparateSheets()
    {
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();

        var linesA = new[] { CuttingLine.Create(Guid.NewGuid(), "A", "MDF 18mm", 600, 400, 18, 1) };
        var sheetA = CuttingSheet.Create(tenantA, "ORD-A", linesA);
        sheetA.Submit(); sheetA.PopDomainEvents();

        var linesB = new[] { CuttingLine.Create(Guid.NewGuid(), "B", "MDF 18mm", 500, 300, 18, 1) };
        var sheetB = CuttingSheet.Create(tenantB, "ORD-B", linesB);
        sheetB.Submit(); sheetB.PopDomainEvents();

        await _repo.AddCuttingSheetAsync(sheetA);
        await _repo.AddCuttingSheetAsync(sheetB);
        await _repo.SaveChangesAsync();

        var tenantASheets = await _db.CuttingSheets.AsNoTracking().Where(s => s.TenantId == tenantA).ToListAsync();
        var tenantBSheets = await _db.CuttingSheets.AsNoTracking().Where(s => s.TenantId == tenantB).ToListAsync();

        tenantASheets.Should().ContainSingle(s => s.Id == sheetA.Id);
        tenantBSheets.Should().ContainSingle(s => s.Id == sheetB.Id);
        tenantASheets.Should().NotContain(s => s.TenantId == tenantB);
    }

    public void Dispose() => _db.Dispose();
}
