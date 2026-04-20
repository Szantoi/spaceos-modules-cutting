using Ardalis.Result;
using MediatR;
using SpaceOS.Modules.Cutting.Application.Events;
using SpaceOS.Modules.Cutting.Domain.Interfaces;

namespace SpaceOS.Modules.Cutting.Application.Commands.CompleteJob;

public sealed class CompleteJobCommandHandler : IRequestHandler<CompleteJobCommand, Result<Unit>>
{
    private readonly ICuttingRepository _repository;
    private readonly ICuttingEventPublisher _eventPublisher;

    public CompleteJobCommandHandler(
        ICuttingRepository repository,
        ICuttingEventPublisher eventPublisher)
    {
        _repository = repository;
        _eventPublisher = eventPublisher;
    }

    public async Task<Result<Unit>> Handle(CompleteJobCommand request, CancellationToken ct)
    {
        var job = await _repository.GetCuttingJobTrackedAsync(request.JobId, ct).ConfigureAwait(false);
        if (job is null)
            return Result<Unit>.NotFound($"CuttingJob {request.JobId} not found.");

        try
        {
            job.MarkAsCut();
        }
        catch (InvalidOperationException ex)
        {
            return Result<Unit>.Invalid(new ValidationError(ex.Message));
        }

        await _repository.SaveChangesAsync(ct).ConfigureAwait(false);

        await _eventPublisher.PublishJobCompletedAsync(
            jobId: request.JobId,
            tenantId: request.TenantId,
            cuttingSheetId: request.CuttingSheetId,
            completedAt: DateTime.UtcNow,
            yieldPct: request.YieldPct,
            wasteM2: request.WasteM2,
            ct: ct).ConfigureAwait(false);

        return Result<Unit>.Success(Unit.Value);
    }
}
