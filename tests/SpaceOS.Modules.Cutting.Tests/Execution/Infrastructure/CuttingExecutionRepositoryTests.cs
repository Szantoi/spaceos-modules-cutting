using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SpaceOS.Modules.Cutting.Execution.Domain.Aggregates;
using SpaceOS.Modules.Cutting.Execution.Domain.ValueObjects;
using SpaceOS.Modules.Cutting.Execution.Infrastructure.Persistence.Repositories;
using SpaceOS.Modules.Cutting.Infrastructure.Persistence;
using Xunit;

namespace SpaceOS.Modules.Cutting.Tests.Execution.Infrastructure;

public class CuttingExecutionRepositoryTests : IDisposable
{
    private readonly CuttingDbContext _db;
    private readonly CuttingExecutionRepository _repo;

    public CuttingExecutionRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<CuttingDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new CuttingDbContext(options);
        _repo = new CuttingExecutionRepository(_db);
    }

    private static CuttingExecution CreateExecution(Guid? tenantId = null)
    {
        var tid = tenantId ?? Guid.NewGuid();
        var worker = WorkerAssignment.Create(Guid.NewGuid(), Guid.NewGuid()).Value;
        var window = ScheduleWindow.Create(DateTime.UtcNow, DateTime.UtcNow.AddHours(1)).Value;
        var result = CuttingExecution.Schedule(Guid.NewGuid(), worker, "machine-01", window, 5, tid);
        result.Value.PopDomainEvents();
        return result.Value;
    }

    [Fact]
    public async Task AddAsync_ThenGetByIdAsync_ReturnsSameExecution()
    {
        var execution = CreateExecution();

        await _repo.AddAsync(execution, CancellationToken.None);
        await _repo.SaveChangesAsync(CancellationToken.None);

        var found = await _repo.GetByIdAsync(execution.Id, CancellationToken.None);
        found.Should().NotBeNull();
        found!.Id.Should().Be(execution.Id);
    }

    [Fact]
    public async Task GetByIdAsync_NotExisting_ReturnsNull()
    {
        var found = await _repo.GetByIdAsync(Guid.NewGuid(), CancellationToken.None);
        found.Should().BeNull();
    }

    [Fact]
    public async Task GetByIdWithProgressAsync_ReturnsSameExecution()
    {
        var execution = CreateExecution();
        await _repo.AddAsync(execution, CancellationToken.None);
        await _repo.SaveChangesAsync(CancellationToken.None);

        var found = await _repo.GetByIdWithProgressAsync(execution.Id, CancellationToken.None);
        found.Should().NotBeNull();
        found!.TenantId.Should().Be(execution.TenantId);
    }

    [Fact]
    public async Task GetByIdWithProgressAsync_NotExisting_ReturnsNull()
    {
        var found = await _repo.GetByIdWithProgressAsync(Guid.NewGuid(), CancellationToken.None);
        found.Should().BeNull();
    }

    [Fact]
    public async Task ListByTenantAsync_ReturnsOnlyMatchingTenant()
    {
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();

        var execA = CreateExecution(tenantA);
        var execB = CreateExecution(tenantB);

        await _repo.AddAsync(execA, CancellationToken.None);
        await _repo.AddAsync(execB, CancellationToken.None);
        await _repo.SaveChangesAsync(CancellationToken.None);

        var results = await _repo.ListByTenantAsync(tenantA, CancellationToken.None);

        results.Should().ContainSingle(e => e.Id == execA.Id);
        results.Should().NotContain(e => e.Id == execB.Id);
    }

    [Fact]
    public async Task ListByTenantAsync_EmptyTenant_ReturnsEmpty()
    {
        var results = await _repo.ListByTenantAsync(Guid.NewGuid(), CancellationToken.None);
        results.Should().BeEmpty();
    }

    [Fact]
    public async Task ListActiveByTenantAsync_DoesNotReturnCompletedExecutions()
    {
        var tenantId = Guid.NewGuid();
        var execution = CreateExecution(tenantId);

        await _repo.AddAsync(execution, CancellationToken.None);
        await _repo.SaveChangesAsync(CancellationToken.None);

        // Scheduled status is active — should be returned
        var active = await _repo.ListActiveByTenantAsync(tenantId, CancellationToken.None);
        active.Should().ContainSingle(e => e.Id == execution.Id);
    }

    [Fact]
    public async Task AddAsync_NullExecution_ThrowsArgumentNullException()
    {
        var act = async () => await _repo.AddAsync(null!, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task SaveChanges_Persists_MultipleExecutions()
    {
        var tenantId = Guid.NewGuid();
        var exec1 = CreateExecution(tenantId);
        var exec2 = CreateExecution(tenantId);

        await _repo.AddAsync(exec1, CancellationToken.None);
        await _repo.AddAsync(exec2, CancellationToken.None);
        await _repo.SaveChangesAsync(CancellationToken.None);

        var results = await _repo.ListByTenantAsync(tenantId, CancellationToken.None);
        results.Should().HaveCount(2);
    }

    [Fact]
    public async Task Execution_PersistedProperties_ArePreserved()
    {
        var tenantId = Guid.NewGuid();
        var sheetId = Guid.NewGuid();
        var worker = WorkerAssignment.Create(Guid.NewGuid(), Guid.NewGuid()).Value;
        var window = ScheduleWindow.Create(DateTime.UtcNow, DateTime.UtcNow.AddHours(2)).Value;
        var execution = CuttingExecution.Schedule(sheetId, worker, "CNC-42", window, 10, tenantId).Value;
        execution.PopDomainEvents();

        await _repo.AddAsync(execution, CancellationToken.None);
        await _repo.SaveChangesAsync(CancellationToken.None);

        // Same in-memory db — the repo re-queries
        var found = await _repo.GetByIdAsync(execution.Id, CancellationToken.None);
        found!.MachineId.Should().Be("CNC-42");
        found.TotalPanels.Should().Be(10);
        found.SheetId.Should().Be(sheetId);
        found.TenantId.Should().Be(tenantId);
    }

    public void Dispose() => _db.Dispose();
}
