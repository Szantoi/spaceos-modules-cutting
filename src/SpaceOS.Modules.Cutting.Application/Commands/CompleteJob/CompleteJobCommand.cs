using Ardalis.Result;
using MediatR;

namespace SpaceOS.Modules.Cutting.Application.Commands.CompleteJob;

/// <summary>
/// Marks a CuttingJob as "Cut" and fires the cross-service CuttingJobCompleted event
/// so the Inventory module can register offcut records.
/// </summary>
public sealed record CompleteJobCommand(
    Guid JobId,
    Guid TenantId,
    Guid CuttingSheetId,
    decimal YieldPct,
    decimal WasteM2) : IRequest<Result<Unit>>;
