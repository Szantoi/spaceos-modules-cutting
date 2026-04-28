using Ardalis.Result;
using MediatR;
using SpaceOS.Modules.Cutting.Application.Services;
using SpaceOS.Modules.Cutting.Domain.Entities;
using SpaceOS.Modules.Cutting.Domain.Enums;
using SpaceOS.Modules.Cutting.Domain.Interfaces;
using SpaceOS.Nesting.Algorithms;
using SpaceOS.Nesting.Algorithms.Models;
using System.Text.Json;

namespace SpaceOS.Modules.Cutting.Application.Commands.PublishCuttingPlan;

public sealed class PublishCuttingPlanCommandHandler
    : IRequestHandler<PublishCuttingPlanCommand, Result<Unit>>
{
    private readonly ICuttingRepository _repository;
    private readonly INestingStrategy _nestingStrategy;
    private readonly PanelSourceService _panelSource;
    private readonly IPlanNestingSnapshotRepository _snapshotRepo;

    public PublishCuttingPlanCommandHandler(
        ICuttingRepository repository,
        INestingStrategy nestingStrategy,
        PanelSourceService panelSource,
        IPlanNestingSnapshotRepository snapshotRepo)
    {
        _repository = repository;
        _nestingStrategy = nestingStrategy;
        _panelSource = panelSource;
        _snapshotRepo = snapshotRepo;
    }

    public async Task<Result<Unit>> Handle(PublishCuttingPlanCommand request, CancellationToken ct)
    {
        var plan = await _repository.GetCuttingPlanTrackedAsync(request.PlanId, ct).ConfigureAwait(false);
        if (plan is null)
            return Result<Unit>.NotFound($"CuttingPlan {request.PlanId} not found.");

        // Collect all CuttingJobs from DaySlots
        var allJobs = plan.DaySlots.SelectMany(s => s.Jobs).ToList();

        // Run nesting if there are jobs
        if (allJobs.Count > 0)
        {
            // Map jobs → NestingPart with GrainDirection → CanRotate
            var parts = allJobs.Select(j => new NestingPart(
                PartId: j.Id.ToString(),
                Name: $"{j.Material}-{j.WidthMm}x{j.HeightMm}",
                WidthMm: j.WidthMm > 0 ? j.WidthMm : 600m,
                HeightMm: j.HeightMm > 0 ? j.HeightMm : 400m,
                CanRotate: j.GrainDirection == GrainDirection.None,
                Quantity: 1
            )).ToList();

            // Get material codes from jobs
            var materialCodes = allJobs
                .Where(j => !string.IsNullOrWhiteSpace(j.Material))
                .Select(j => j.Material)
                .Distinct();

            // Fetch available panels from Inventory
            var panels = await _panelSource.GetAvailablePanelsAsync(materialCodes, ct).ConfigureAwait(false);

            if (panels.Count > 0)
            {
                var input = new NestingInput(parts, panels);
                var nestingResult = await _nestingStrategy.ComputeAsync(input, ct).ConfigureAwait(false);

                // Build snapshot JSON (WastePieces per assignment for offcut registration)
                var wastePayload = nestingResult.Assignments.Select(a => new
                {
                    a.MaterialCode,
                    WastePieces = a.WastePieces.Select(w => new { w.X, w.Y, w.WidthMm, w.HeightMm }).ToList()
                }).ToList();

                // Build placements JSON
                var placementsPayload = nestingResult.Assignments.Select(a => new
                {
                    a.PanelId,
                    a.MaterialCode,
                    a.PanelWidthMm,
                    a.PanelHeightMm,
                    Parts = a.PlacedParts.Select(p => new
                    {
                        p.PartId, p.Name, p.X, p.Y, p.WidthMm, p.HeightMm, p.IsRotated
                    }).ToList()
                }).ToList();

                var nestingResultJson = JsonSerializer.Serialize(wastePayload);
                var placementsJson = JsonSerializer.Serialize(placementsPayload);

                var totalWasteArea = nestingResult.Assignments.Sum(a => (long)a.WasteAreaMm2);
                var totalPanelArea = nestingResult.Assignments.Sum(a => a.PanelWidthMm * a.PanelHeightMm);
                var yieldPercent = totalPanelArea > 0
                    ? Math.Round((totalPanelArea - totalWasteArea) / totalPanelArea * 100m, 2)
                    : 0m;

                // Save or update snapshot
                var existingSnapshot = await _snapshotRepo.GetByPlanAsync(request.PlanId, ct).ConfigureAwait(false);
                if (existingSnapshot is null)
                {
                    var snapshot = PlanNestingSnapshot.Create(
                        request.PlanId,
                        plan.TenantId,
                        nestingResultJson,
                        placementsJson,
                        yieldPercent,
                        totalWasteArea,
                        _nestingStrategy.AlgorithmName);
                    await _snapshotRepo.AddAsync(snapshot, ct).ConfigureAwait(false);
                }
            }
        }

        // Publish the plan (FSM transition: Draft → Published)
        var result = plan.Publish(request.ProfileSnapshotId);
        if (!result.IsSuccess)
            return Result<Unit>.Invalid(result.ValidationErrors.ToArray());

        await _snapshotRepo.SaveChangesAsync(ct).ConfigureAwait(false);
        await _repository.SaveChangesAsync(ct).ConfigureAwait(false);
        return Result<Unit>.Success(Unit.Value);
    }
}
