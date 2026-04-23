using SpaceOS.Modules.Cutting.Domain.Common;
using SpaceOS.Modules.Cutting.Domain.Enums;

namespace SpaceOS.Modules.Cutting.Domain.Aggregates;

/// <summary>
/// Tracks a panel reservation made in the Inventory service on behalf of a CuttingPlan DaySlot.
/// The InventoryReservationId is the authoritative reference — Inventory is the SSoT for stock.
/// </summary>
public sealed class PanelReservation : AggregateRoot
{
    public Guid Id { get; private set; }
    public Guid CuttingPlanId { get; private set; }
    public Guid DaySlotId { get; private set; }
    public Guid TenantId { get; private set; }

    /// <summary>Reference to the reservation record in the Inventory service (SSoT).</summary>
    public Guid InventoryReservationId { get; private set; }

    public string MaterialCode { get; private set; } = string.Empty;
    public decimal WidthMm { get; private set; }
    public decimal HeightMm { get; private set; }
    public PanelReservationStatus Status { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private PanelReservation() { }

    public static PanelReservation Create(
        Guid tenantId,
        Guid planId,
        Guid slotId,
        Guid inventoryReservationId,
        string materialCode,
        decimal widthMm,
        decimal heightMm)
    {
        if (tenantId == Guid.Empty)       throw new ArgumentException("TenantId required.", nameof(tenantId));
        if (planId == Guid.Empty)         throw new ArgumentException("CuttingPlanId required.", nameof(planId));
        if (slotId == Guid.Empty)         throw new ArgumentException("DaySlotId required.", nameof(slotId));
        if (inventoryReservationId == Guid.Empty)
            throw new ArgumentException("InventoryReservationId required.", nameof(inventoryReservationId));
        if (string.IsNullOrWhiteSpace(materialCode))
            throw new ArgumentException("MaterialCode required.", nameof(materialCode));
        if (widthMm <= 0)  throw new ArgumentException("WidthMm must be positive.", nameof(widthMm));
        if (heightMm <= 0) throw new ArgumentException("HeightMm must be positive.", nameof(heightMm));

        return new PanelReservation
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            CuttingPlanId = planId,
            DaySlotId = slotId,
            InventoryReservationId = inventoryReservationId,
            MaterialCode = materialCode,
            WidthMm = widthMm,
            HeightMm = heightMm,
            Status = PanelReservationStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };
    }

    /// <summary>Pending → Confirmed after Inventory confirms the reservation.</summary>
    public void Confirm()
    {
        if (Status != PanelReservationStatus.Pending)
            throw new InvalidOperationException($"PanelReservation {Id} cannot be confirmed from status '{Status}'.");
        Status = PanelReservationStatus.Confirmed;
    }

    /// <summary>Pending/Confirmed → Released when the plan is cancelled or the slot is freed.</summary>
    public void Release()
    {
        if (Status == PanelReservationStatus.Released)
            throw new InvalidOperationException($"PanelReservation {Id} is already Released.");
        Status = PanelReservationStatus.Released;
    }
}
