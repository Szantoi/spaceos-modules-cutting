using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using SpaceOS.Modules.Cutting.Domain.Common;
using SpaceOS.Modules.Cutting.Infrastructure.Persistence;

namespace SpaceOS.Modules.Cutting.Infrastructure.Outbox;

/// <summary>
/// EF Core interceptor that converts domain events on aggregate roots into
/// <see cref="LocalOutboxMessage"/> rows, written atomically within the same SaveChanges transaction.
/// </summary>
public sealed class OutboxSaveChangesInterceptor : SaveChangesInterceptor
{
    private static readonly JsonSerializerOptions _serializerOptions = new()
    {
        WriteIndented = false
    };

    /// <inheritdoc />
    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken ct = default)
    {
        if (eventData.Context is CuttingDbContext context)
        {
            WriteOutboxMessages(context);
        }

        return await base.SavingChangesAsync(eventData, result, ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        if (eventData.Context is CuttingDbContext context)
        {
            WriteOutboxMessages(context);
        }

        return base.SavingChanges(eventData, result);
    }

    private static void WriteOutboxMessages(CuttingDbContext context)
    {
        var aggregates = context.ChangeTracker
            .Entries<AggregateRoot>()
            .Where(e => e.Entity.DomainEvents.Count > 0)
            .ToList();

        foreach (var entry in aggregates)
        {
            var aggregate = entry.Entity;
            var events = aggregate.PopDomainEvents();
            var batchId = Guid.NewGuid();
            var aggregateType = aggregate.GetType().Name;

            // Try to resolve TenantId and AggregateId from the aggregate
            var tenantIdProp = aggregate.GetType().GetProperty("TenantId");
            var idProp = aggregate.GetType().GetProperty("Id");

            var tenantId = tenantIdProp?.GetValue(aggregate) is Guid t ? t : Guid.Empty;
            var aggregateId = idProp?.GetValue(aggregate) is Guid id ? id : (Guid?)null;

            for (var seq = 0; seq < events.Count; seq++)
            {
                var domainEvent = events[seq];
                var eventType = domainEvent.GetType().Name;
                var payloadJson = JsonSerializer.Serialize(domainEvent, domainEvent.GetType(), _serializerOptions);

                var message = LocalOutboxMessage.Create(
                    tenantId,
                    eventType,
                    payloadJson,
                    batchId,
                    seq,
                    aggregateId,
                    aggregateType);

                context.LocalOutboxMessages.Add(message);
            }
        }
    }
}
