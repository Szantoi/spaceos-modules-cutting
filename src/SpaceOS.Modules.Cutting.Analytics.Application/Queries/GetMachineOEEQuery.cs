using Ardalis.Result;
using MediatR;
using SpaceOS.Modules.Cutting.Analytics.Domain.Common;
using SpaceOS.Modules.Cutting.Analytics.Domain.Interfaces;
using SpaceOS.Modules.Cutting.Analytics.Domain.ReadModels;
using SpaceOS.Modules.Cutting.Analytics.Domain.ValueObjects;

namespace SpaceOS.Modules.Cutting.Analytics.Application.Queries;

/// <summary>Returns a paginated list of hourly OEE snapshots for a tenant.</summary>
public sealed record GetMachineOEEQuery(
    Guid TenantId,
    string? MachineId,
    DateTime From,
    DateTime To,
    MetricResolution Resolution = MetricResolution.Hourly,
    int Skip = 0,
    int Take = 100) : IRequest<Result<AnalyticsPagedResult<MachineOEEHourly>>>;

/// <summary>Handles <see cref="GetMachineOEEQuery"/>.</summary>
public sealed class GetMachineOEEQueryHandler(IAnalyticsQueryRepository repo)
    : IRequestHandler<GetMachineOEEQuery, Result<AnalyticsPagedResult<MachineOEEHourly>>>
{
    private const int MaxTake = 500;

    /// <inheritdoc/>
    public async Task<Result<AnalyticsPagedResult<MachineOEEHourly>>> Handle(
        GetMachineOEEQuery q, CancellationToken ct)
    {
        if (q.TenantId == Guid.Empty)
            return Result<AnalyticsPagedResult<MachineOEEHourly>>.Invalid(new ValidationError("TenantId required."));
        if (q.From >= q.To)
            return Result<AnalyticsPagedResult<MachineOEEHourly>>.Invalid(new ValidationError("From must be before To."));

        var take = Math.Min(q.Take, MaxTake);
        var items = await repo.GetOEEAsync(
            q.TenantId, q.MachineId, q.From, q.To, q.Skip, take, ct).ConfigureAwait(false);

        return Result<AnalyticsPagedResult<MachineOEEHourly>>.Success(
            new AnalyticsPagedResult<MachineOEEHourly>(items, items.Count, q.Skip, take));
    }
}
