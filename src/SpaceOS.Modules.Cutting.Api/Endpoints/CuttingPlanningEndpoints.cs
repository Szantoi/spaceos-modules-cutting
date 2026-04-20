using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SpaceOS.Modules.Cutting.Application.Commands.CompleteJob;
using SpaceOS.Modules.Cutting.Application.Commands.CreateCuttingPlan;
using SpaceOS.Modules.Cutting.Application.Commands.UpdateCuttingPlanStatus;
using SpaceOS.Modules.Cutting.Application.Queries.GetCuttingPlan;
using SpaceOS.Modules.Cutting.Domain.Interfaces;

namespace SpaceOS.Modules.Cutting.Api.Endpoints;

public static class CuttingPlanningEndpoints
{
    public static IEndpointRouteBuilder MapCuttingPlanningEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/cutting/planning")
            .RequireAuthorization("ManufacturerOnly");

        group.MapPost("/", CreateCuttingPlan);
        group.MapGet("/", GetCuttingPlans);
        group.MapGet("/{planId:guid}", GetCuttingPlan);
        group.MapPut("/{planId:guid}", UpdateCuttingPlan);
        group.MapGet("/{planId:guid}/daily/{date}", GetDailyPlan);
        group.MapPut("/jobs/{jobId:guid}/complete", CompleteJob);

        return app;
    }

    private static async Task<IResult> CreateCuttingPlan(
        CreateCuttingPlanRequest request,
        IMediator mediator,
        HttpContext httpContext,
        CancellationToken ct)
    {
        var tenantId = GetTenantId(httpContext);
        if (tenantId == Guid.Empty) return Results.Unauthorized();

        if (!DateTime.TryParse(request.PlanDate, out var rawDate))
            return Results.BadRequest("Invalid planDate format. Use yyyy-MM-dd.");

        var planDate = DateTime.SpecifyKind(rawDate.Date, DateTimeKind.Utc);
        var planDays = request.PlanDays ?? 14;
        var strategyId = request.StrategyId ?? "maxcut-v1";

        var command = new CreateCuttingPlanCommand(tenantId, planDate, planDays, strategyId);
        var result = await mediator.Send(command, ct).ConfigureAwait(false);

        if (!result.IsSuccess)
        {
            if (result.Status == Ardalis.Result.ResultStatus.Invalid)
                return Results.BadRequest(result.ValidationErrors);
            return Results.BadRequest(result.Errors);
        }

        return Results.Created($"/api/cutting/planning/{result.Value.PlanId}", result.Value);
    }

    private static async Task<IResult> GetCuttingPlans(
        ICuttingRepository repo,
        CancellationToken ct)
    {
        var plans = await repo.GetAllCuttingPlansAsync(ct).ConfigureAwait(false);
        var result = plans.Select(p => new CuttingPlanSummaryResponse(
            p.Id,
            p.PlanDate.ToString("yyyy-MM-dd"),
            p.PlanDays,
            p.Status,
            p.StrategyId));
        return Results.Ok(result);
    }

    private static async Task<IResult> GetCuttingPlan(
        Guid planId,
        IMediator mediator,
        CancellationToken ct)
    {
        var result = await mediator.Send(new GetCuttingPlanQuery(planId), ct).ConfigureAwait(false);
        return result.IsSuccess ? Results.Ok(result.Value) : Results.NotFound(result.Errors);
    }

    private static async Task<IResult> UpdateCuttingPlan(
        Guid planId,
        UpdateCuttingPlanRequest request,
        IMediator mediator,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Status))
            return Results.BadRequest("Status is required.");

        var command = new UpdateCuttingPlanStatusCommand(planId, request.Status);
        var result = await mediator.Send(command, ct).ConfigureAwait(false);

        if (!result.IsSuccess)
        {
            if (result.Status == Ardalis.Result.ResultStatus.NotFound)
                return Results.NotFound(result.Errors);
            if (result.Status == Ardalis.Result.ResultStatus.Invalid)
                return Results.BadRequest(result.ValidationErrors);
            return Results.BadRequest(result.Errors);
        }

        return Results.Ok(new { planId, status = request.Status });
    }

    private static async Task<IResult> GetDailyPlan(
        Guid planId,
        string date,
        IMediator mediator,
        CancellationToken ct)
    {
        if (!DateTime.TryParse(date, out var parsedDate))
            return Results.BadRequest("Invalid date format. Use yyyy-MM-dd.");

        var result = await mediator.Send(new GetCuttingPlanQuery(planId), ct).ConfigureAwait(false);
        if (!result.IsSuccess)
            return Results.NotFound(result.Errors);

        var targetDate = parsedDate.Date.ToString("yyyy-MM-dd");
        var daily = result.Value.DailyPlans.FirstOrDefault(d => d.Date == targetDate);
        if (daily is null)
            return Results.NotFound($"No daily plan for date {targetDate} in plan {planId}.");

        return Results.Ok(daily);
    }

    private static async Task<IResult> CompleteJob(
        Guid jobId,
        CompleteJobRequest request,
        IMediator mediator,
        HttpContext httpContext,
        CancellationToken ct)
    {
        var tenantId = GetTenantId(httpContext);
        if (tenantId == Guid.Empty) return Results.Unauthorized();

        var command = new CompleteJobCommand(
            JobId: jobId,
            TenantId: tenantId,
            CuttingSheetId: request.CuttingSheetId ?? Guid.Empty,
            YieldPct: request.YieldPct,
            WasteM2: request.WasteM2);

        var result = await mediator.Send(command, ct).ConfigureAwait(false);

        if (!result.IsSuccess)
        {
            if (result.Status == Ardalis.Result.ResultStatus.NotFound)
                return Results.NotFound(result.Errors);
            if (result.Status == Ardalis.Result.ResultStatus.Invalid)
                return Results.BadRequest(result.ValidationErrors);
            return Results.BadRequest(result.Errors);
        }

        return Results.Ok(new { jobId, status = "Cut" });
    }

    private static Guid GetTenantId(HttpContext ctx)
    {
        var claim = ctx.User?.FindFirst("tid")?.Value;
        return Guid.TryParse(claim, out var id) ? id : Guid.Empty;
    }
}

public sealed record CreateCuttingPlanRequest(string PlanDate, int? PlanDays, string? StrategyId);
public sealed record UpdateCuttingPlanRequest(string? Status);
public sealed record CompleteJobRequest(Guid? CuttingSheetId, decimal YieldPct, decimal WasteM2);
