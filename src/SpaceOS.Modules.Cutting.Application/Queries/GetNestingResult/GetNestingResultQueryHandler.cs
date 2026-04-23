using Ardalis.Result;
using MediatR;
using SpaceOS.Modules.Cutting.Domain.Entities;
using SpaceOS.Modules.Cutting.Domain.Interfaces;
using SpaceOS.Modules.Inventory.Contracts.Providers;
using SpaceOS.Nesting.Algorithms;
using SpaceOS.Nesting.Algorithms.Models;
using System.Text.Json;

namespace SpaceOS.Modules.Cutting.Application.Queries.GetNestingResult;

public sealed class GetNestingResultQueryHandler : IRequestHandler<GetNestingResultQuery, Result<NestingResultResponse>>
{
    private readonly ICuttingRepository _repository;
    private readonly IInventoryProvider _inventoryProvider;
    private readonly INestingStrategy _nestingStrategy;
    private readonly IPlanNestingSnapshotRepository _snapshotRepo;

    public GetNestingResultQueryHandler(
        ICuttingRepository repository,
        IInventoryProvider inventoryProvider,
        INestingStrategy nestingStrategy,
        IPlanNestingSnapshotRepository snapshotRepo)
    {
        _repository = repository;
        _inventoryProvider = inventoryProvider;
        _nestingStrategy = nestingStrategy;
        _snapshotRepo = snapshotRepo;
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
            .Select(l => new NestingPart(
                PartId: l.Id.ToString(),
                Name: l.PartName,
                WidthMm: l.WidthMm,
                HeightMm: l.HeightMm,
                CanRotate: true,
                Quantity: l.Quantity))
            .ToList();

        // Fetch panels from Inventory — graceful degradation on failure
        var availablePanels = new List<AvailablePanel>();
        try
        {
            foreach (var materialType in sheet.Lines.Select(l => l.MaterialType).Distinct())
            {
                var stock = await _inventoryProvider.GetStockAsync(materialType, ct).ConfigureAwait(false);
                for (int i = 0; i < stock.FullPanelCount; i++)
                    availablePanels.Add(new AvailablePanel(
                        PanelId: Guid.NewGuid().ToString(),
                        MaterialCode: materialType,
                        WidthMm: stock.WidthMm,
                        HeightMm: stock.HeightMm,
                        IsOffcut: false));

                var offcuts = await _inventoryProvider.GetOffcutsAsync(materialType, ct).ConfigureAwait(false);
                foreach (var offcut in offcuts)
                    availablePanels.Add(new AvailablePanel(
                        PanelId: offcut.Id.ToString(),
                        MaterialCode: materialType,
                        WidthMm: offcut.WidthMm,
                        HeightMm: offcut.HeightMm,
                        IsOffcut: true));
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
            var input = new NestingInput(parts, availablePanels);
            var nestingResult = await _nestingStrategy.ComputeAsync(input, ct).ConfigureAwait(false);
            panelAssignments = nestingResult.Assignments.Select(a => new PanelAssignmentResponse(
                Guid.TryParse(a.PanelId, out var panelGuid) ? panelGuid : Guid.Empty,
                a.MaterialCode,
                (int)a.PanelWidthMm,
                (int)a.PanelHeightMm,
                a.PlacedParts.Select(p => new PlacedPartResponse(
                    p.Name, (int)p.X, (int)p.Y, (int)p.WidthMm, (int)p.HeightMm, p.IsRotated)).ToList(),
                (int)a.WasteAreaMm2,
                a.UtilizationPercent)).ToList();

            // Save nesting snapshot if a PlanId context is provided
            if (request.PlanId.HasValue && request.TenantId.HasValue)
            {
                var existing = await _snapshotRepo.GetByPlanAsync(request.PlanId.Value, ct).ConfigureAwait(false);
                if (existing is null)
                {
                    var snapshotPayload = nestingResult.Assignments
                        .Select(a => new
                        {
                            a.MaterialCode,
                            WastePieces = a.WastePieces.Select(w => new
                            {
                                w.X, w.Y, w.WidthMm, w.HeightMm
                            }).ToList()
                        }).ToList();

                    var json = JsonSerializer.Serialize(snapshotPayload);
                    var snapshot = PlanNestingSnapshot.Create(request.PlanId.Value, request.TenantId.Value, json);
                    await _snapshotRepo.AddAsync(snapshot, ct).ConfigureAwait(false);
                    await _snapshotRepo.SaveChangesAsync(ct).ConfigureAwait(false);
                }
            }
        }

        return Result<NestingResultResponse>.Success(new NestingResultResponse(
            sheet.Id,
            sheet.OrderReference,
            groups,
            sheet.Lines.Sum(l => l.Quantity),
            panelAssignments));
    }
}
