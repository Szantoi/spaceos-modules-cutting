using SpaceOS.Modules.Cutting.Domain.Common;
using SpaceOS.Modules.Cutting.Domain.Entities;
using SpaceOS.Modules.Cutting.Domain.Enums;
using SpaceOS.Modules.Cutting.Domain.Events;

namespace SpaceOS.Modules.Cutting.Domain.Aggregates;

public class CuttingSheet : AggregateRoot
{
    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public string OrderReference { get; private set; } = string.Empty;
    public CuttingSheetStatus Status { get; private set; }
    public DateTime CreatedAt { get; private set; }
    private readonly List<CuttingLine> _lines = new();
    public IReadOnlyList<CuttingLine> Lines => _lines.AsReadOnly();

    private CuttingSheet() { }

    public static CuttingSheet Create(Guid tenantId, string orderReference, IEnumerable<CuttingLine> lines)
    {
        if (tenantId == Guid.Empty) throw new ArgumentException("TenantId required.", nameof(tenantId));
        ArgumentException.ThrowIfNullOrWhiteSpace(orderReference);

        var sheet = new CuttingSheet
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            OrderReference = orderReference,
            Status = CuttingSheetStatus.Draft,
            CreatedAt = DateTime.UtcNow
        };
        sheet._lines.AddRange(lines.Select(l =>
            CuttingLine.Create(sheet.Id, l.PartName, l.MaterialType, l.WidthMm, l.HeightMm, l.ThicknessMm, l.Quantity, l.Notes)));
        return sheet;
    }

    public void Submit()
    {
        if (Status != CuttingSheetStatus.Draft)
            throw new InvalidOperationException($"Cannot submit sheet in status {Status}.");
        if (!_lines.Any())
            throw new InvalidOperationException("Cannot submit empty CuttingSheet.");
        Status = CuttingSheetStatus.Submitted;
        RaiseDomainEvent(new CuttingSheetSubmittedEvent(Id, TenantId, OrderReference, _lines.Count));
    }

    public void Complete()
    {
        if (Status != CuttingSheetStatus.Submitted)
            throw new InvalidOperationException($"Cannot complete sheet in status {Status}.");
        Status = CuttingSheetStatus.Completed;
    }
}
