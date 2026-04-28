using Ardalis.Result;
using MediatR;
using SpaceOS.Modules.Cutting.Analytics.Domain.Common;
using SpaceOS.Modules.Cutting.Analytics.Domain.Interfaces;
using SpaceOS.Modules.Cutting.Analytics.Domain.ReadModels;
using SpaceOS.Modules.Cutting.Analytics.Domain.ValueObjects;

namespace SpaceOS.Modules.Cutting.Analytics.Application.Queries;

/// <summary>Returns a paginated list of privacy-safe operator metrics for a tenant.</summary>
public sealed record GetOperatorMetricsQuery(
    Guid TenantId,
    DateOnly From,
    DateOnly To,
    int Skip = 0,
    int Take = 100) : IRequest<Result<AnalyticsPagedResult<DailyOperatorMetric>>>;

/// <summary>Handles <see cref="GetOperatorMetricsQuery"/>. Only non-suppressed records are returned.</summary>
public sealed class GetOperatorMetricsQueryHandler(IAnalyticsQueryRepository repo)
    : IRequestHandler<GetOperatorMetricsQuery, Result<AnalyticsPagedResult<DailyOperatorMetric>>>
{
    private const int MaxTake = 500;
    private static readonly AnonymizationPolicy DefaultPolicy = AnonymizationPolicy.Default;

    /// <inheritdoc/>
    public async Task<Result<AnalyticsPagedResult<DailyOperatorMetric>>> Handle(
        GetOperatorMetricsQuery q, CancellationToken ct)
    {
        if (q.TenantId == Guid.Empty)
            return Result<AnalyticsPagedResult<DailyOperatorMetric>>.Invalid(new ValidationError("TenantId required."));
        if (q.From > q.To)
            return Result<AnalyticsPagedResult<DailyOperatorMetric>>.Invalid(new ValidationError("From must be <= To."));

        var take = Math.Min(q.Take, MaxTake);
        var items = await repo.GetOperatorMetricsAnonymizedAsync(
            q.TenantId, q.From, q.To, DefaultPolicy, q.Skip, take, ct).ConfigureAwait(false);

        return Result<AnalyticsPagedResult<DailyOperatorMetric>>.Success(
            new AnalyticsPagedResult<DailyOperatorMetric>(items, items.Count, q.Skip, take));
    }
}
