using Ardalis.Result;
using MediatR;
using SpaceOS.Modules.Cutting.Analytics.Domain.Aggregates;
using SpaceOS.Modules.Cutting.Analytics.Domain.Interfaces;

namespace SpaceOS.Modules.Cutting.Analytics.Application.Queries;

/// <summary>Returns the status of an analytics rebuild job scoped to a tenant.</summary>
public sealed record GetRebuildJobStatusQuery(
    Guid TenantId,
    Guid JobId) : IRequest<Result<AnalyticsRebuildJob>>;

/// <summary>Handles <see cref="GetRebuildJobStatusQuery"/>.</summary>
public sealed class GetRebuildJobStatusQueryHandler(IAnalyticsQueryRepository repo)
    : IRequestHandler<GetRebuildJobStatusQuery, Result<AnalyticsRebuildJob>>
{
    /// <inheritdoc/>
    public async Task<Result<AnalyticsRebuildJob>> Handle(
        GetRebuildJobStatusQuery q, CancellationToken ct)
    {
        if (q.TenantId == Guid.Empty)
            return Result<AnalyticsRebuildJob>.Invalid(new ValidationError("TenantId required."));
        if (q.JobId == Guid.Empty)
            return Result<AnalyticsRebuildJob>.Invalid(new ValidationError("JobId required."));

        var job = await repo.GetRebuildJobAsync(q.JobId, ct).ConfigureAwait(false);
        if (job is null)
            return Result<AnalyticsRebuildJob>.NotFound($"RebuildJob {q.JobId} not found.");
        if (job.TenantId != q.TenantId)
            return Result<AnalyticsRebuildJob>.Forbidden();

        return Result<AnalyticsRebuildJob>.Success(job);
    }
}
