using MediatR;
using SpaceOS.Modules.Cutting.Application.Commands.SubmitCuttingSheet;
using SpaceOS.Modules.Cutting.Application.Queries.GetExecutionStatus;
using SpaceOS.Modules.Cutting.Application.Queries.GetNestingResult;
using SpaceOS.Modules.Cutting.Application.Queries.GetWasteReport;
using SpaceOS.Modules.Cutting.Contracts.Dtos;
using SpaceOS.Modules.Cutting.Contracts.Providers;
using SpaceOS.Modules.Inventory.Contracts.Dtos;

namespace SpaceOS.Modules.Cutting.Infrastructure.Adapters;

public class CuttingProviderAdapter : ICuttingProvider
{
    private readonly IMediator _mediator;
    private readonly ICuttingTenantAccessor _tenantAccessor;

    public CuttingProviderAdapter(IMediator mediator, ICuttingTenantAccessor tenantAccessor)
    {
        _mediator = mediator;
        _tenantAccessor = tenantAccessor;
    }

    public async Task<Guid> SubmitCuttingSheetAsync(CuttingSheetDto sheet, CancellationToken ct = default)
    {
        var tenantId = _tenantAccessor.TenantId;
        var lines = sheet.Lines.Select(l => new CuttingLineInput(
            l.Name, l.PartType, l.RawWidth, l.RawHeight, l.Thickness, l.Quantity, l.EdgeBanding)).ToList();

        var command = new SubmitCuttingSheetCommand(tenantId, sheet.SourceOrderId.ToString(), lines);
        var result = await _mediator.Send(command, ct).ConfigureAwait(false);

        if (!result.IsSuccess)
            throw new InvalidOperationException(result.Errors.FirstOrDefault() ?? "Submit failed.");

        return result.Value;
    }

    public async Task<PanelAssignmentDto> GetNestingResultAsync(Guid sheetId, CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetNestingResultQuery(sheetId), ct).ConfigureAwait(false);
        if (!result.IsSuccess)
            return new PanelAssignmentDto(sheetId, Array.Empty<PanelPlacementDto>(), 0m, 0);

        var placements = result.Value.Groups
            .SelectMany(g => g.Lines.Select(l => new PanelPlacementDto(l.PartName, 0, 0, l.WidthMm, l.HeightMm, false)))
            .ToList();

        return new PanelAssignmentDto(sheetId, placements, 0m, result.Value.Groups.Count);
    }

    public async Task<CuttingExecutionDto> GetExecutionStatusAsync(Guid sheetId, CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetExecutionStatusQuery(sheetId), ct).ConfigureAwait(false);
        if (!result.IsSuccess)
            return new CuttingExecutionDto(sheetId, "NotFound", null, null, null);

        return new CuttingExecutionDto(
            result.Value.SheetId,
            result.Value.Status,
            result.Value.StartedAt,
            result.Value.CompletedAt,
            null);
    }

    public async Task<WasteReportDto> GetWasteReportAsync(DateRange range, CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetWasteReportQuery(range.From, range.To), ct).ConfigureAwait(false);
        if (!result.IsSuccess)
            return new WasteReportDto(0, 0, 0, Array.Empty<WasteLineDto>());

        return new WasteReportDto(
            result.Value.TotalWasteAreaCm2,
            0m,
            result.Value.ExecutionCount > 0 ? result.Value.TotalWasteAreaCm2 / result.Value.ExecutionCount : 0m,
            Array.Empty<WasteLineDto>());
    }
}
