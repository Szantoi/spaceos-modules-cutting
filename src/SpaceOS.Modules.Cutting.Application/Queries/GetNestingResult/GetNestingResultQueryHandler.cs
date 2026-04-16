using Ardalis.Result;
using MediatR;
using SpaceOS.Modules.Cutting.Domain.Interfaces;
using SpaceOS.Modules.Cutting.Domain.Services;
using SpaceOS.Modules.Inventory.Contracts.Providers;

namespace SpaceOS.Modules.Cutting.Application.Queries.GetNestingResult;

public sealed class GetNestingResultQueryHandler : IRequestHandler<GetNestingResultQuery, Result<NestingResultResponse>>
{
    private readonly ICuttingRepository _repository;
    private readonly IInventoryProvider _inventoryProvider;
    private readonly NestingService _nestingService;

    public GetNestingResultQueryHandler(
        ICuttingRepository repository,
        IInventoryProvider inventoryProvider,
        NestingService nestingService)
    {
        _repository = repository;
        _inventoryProvider = inventoryProvider;
        _nestingService = nestingService;
    }

    public async Task<Result<NestingResultResponse>> Handle(GetNestingResultQuery request, CancellationToken ct)
    {
        var sheet = await _repository.GetCuttingSheetByIdAsync(request.SheetId, ct).ConfigureAwait(false);
        if (sheet is null)
            return Result<NestingResultResponse>.NotFound($"CuttingSheet {request.SheetId} not found.");

        var groups = sheet.Lines
            .GroupBy(l => (l.MaterialType, l.ThicknessMm))
            .Select(g => new NestingGroupResponse(
                g.Key.MaterialType,
                g.Key.ThicknessMm,
                g.Select(l => new NestingLineResponse(l.PartName, l.WidthMm, l.HeightMm, l.Quantity)).ToList()))
            .ToList();

        var parts = sheet.Lines
            .SelectMany(l => Enumerable.Range(0, l.Quantity)
                .Select(_ => new CuttingLineRequest(l.PartName, l.WidthMm, l.HeightMm)))
            .ToList();

        // Fetch panels from Inventory — graceful degradation on failure
        var availablePanels = new List<AvailablePanel>();
        try
        {
            foreach (var materialType in sheet.Lines.Select(l => l.MaterialType).Distinct())
            {
                var stock = await _inventoryProvider.GetStockAsync(materialType, ct).ConfigureAwait(false);
                for (int i = 0; i < stock.FullPanelCount; i++)
                    availablePanels.Add(new AvailablePanel(Guid.NewGuid(), materialType, stock.WidthMm, stock.HeightMm, IsOffcut: false));

                var offcuts = await _inventoryProvider.GetOffcutsAsync(materialType, ct).ConfigureAwait(false);
                foreach (var offcut in offcuts)
                    availablePanels.Add(new AvailablePanel(offcut.Id, materialType, offcut.WidthMm, offcut.HeightMm, IsOffcut: true));
            }
        }
        catch
        {
            // Inventory unavailable — return grouping only
            return Result<NestingResultResponse>.Success(new NestingResultResponse(
                sheet.Id, sheet.OrderReference, groups, sheet.Lines.Sum(l => l.Quantity)));
        }

        IReadOnlyList<PanelAssignmentResponse>? panelAssignments = null;
        if (availablePanels.Any() && parts.Any())
        {
            var assignments = _nestingService.ComputeNesting(parts, availablePanels);
            panelAssignments = assignments.Select(a => new PanelAssignmentResponse(
                a.PanelStockId,
                a.MaterialType,
                a.PanelWidthMm,
                a.PanelHeightMm,
                a.PlacedParts.Select(p => new PlacedPartResponse(
                    p.PartName, p.X, p.Y, p.WidthMm, p.HeightMm, p.IsRotated)).ToList(),
                a.WasteAreaMm2,
                a.UtilizationPercent)).ToList();
        }

        return Result<NestingResultResponse>.Success(new NestingResultResponse(
            sheet.Id,
            sheet.OrderReference,
            groups,
            sheet.Lines.Sum(l => l.Quantity),
            panelAssignments));
    }
}
