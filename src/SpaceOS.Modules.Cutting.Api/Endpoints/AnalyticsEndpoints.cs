using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SpaceOS.Modules.Cutting.Analytics.Application.Queries;
using SpaceOS.Modules.Cutting.Analytics.Domain.Aggregates;
using SpaceOS.Modules.Cutting.Analytics.Domain.Interfaces;
using SpaceOS.Modules.Cutting.Analytics.Domain.ValueObjects;
using ArdalisResultStatus = Ardalis.Result.ResultStatus;
using HttpIResult = Microsoft.AspNetCore.Http.IResult;

namespace SpaceOS.Modules.Cutting.Api.Endpoints;

/// <summary>Analytics API endpoints — /api/cutting/analytics.</summary>
public static class AnalyticsEndpoints
{
    /// <summary>Registers all analytics routes on the given <paramref name="app"/>.</summary>
    public static IEndpointRouteBuilder MapAnalyticsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/cutting/analytics")
            .RequireAuthorization("ManufacturerOnly");

        group.MapGet("/execution-metrics", GetExecutionMetrics);
        group.MapGet("/material-usage", GetMaterialUsage);
        group.MapGet("/oee", GetOEE);
        group.MapGet("/operator-metrics", GetOperatorMetrics);
        group.MapGet("/rebuild-status", GetRebuildStatus);
        group.MapPost("/rebuild", TriggerRebuild);
        group.MapGet("/dashboard-summary", GetDashboardSummary);

        return app;
    }

    private static async Task<HttpIResult> GetExecutionMetrics(
        ISender sender,
        Guid tenantId, string? machineId,
        DateOnly? from, DateOnly? to,
        int skip = 0, int take = 100,
        CancellationToken ct = default)
    {
        var query = new GetDailyExecutionMetricsQuery(
            tenantId, machineId,
            from ?? DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-30)),
            to ?? DateOnly.FromDateTime(DateTime.UtcNow),
            skip, take);
        var result = await sender.Send(query, ct).ConfigureAwait(false);
        return result.IsSuccess ? Results.Ok(result.Value) : Results.BadRequest(result.Errors);
    }

    private static async Task<HttpIResult> GetMaterialUsage(
        ISender sender,
        Guid tenantId, string? materialCode,
        DateOnly? from, DateOnly? to,
        int skip = 0, int take = 100,
        CancellationToken ct = default)
    {
        var query = new GetMaterialUsageQuery(
            tenantId, materialCode,
            from ?? DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-30)),
            to ?? DateOnly.FromDateTime(DateTime.UtcNow),
            skip, take);
        var result = await sender.Send(query, ct).ConfigureAwait(false);
        return result.IsSuccess ? Results.Ok(result.Value) : Results.BadRequest(result.Errors);
    }

    private static async Task<HttpIResult> GetOEE(
        ISender sender,
        Guid tenantId, string? machineId,
        DateTime? from, DateTime? to,
        int skip = 0, int take = 100,
        CancellationToken ct = default)
    {
        var query = new GetMachineOEEQuery(
            tenantId, machineId,
            from ?? DateTime.UtcNow.AddDays(-7),
            to ?? DateTime.UtcNow,
            MetricResolution.Hourly, skip, take);
        var result = await sender.Send(query, ct).ConfigureAwait(false);
        return result.IsSuccess ? Results.Ok(result.Value) : Results.BadRequest(result.Errors);
    }

    private static async Task<HttpIResult> GetOperatorMetrics(
        ISender sender,
        Guid tenantId,
        DateOnly? from, DateOnly? to,
        int skip = 0, int take = 100,
        CancellationToken ct = default)
    {
        var query = new GetOperatorMetricsQuery(
            tenantId,
            from ?? DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-30)),
            to ?? DateOnly.FromDateTime(DateTime.UtcNow),
            skip, take);
        var result = await sender.Send(query, ct).ConfigureAwait(false);
        return result.IsSuccess ? Results.Ok(result.Value) : Results.BadRequest(result.Errors);
    }

    private static async Task<HttpIResult> GetRebuildStatus(
        ISender sender,
        Guid tenantId, Guid jobId,
        CancellationToken ct = default)
    {
        var query = new GetRebuildJobStatusQuery(tenantId, jobId);
        var result = await sender.Send(query, ct).ConfigureAwait(false);
        if (!result.IsSuccess)
            return result.Status == ArdalisResultStatus.NotFound
                ? Results.NotFound()
                : Results.BadRequest(result.Errors);
        return Results.Ok(result.Value);
    }

    private static async Task<HttpIResult> TriggerRebuild(
        IRebuildJobRepository repo,
        Guid tenantId,
        CancellationToken ct = default)
    {
        // SEC-07: allow at most one active rebuild job per tenant.
        var active = await repo.GetActiveForTenantAsync(tenantId, ct).ConfigureAwait(false);
        if (active is not null)
            return Results.Conflict(new
            {
                message = "A rebuild job is already active for this tenant.",
                jobId = active.Id
            });

        var job = AnalyticsRebuildJob.Create(tenantId);
        await repo.AddAsync(job, ct).ConfigureAwait(false);
        await repo.SaveChangesAsync(ct).ConfigureAwait(false);

        return Results.Accepted(
            $"/api/cutting/analytics/rebuild-status?tenantId={tenantId}&jobId={job.Id}",
            new { jobId = job.Id });
    }

    private static async Task<HttpIResult> GetDashboardSummary(
        ISender sender,
        Guid tenantId,
        CancellationToken ct = default)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var monthAgo = today.AddDays(-30);

        // Fire both queries in parallel for dashboard latency.
        var execTask = sender.Send(
            new GetDailyExecutionMetricsQuery(tenantId, null, monthAgo, today, 0, 10), ct);
        var matTask = sender.Send(
            new GetMaterialUsageQuery(tenantId, null, monthAgo, today, 0, 10), ct);

        await Task.WhenAll(execTask, matTask).ConfigureAwait(false);

        return Results.Ok(new
        {
            executionMetrics = execTask.Result.Value,
            materialUsage = matTask.Result.Value
        });
    }
}
