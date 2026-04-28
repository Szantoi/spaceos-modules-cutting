using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SpaceOS.Modules.Cutting.Execution.Domain.Aggregates;
using SpaceOS.Modules.Cutting.Execution.Domain.ValueObjects;
using SpaceOS.Modules.Cutting.Execution.Infrastructure.Persistence.Repositories;
using SpaceOS.Modules.Cutting.Infrastructure.Persistence;
using Xunit;

namespace SpaceOS.Modules.Cutting.Tests.Execution.Integration;

/// <summary>
/// Tests that tenant data is isolated by TenantId at the application layer.
/// Note: actual PostgreSQL RLS is tested in integration/E2E tests. These tests verify
/// that repository queries use TenantId filtering correctly.
/// </summary>
public class RlsIsolationTests : IDisposable
{
    private readonly CuttingDbContext _db;
    private readonly CuttingExecutionRepository _repo;

    public RlsIsolationTests()
    {
        var options = new DbContextOptionsBuilder<CuttingDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new CuttingDbContext(options);
        _repo = new CuttingExecutionRepository(_db);
    }

    [Fact]
    public async Task ListByTenant_TenantA_DoesNotSeeTenanntBData()
    {
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();

        var execA = CreateExecution(tenantA);
        var execB = CreateExecution(tenantB);

        await _repo.AddAsync(execA, CancellationToken.None);
        await _repo.AddAsync(execB, CancellationToken.None);
        await _repo.SaveChangesAsync(CancellationToken.None);

        var resultsA = await _repo.ListByTenantAsync(tenantA, CancellationToken.None);

        resultsA.Should().ContainSingle(e => e.TenantId == tenantA);
        resultsA.Should().NotContain(e => e.TenantId == tenantB);
    }

    [Fact]
    public async Task ListByTenant_TenantB_DoesNotSeeTenanntAData()
    {
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();

        var execA = CreateExecution(tenantA);
        var execB = CreateExecution(tenantB);

        await _repo.AddAsync(execA, CancellationToken.None);
        await _repo.AddAsync(execB, CancellationToken.None);
        await _repo.SaveChangesAsync(CancellationToken.None);

        var resultsB = await _repo.ListByTenantAsync(tenantB, CancellationToken.None);

        resultsB.Should().ContainSingle(e => e.TenantId == tenantB);
        resultsB.Should().NotContain(e => e.TenantId == tenantA);
    }

    [Fact]
    public async Task GetById_CanRetrieveByIdRegardlessOfTenant()
    {
        var tenantA = Guid.NewGuid();
        var exec = CreateExecution(tenantA);

        await _repo.AddAsync(exec, CancellationToken.None);
        await _repo.SaveChangesAsync(CancellationToken.None);

        // GetById is not tenant-scoped (relies on upper layers or RLS in DB)
        var found = await _repo.GetByIdAsync(exec.Id, CancellationToken.None);
        found.Should().NotBeNull();
    }

    [Fact]
    public async Task ListActiveByTenant_OnlyReturnsScheduledStartedInProgress()
    {
        var tenantId = Guid.NewGuid();
        var activeExec = CreateExecution(tenantId);

        await _repo.AddAsync(activeExec, CancellationToken.None);
        await _repo.SaveChangesAsync(CancellationToken.None);

        var actives = await _repo.ListActiveByTenantAsync(tenantId, CancellationToken.None);
        actives.Should().ContainSingle();
        actives[0].Status.Should().Be(SpaceOS.Modules.Cutting.Execution.Domain.Enums.CuttingExecutionStatus.Scheduled);
    }

    [Fact]
    public async Task MultiTenantConcurrent_EachTenantSeesOnlyOwnExecutions()
    {
        var tenants = Enumerable.Range(0, 3).Select(_ => Guid.NewGuid()).ToList();

        foreach (var tid in tenants)
        {
            await _repo.AddAsync(CreateExecution(tid), CancellationToken.None);
        }
        await _repo.SaveChangesAsync(CancellationToken.None);

        foreach (var tid in tenants)
        {
            var results = await _repo.ListByTenantAsync(tid, CancellationToken.None);
            results.Should().ContainSingle(e => e.TenantId == tid);
            results.Should().NotContain(e => e.TenantId != tid);
        }
    }

    private static CuttingExecution CreateExecution(Guid tenantId)
    {
        var worker = WorkerAssignment.Create(Guid.NewGuid(), Guid.NewGuid()).Value;
        var window = ScheduleWindow.Create(DateTime.UtcNow, DateTime.UtcNow.AddHours(1)).Value;
        var result = CuttingExecution.Schedule(Guid.NewGuid(), worker, "machine-01", window, 5, tenantId);
        result.Value.PopDomainEvents();
        return result.Value;
    }

    public void Dispose() => _db.Dispose();
}
