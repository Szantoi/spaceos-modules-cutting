using Ardalis.Result;
using MediatR;
using SpaceOS.Modules.Cutting.Analytics.Domain.Common;
using SpaceOS.Modules.Cutting.Analytics.Domain.Interfaces;
using SpaceOS.Modules.Cutting.Analytics.Domain.ReadModels;

namespace SpaceOS.Modules.Cutting.Analytics.Application.Queries;

/// <summary>Returns a paginated list of daily execution metrics for a tenant.</summary>
public sealed record GetDailyExecutionMetricsQuery(
    Guid TenantId,
    string? MachineId,
    DateOnly From,
    DateOnly To,
    int Skip = 0,
    int Take = 100) : IRequest<Result<AnalyticsPagedResult<DailyExecutionMetric>>>;

/// <summary>Handles <see cref="GetDailyExecutionMetricsQuery"/>.</summary>
public sealed class GetDailyExecutionMetricsQueryHandler(IAnalyticsQueryRepository repo)
    : IRequestHandler<GetDailyExecutionMetricsQuery, Result<AnalyticsPagedResult<DailyExecutionMetric>>>
{
    private const int MaxTake = 500;

    /// <inheritdoc/>
    public async Task<Result<AnalyticsPagedResult<DailyExecutionMetric>>> Handle(
        GetDailyExecutionMetricsQuery q, CancellationToken ct)
    {
        if (q.TenantId == Guid.Empty)
            return Result<AnalyticsPagedResult<DailyExecutionMetric>>.Invalid(new ValidationError("TenantId required."));
        if (q.From > q.To)
            return Result<AnalyticsPagedResult<DailyExecutionMetric>>.Invalid(new ValidationError("From must be <= To."));

        var take = Math.Min(q.Take, MaxTake);
        var items = await repo.GetExecutionMetricsAsync(
            q.TenantId, q.MachineId, q.From, q.To, q.Skip, take, ct).ConfigureAwait(false);

        return Result<AnalyticsPagedResult<DailyExecutionMetric>>.Success(
            new AnalyticsPagedResult<DailyExecutionMetric>(items, items.Count, q.Skip, take));
    }
}
