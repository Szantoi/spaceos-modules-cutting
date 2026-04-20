namespace SpaceOS.Modules.Cutting.Application.Events;

/// <summary>
/// Cross-service event publisher: fires integration events from CUTTING → INVENTORY
/// when key domain transitions occur.
/// </summary>
public interface ICuttingEventPublisher
{
    /// <summary>
    /// Publishes a CuttingJobCompleted event to the Inventory service so it can
    /// register offcut records and update stock analytics.
    /// </summary>
    Task PublishJobCompletedAsync(
        Guid jobId,
        Guid tenantId,
        Guid cuttingSheetId,
        DateTime completedAt,
        decimal yieldPct,
        decimal wasteM2,
        CancellationToken ct = default);
}
