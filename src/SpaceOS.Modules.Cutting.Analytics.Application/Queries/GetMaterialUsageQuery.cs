using Ardalis.Result;
using MediatR;
using SpaceOS.Modules.Cutting.Analytics.Domain.Common;
using SpaceOS.Modules.Cutting.Analytics.Domain.Interfaces;
using SpaceOS.Modules.Cutting.Analytics.Domain.ReadModels;

namespace SpaceOS.Modules.Cutting.Analytics.Application.Queries;

/// <summary>Returns a paginated list of daily material usage records for a tenant.</summary>
public sealed record GetMaterialUsageQuery(
    Guid TenantId,
    string? MaterialCode,
    DateOnly From,
    DateOnly To,
    int Skip = 0,
    int Take = 100) : IRequest<Result<AnalyticsPagedResult<DailyMaterialUsage>>>;

/// <summary>Handles <see cref="GetMaterialUsageQuery"/>.</summary>
public sealed class GetMaterialUsageQueryHandler(IAnalyticsQueryRepository repo)
    : IRequestHandler<GetMaterialUsageQuery, Result<AnalyticsPagedResult<DailyMaterialUsage>>>
{
    private const int MaxTake = 500;

    /// <inheritdoc/>
    public async Task<Result<AnalyticsPagedResult<DailyMaterialUsage>>> Handle(
        GetMaterialUsageQuery q, CancellationToken ct)
    {
        if (q.TenantId == Guid.Empty)
            return Result<AnalyticsPagedResult<DailyMaterialUsage>>.Invalid(new ValidationError("TenantId required."));
        if (q.From > q.To)
            return Result<AnalyticsPagedResult<DailyMaterialUsage>>.Invalid(new ValidationError("From must be <= To."));

        var take = Math.Min(q.Take, MaxTake);
        var items = await repo.GetMaterialUsageAsync(
            q.TenantId, q.MaterialCode, q.From, q.To, q.Skip, take, ct).ConfigureAwait(false);

        return Result<AnalyticsPagedResult<DailyMaterialUsage>>.Success(
            new AnalyticsPagedResult<DailyMaterialUsage>(items, items.Count, q.Skip, take));
    }
}
