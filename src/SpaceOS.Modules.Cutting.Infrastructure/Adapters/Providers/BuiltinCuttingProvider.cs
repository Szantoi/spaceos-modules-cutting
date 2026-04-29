using SpaceOS.Modules.Cutting.Contracts.Dtos;
using SpaceOS.Modules.Cutting.Contracts.Providers;
using SpaceOS.Modules.Inventory.Contracts.Dtos;

namespace SpaceOS.Modules.Cutting.Infrastructure.Adapters.Providers;

/// <summary>
/// BE-02: Backward-compatible builtin adapter. Delegates all calls to the existing
/// <see cref="CuttingProviderAdapter"/> MediatR pipeline. Named "builtin" in the adapter registry.
/// </summary>
internal sealed class BuiltinCuttingProvider : ICuttingProvider
{
    private readonly CuttingProviderAdapter _inner;

    public BuiltinCuttingProvider(CuttingProviderAdapter inner)
    {
        ArgumentNullException.ThrowIfNull(inner);
        _inner = inner;
    }

    /// <inheritdoc />
    public Task<Guid> SubmitCuttingSheetAsync(CuttingSheetDto sheet, CancellationToken ct = default)
        => _inner.SubmitCuttingSheetAsync(sheet, ct);

    /// <inheritdoc />
    public Task<PanelAssignmentDto> GetNestingResultAsync(Guid sheetId, CancellationToken ct = default)
        => _inner.GetNestingResultAsync(sheetId, ct);

    /// <inheritdoc />
    public Task<CuttingExecutionDto> GetExecutionStatusAsync(Guid sheetId, CancellationToken ct = default)
        => _inner.GetExecutionStatusAsync(sheetId, ct);

    /// <inheritdoc />
    public Task<WasteReportDto> GetWasteReportAsync(DateRange range, CancellationToken ct = default)
        => _inner.GetWasteReportAsync(range, ct);
}
