using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SpaceOS.Modules.Cutting.Execution.Domain.Aggregates;
using SpaceOS.Modules.Cutting.Execution.Domain.ValueObjects;
using SpaceOS.Modules.Cutting.Infrastructure.Outbox;
using SpaceOS.Modules.Cutting.Infrastructure.Persistence;
using Xunit;

namespace SpaceOS.Modules.Cutting.Tests.Execution.Integration;

/// <summary>
/// Tests that domain events are written to the outbox atomically within SaveChanges.
/// </summary>
public class OutboxTransactionTests : IDisposable
{
    private readonly CuttingDbContext _db;

    public OutboxTransactionTests()
    {
        var interceptor = new OutboxSaveChangesInterceptor();
        var options = new DbContextOptionsBuilder<CuttingDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .AddInterceptors(interceptor)
            .Options;
        _db = new CuttingDbContext(options);
    }

    [Fact]
    public async Task SaveChanges_WithExecution_WritesOutboxAtomically()
    {
        var tenantId = Guid.NewGuid();
        var execution = CreateExecution(tenantId);

        await _db.CuttingExecutions.AddAsync(execution);
        await _db.SaveChangesAsync();

        var execCount = await _db.CuttingExecutions.CountAsync();
        var outboxCount = await _db.LocalOutboxMessages.CountAsync();

        execCount.Should().Be(1);
        outboxCount.Should().BeGreaterThan(0, "domain events must be captured atomically");
    }

    [Fact]
    public async Task SaveChanges_EventType_MatchesDomainEventName()
    {
        var tenantId = Guid.NewGuid();
        var execution = CreateExecution(tenantId);

        await _db.CuttingExecutions.AddAsync(execution);
        await _db.SaveChangesAsync();

        var messages = await _db.LocalOutboxMessages.ToListAsync();
        messages.Should().Contain(m => m.EventType == "CuttingExecutionScheduled");
    }

    [Fact]
    public async Task SaveChanges_Status_IsPending()
    {
        var tenantId = Guid.NewGuid();
        var execution = CreateExecution(tenantId);

        await _db.CuttingExecutions.AddAsync(execution);
        await _db.SaveChangesAsync();

        var messages = await _db.LocalOutboxMessages.ToListAsync();
        messages.Should().AllSatisfy(m => m.Status.Should().Be(LocalOutboxStatus.Pending));
    }

    [Fact]
    public async Task SaveChanges_MultipleAggregates_AllGetOutboxMessages()
    {
        var tenantId = Guid.NewGuid();
        var exec1 = CreateExecution(tenantId);
        var exec2 = CreateExecution(tenantId);

        await _db.CuttingExecutions.AddAsync(exec1);
        await _db.CuttingExecutions.AddAsync(exec2);
        await _db.SaveChangesAsync();

        var outboxMessages = await _db.LocalOutboxMessages.ToListAsync();
        // Each execution raises 1 domain event on schedule
        outboxMessages.Should().HaveCount(2);
    }

    [Fact]
    public async Task SaveChanges_SecondSave_NoDomainEvents_NoNewOutboxMessages()
    {
        var tenantId = Guid.NewGuid();
        var execution = CreateExecution(tenantId);

        await _db.CuttingExecutions.AddAsync(execution);
        await _db.SaveChangesAsync();

        var countAfterFirst = await _db.LocalOutboxMessages.CountAsync();

        // Second save with no new events
        await _db.SaveChangesAsync();

        var countAfterSecond = await _db.LocalOutboxMessages.CountAsync();
        countAfterSecond.Should().Be(countAfterFirst);
    }

    [Fact]
    public async Task OutboxMessage_BatchId_IsNotNull()
    {
        var tenantId = Guid.NewGuid();
        var execution = CreateExecution(tenantId);

        await _db.CuttingExecutions.AddAsync(execution);
        await _db.SaveChangesAsync();

        var msg = await _db.LocalOutboxMessages.FirstAsync();
        msg.BatchId.Should().NotBeNull();
    }

    private static CuttingExecution CreateExecution(Guid tenantId)
    {
        var worker = WorkerAssignment.Create(Guid.NewGuid(), Guid.NewGuid()).Value;
        var window = ScheduleWindow.Create(DateTime.UtcNow, DateTime.UtcNow.AddHours(1)).Value;
        return CuttingExecution.Schedule(Guid.NewGuid(), worker, "machine-01", window, 5, tenantId).Value;
    }

    public void Dispose() => _db.Dispose();
}
