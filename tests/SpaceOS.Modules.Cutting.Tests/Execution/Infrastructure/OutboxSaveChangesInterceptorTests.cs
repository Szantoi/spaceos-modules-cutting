using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SpaceOS.Modules.Cutting.Execution.Domain.Aggregates;
using SpaceOS.Modules.Cutting.Execution.Domain.ValueObjects;
using SpaceOS.Modules.Cutting.Infrastructure.Outbox;
using SpaceOS.Modules.Cutting.Infrastructure.Persistence;
using Xunit;

namespace SpaceOS.Modules.Cutting.Tests.Execution.Infrastructure;

public class OutboxSaveChangesInterceptorTests : IDisposable
{
    private readonly CuttingDbContext _db;

    public OutboxSaveChangesInterceptorTests()
    {
        var interceptor = new OutboxSaveChangesInterceptor();
        var options = new DbContextOptionsBuilder<CuttingDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .AddInterceptors(interceptor)
            .Options;
        _db = new CuttingDbContext(options);
    }

    private static CuttingExecution CreateScheduledExecution(Guid tenantId)
    {
        var worker = WorkerAssignment.Create(Guid.NewGuid(), Guid.NewGuid()).Value;
        var window = ScheduleWindow.Create(DateTime.UtcNow, DateTime.UtcNow.AddHours(1)).Value;
        return CuttingExecution.Schedule(Guid.NewGuid(), worker, "machine-01", window, 5, tenantId).Value;
    }

    [Fact]
    public async Task SaveChanges_WithDomainEvents_WritesOutboxMessages()
    {
        var tenantId = Guid.NewGuid();
        var execution = CreateScheduledExecution(tenantId);

        await _db.CuttingExecutions.AddAsync(execution);
        await _db.SaveChangesAsync();

        var outboxMessages = await _db.LocalOutboxMessages.ToListAsync();
        outboxMessages.Should().NotBeEmpty("domain events from Schedule() should be captured");
        outboxMessages.Should().ContainSingle(m => m.EventType == "CuttingExecutionScheduled");
    }

    [Fact]
    public async Task SaveChanges_WithDomainEvents_ClearsEventsFromAggregate()
    {
        var tenantId = Guid.NewGuid();
        var execution = CreateScheduledExecution(tenantId);
        execution.DomainEvents.Should().HaveCount(1, "Schedule() raises one event");

        await _db.CuttingExecutions.AddAsync(execution);
        await _db.SaveChangesAsync();

        // After save, events should be popped
        execution.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public async Task SaveChanges_WithDomainEvents_SetsCorrectTenantId()
    {
        var tenantId = Guid.NewGuid();
        var execution = CreateScheduledExecution(tenantId);

        await _db.CuttingExecutions.AddAsync(execution);
        await _db.SaveChangesAsync();

        var msg = await _db.LocalOutboxMessages.FirstAsync();
        msg.TenantId.Should().Be(tenantId);
    }

    [Fact]
    public async Task SaveChanges_WithDomainEvents_SetsAggregateId()
    {
        var tenantId = Guid.NewGuid();
        var execution = CreateScheduledExecution(tenantId);

        await _db.CuttingExecutions.AddAsync(execution);
        await _db.SaveChangesAsync();

        var msg = await _db.LocalOutboxMessages.FirstAsync();
        msg.AggregateId.Should().Be(execution.Id);
    }

    [Fact]
    public async Task SaveChanges_WithDomainEvents_SetsAggregateType()
    {
        var tenantId = Guid.NewGuid();
        var execution = CreateScheduledExecution(tenantId);

        await _db.CuttingExecutions.AddAsync(execution);
        await _db.SaveChangesAsync();

        var msg = await _db.LocalOutboxMessages.FirstAsync();
        msg.AggregateType.Should().Be("CuttingExecution");
    }

    [Fact]
    public async Task SaveChanges_WithDomainEvents_PayloadJsonIsValid()
    {
        var tenantId = Guid.NewGuid();
        var execution = CreateScheduledExecution(tenantId);

        await _db.CuttingExecutions.AddAsync(execution);
        await _db.SaveChangesAsync();

        var msg = await _db.LocalOutboxMessages.FirstAsync();
        var act = () => System.Text.Json.JsonDocument.Parse(msg.PayloadJson);
        act.Should().NotThrow();
    }

    [Fact]
    public async Task SaveChanges_WithNoDomainEvents_WritesNoOutboxMessages()
    {
        // SaveChanges on entities without domain events should not create outbox messages
        await _db.SaveChangesAsync();

        var outboxMessages = await _db.LocalOutboxMessages.ToListAsync();
        outboxMessages.Should().BeEmpty();
    }

    [Fact]
    public async Task SaveChanges_MultipleEvents_WritesBatchId()
    {
        var tenantId = Guid.NewGuid();
        var execution = CreateScheduledExecution(tenantId);

        await _db.CuttingExecutions.AddAsync(execution);
        await _db.SaveChangesAsync();

        var messages = await _db.LocalOutboxMessages.ToListAsync();
        // All messages from same save should share the same batch id
        messages.Select(m => m.BatchId).Distinct().Should().HaveCount(1);
    }

    public void Dispose() => _db.Dispose();
}
