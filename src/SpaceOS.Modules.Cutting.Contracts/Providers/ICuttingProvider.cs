using SpaceOS.Modules.Cutting.Contracts.Dtos;
using SpaceOS.Modules.Inventory.Contracts.Dtos;

namespace SpaceOS.Modules.Cutting.Contracts.Providers;

/// <summary>
/// Contract for submitting cutting sheets and querying nesting, execution status, and waste reports.
/// Implementations live in the Cutting module — callers depend only on this interface.
/// </summary>
public interface ICuttingProvider
{
    /// <summary>Submits a cutting sheet for nesting optimisation. Returns the sheet identifier.</summary>
    Task<Guid> SubmitCuttingSheetAsync(CuttingSheetDto sheet, CancellationToken ct = default);

    /// <summary>Returns the panel placement (nesting) result for the given sheet.</summary>
    Task<PanelAssignmentDto> GetNestingResultAsync(Guid sheetId, CancellationToken ct = default);

    /// <summary>Returns the current machine execution status for the given sheet.</summary>
    Task<CuttingExecutionDto> GetExecutionStatusAsync(Guid sheetId, CancellationToken ct = default);

    /// <summary>Returns aggregated waste statistics for the specified date range.</summary>
    Task<WasteReportDto> GetWasteReportAsync(DateRange range, CancellationToken ct = default);
}
