using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SpaceOS.Modules.Cutting.Application.Commands.AssignBatch;
using SpaceOS.Modules.Cutting.Application.Commands.CloseCuttingPlan;
using SpaceOS.Modules.Cutting.Application.Commands.ReservePanels;
using SpaceOS.Modules.Cutting.Application.Commands.CompleteJob;
using SpaceOS.Modules.Cutting.Application.Commands.CreateCuttingPlan;
using SpaceOS.Modules.Cutting.Application.Commands.CreatePriorityProfile;
using SpaceOS.Modules.Cutting.Application.Commands.FreezeCuttingPlan;
using SpaceOS.Modules.Cutting.Application.Commands.PublishCuttingPlan;
using SpaceOS.Modules.Cutting.Application.Commands.UpdateCuttingPlanStatus;
using SpaceOS.Modules.Cutting.Application.Queries.GetCuttingPlan;
using SpaceOS.Modules.Cutting.Application.Queries.GetPriorityProfiles;
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
        group.MapPost("/{planId:guid}/publish", PublishCuttingPlan);
        group.MapPost("/{planId:guid}/freeze", FreezeCuttingPlan);
        group.MapPost("/{planId:guid}/close", CloseCuttingPlan);
        group.MapPost("/{planId:guid}/reserve-panels", ReservePanels);

        var profileGroup = app.MapGroup("/api/cutting/priority-profiles")
            .RequireAuthorization("ManufacturerOnly");
        profileGroup.MapGet("/", GetPriorityProfiles);
        profileGroup.MapPost("/", CreatePriorityProfile);

        // Plans endpoints — POST /cutting/api/plans/{date}/assign-batch
        var plansGroup = app.MapGroup("/cutting/api/plans")
            .RequireAuthorization();
        plansGroup.MapPost("/{date}/assign-batch", AssignBatch)
            .RequireAuthorization(policy => policy.RequireRole("machine_operator", "production_manager"));

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
            p.Status.ToString(),
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

    private static async Task<IResult> ReservePanels(
        Guid planId,
        IMediator mediator,
        HttpContext httpContext,
        CancellationToken ct)
    {
        var tenantId = GetTenantId(httpContext);
        if (tenantId == Guid.Empty) return Results.Unauthorized();

        var result = await mediator.Send(new ReservePanelsCommand(planId, tenantId), ct).ConfigureAwait(false);
        if (!result.IsSuccess)
        {
            if (result.Status == Ardalis.Result.ResultStatus.NotFound) return Results.NotFound(result.Errors);
            return Results.BadRequest(result.Errors);
        }
        return Results.Ok(new { planId, reservedCount = result.Value });
    }

    private static async Task<IResult> PublishCuttingPlan(
        Guid planId,
        PublishCuttingPlanRequest request,
        IMediator mediator,
        CancellationToken ct)
    {
        var result = await mediator.Send(new PublishCuttingPlanCommand(planId, request.ProfileSnapshotId), ct).ConfigureAwait(false);
        if (!result.IsSuccess)
        {
            if (result.Status == Ardalis.Result.ResultStatus.NotFound) return Results.NotFound(result.Errors);
            if (result.Status == Ardalis.Result.ResultStatus.Invalid) return Results.BadRequest(result.ValidationErrors);
            return Results.BadRequest(result.Errors);
        }
        return Results.Ok(new { planId, status = "Published" });
    }

    private static async Task<IResult> FreezeCuttingPlan(
        Guid planId,
        IMediator mediator,
        CancellationToken ct)
    {
        var result = await mediator.Send(new FreezeCuttingPlanCommand(planId), ct).ConfigureAwait(false);
        if (!result.IsSuccess)
        {
            if (result.Status == Ardalis.Result.ResultStatus.NotFound) return Results.NotFound(result.Errors);
            if (result.Status == Ardalis.Result.ResultStatus.Invalid) return Results.BadRequest(result.ValidationErrors);
            return Results.BadRequest(result.Errors);
        }
        return Results.Ok(new { planId, status = "Frozen" });
    }

    private static async Task<IResult> CloseCuttingPlan(
        Guid planId,
        IMediator mediator,
        CancellationToken ct)
    {
        var result = await mediator.Send(new CloseCuttingPlanCommand(planId), ct).ConfigureAwait(false);
        if (!result.IsSuccess)
        {
            if (result.Status == Ardalis.Result.ResultStatus.NotFound) return Results.NotFound(result.Errors);
            if (result.Status == Ardalis.Result.ResultStatus.Invalid) return Results.BadRequest(result.ValidationErrors);
            return Results.BadRequest(result.Errors);
        }
        return Results.Ok(new { planId, status = "Closed" });
    }

    private static async Task<IResult> GetPriorityProfiles(
        IMediator mediator,
        HttpContext httpContext,
        CancellationToken ct)
    {
        var tenantId = GetTenantId(httpContext);
        if (tenantId == Guid.Empty) return Results.Unauthorized();

        var result = await mediator.Send(new GetPriorityProfilesQuery(tenantId), ct).ConfigureAwait(false);
        return Results.Ok(result.Value);
    }

    private static async Task<IResult> CreatePriorityProfile(
        CreatePriorityProfileRequest request,
        IMediator mediator,
        HttpContext httpContext,
        CancellationToken ct)
    {
        var tenantId = GetTenantId(httpContext);
        if (tenantId == Guid.Empty) return Results.Unauthorized();

        var command = new CreatePriorityProfileCommand(
            tenantId,
            request.Name,
            request.CapacityModelId,
            request.ReworkPolicyId,
            request.PlanningStrategyId,
            request.IsDefault);

        var result = await mediator.Send(command, ct).ConfigureAwait(false);

        if (!result.IsSuccess)
        {
            if (result.Status == Ardalis.Result.ResultStatus.Invalid)
                return Results.BadRequest(result.ValidationErrors);
            return Results.BadRequest(result.Errors);
        }

        return Results.Created($"/api/cutting/priority-profiles/{result.Value}", new { id = result.Value });
    }

    private static async Task<IResult> AssignBatch(
        string date,
        AssignBatchRequest request,
        IMediator mediator,
        HttpContext httpContext,
        CancellationToken ct)
    {
        var tenantId = GetTenantId(httpContext);
        if (tenantId == Guid.Empty) return Results.Unauthorized();

        if (!DateOnly.TryParse(date, out var planDate))
            return Results.BadRequest("Invalid date format. Use yyyy-MM-dd.");

        if (request.BatchId == Guid.Empty)
            return Results.BadRequest("BatchId is required.");
        if (request.MachineId == Guid.Empty)
            return Results.BadRequest("MachineId is required.");
        if (request.OperatorId == Guid.Empty)
            return Results.BadRequest("OperatorId is required.");

        var command = new AssignBatchCommand(
            TenantId: tenantId,
            PlanDate: planDate,
            BatchId: request.BatchId,
            MachineId: request.MachineId,
            OperatorId: request.OperatorId,
            Priority: request.Priority,
            StartTime: request.StartTime);

        var result = await mediator.Send(command, ct).ConfigureAwait(false);

        return result.Status switch
        {
            Ardalis.Result.ResultStatus.Ok => Results.Ok(
                new { executionId = result.Value.ExecutionId, status = result.Value.Status }),
            Ardalis.Result.ResultStatus.Conflict => Results.Conflict(
                new { error = string.Join("; ", result.Errors) }),
            Ardalis.Result.ResultStatus.Invalid => Results.BadRequest(result.ValidationErrors),
            Ardalis.Result.ResultStatus.NotFound => Results.NotFound(result.Errors),
            _ => Results.StatusCode(500)
        };
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
public sealed record CreatePriorityProfileRequest(
    string Name,
    string CapacityModelId,
    string ReworkPolicyId,
    string PlanningStrategyId,
    bool IsDefault = false);
public sealed record PublishCuttingPlanRequest(Guid ProfileSnapshotId);
public sealed record AssignBatchRequest(
    Guid BatchId,
    Guid MachineId,
    Guid OperatorId,
    int Priority,
    DateTime StartTime);
