using SpaceOS.Modules.Cutting.Contracts.Dtos;
using SpaceOS.Modules.Cutting.Contracts.Providers;
using SpaceOS.Modules.Inventory.Contracts.Dtos;

namespace SpaceOS.Modules.Cutting.Infrastructure.Adapters.Providers;

/// <summary>
/// Submit-only adapter for manual (paper/offline) workflows.
/// Delegates persistence to the builtin provider but returns empty nesting/health data
/// because nesting is performed manually outside the system.
/// </summary>
internal sealed class ManualAdapter : ICuttingProvider
{
    private readonly BuiltinCuttingProvider _builtin;

    public ManualAdapter(BuiltinCuttingProvider builtin)
    {
        ArgumentNullException.ThrowIfNull(builtin);
        _builtin = builtin;
    }

    /// <inheritdoc />
    public Task<Guid> SubmitCuttingSheetAsync(CuttingSheetDto sheet, CancellationToken ct = default)
        => _builtin.SubmitCuttingSheetAsync(sheet, ct);

    /// <inheritdoc />
    public Task<PanelAssignmentDto> GetNestingResultAsync(Guid sheetId, CancellationToken ct = default)
        => Task.FromResult(new PanelAssignmentDto(sheetId, Array.Empty<PanelPlacementDto>(), 0m, 0));

    /// <inheritdoc />
    public Task<CuttingExecutionDto> GetExecutionStatusAsync(Guid sheetId, CancellationToken ct = default)
        => Task.FromResult(new CuttingExecutionDto(sheetId, "ManualMode", null, null, null));

    /// <inheritdoc />
    public Task<WasteReportDto> GetWasteReportAsync(DateRange range, CancellationToken ct = default)
        => Task.FromResult(new WasteReportDto(0m, 0m, 0m, Array.Empty<WasteLineDto>()));
}
