namespace SpaceOS.Modules.Cutting.Analytics.Domain.ReadModels;

/// <summary>
/// Per-material, per-day consumption summary projected from cutting events.
/// Read-model entity — no domain events, no FSM.
/// </summary>
public sealed class DailyMaterialUsage
{
    /// <summary>Surrogate primary key.</summary>
    public Guid Id { get; private set; }

    /// <summary>Owning tenant.</summary>
    public Guid TenantId { get; private set; }

    /// <summary>Material catalogue code (e.g. "MDF-18", "HDF-3").</summary>
    public string MaterialCode { get; private set; } = string.Empty;

    /// <summary>Calendar date the metric covers.</summary>
    public DateOnly UsageDate { get; private set; }

    /// <summary>Total panel area consumed in mm².</summary>
    public decimal TotalAreaUsedMm2 { get; private set; }

    /// <summary>Area that became waste (offcut or scrap) in mm².</summary>
    public decimal WasteAreaMm2 { get; private set; }

    /// <summary>Number of offcut pieces generated.</summary>
    public int OffcutCount { get; private set; }

    /// <summary>UTC timestamp of the last projection write.</summary>
    public DateTime LastUpdatedAt { get; private set; }

    private DailyMaterialUsage() { }

    /// <summary>Creates a new <see cref="DailyMaterialUsage"/> with validated inputs.</summary>
    public static DailyMaterialUsage Create(
        Guid tenantId, string materialCode, DateOnly date,
        decimal totalAreaMm2, decimal wasteAreaMm2, int offcutCount)
    {
        if (tenantId == Guid.Empty) throw new ArgumentException("TenantId required.", nameof(tenantId));
        if (string.IsNullOrWhiteSpace(materialCode)) throw new ArgumentException("MaterialCode required.", nameof(materialCode));
        if (totalAreaMm2 < 0) throw new ArgumentOutOfRangeException(nameof(totalAreaMm2));
        if (wasteAreaMm2 < 0 || wasteAreaMm2 > totalAreaMm2) throw new ArgumentOutOfRangeException(nameof(wasteAreaMm2));
        if (offcutCount < 0) throw new ArgumentOutOfRangeException(nameof(offcutCount));

        return new DailyMaterialUsage
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            MaterialCode = materialCode,
            UsageDate = date,
            TotalAreaUsedMm2 = totalAreaMm2,
            WasteAreaMm2 = wasteAreaMm2,
            OffcutCount = offcutCount,
            LastUpdatedAt = DateTime.UtcNow
        };
    }

    /// <summary>Updates counters when a later event arrives for the same material/date.</summary>
    public void Update(decimal totalAreaMm2, decimal wasteAreaMm2, int offcutCount)
    {
        if (totalAreaMm2 < 0) throw new ArgumentOutOfRangeException(nameof(totalAreaMm2));
        if (wasteAreaMm2 < 0 || wasteAreaMm2 > totalAreaMm2) throw new ArgumentOutOfRangeException(nameof(wasteAreaMm2));
        if (offcutCount < 0) throw new ArgumentOutOfRangeException(nameof(offcutCount));

        TotalAreaUsedMm2 = totalAreaMm2;
        WasteAreaMm2 = wasteAreaMm2;
        OffcutCount = offcutCount;
        LastUpdatedAt = DateTime.UtcNow;
    }
}
