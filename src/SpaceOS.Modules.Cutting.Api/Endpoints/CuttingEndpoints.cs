using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using SpaceOS.Modules.Cutting.Application.Commands.SubmitCuttingSheet;
using SpaceOS.Modules.Cutting.Application.Commands.CreateDailyCuttingPlan;
using SpaceOS.Modules.Cutting.Application.Queries.GetNestingResult;
using SpaceOS.Modules.Cutting.Application.Queries.GetExecutionStatus;
using SpaceOS.Modules.Cutting.Application.Queries.GetWasteReport;
using SpaceOS.Modules.Cutting.Application.Queries.GetDailyCuttingPlan;
using SpaceOS.Modules.Cutting.Domain.Interfaces;

namespace SpaceOS.Modules.Cutting.Api.Endpoints;

public static class CuttingEndpoints
{
    public static IEndpointRouteBuilder MapCuttingEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/cutting")
            .RequireAuthorization("ManufacturerOnly");

        group.MapPost("/sheets", SubmitCuttingSheet);
        group.MapGet("/sheets/{id:guid}/nesting", GetNestingResult);
        group.MapGet("/sheets/{id:guid}/status", GetExecutionStatus);
        group.MapGet("/waste", GetWasteReport);
        group.MapGet("/plans", GetAllCuttingPlans);
        group.MapPost("/plans", CreateDailyCuttingPlan);
        group.MapGet("/plans/{date}", GetDailyCuttingPlan);

        return app;
    }

    private static async Task<IResult> SubmitCuttingSheet(
        SubmitSheetRequest request,
        IMediator mediator,
        HttpContext httpContext,
        CancellationToken ct)
    {
        var tenantId = GetTenantId(httpContext);
        if (tenantId == Guid.Empty) return Results.Unauthorized();

        var lines = request.Lines.Select(l => new CuttingLineInput(l.PartName, l.MaterialType, l.WidthMm, l.HeightMm, l.ThicknessMm, l.Quantity, l.Notes)).ToList();
        var command = new SubmitCuttingSheetCommand(tenantId, request.OrderReference, lines);
        var result = await mediator.Send(command, ct).ConfigureAwait(false);
        return result.IsSuccess ? Results.Ok(new { Id = result.Value }) : Results.BadRequest(result.Errors);
    }

    private static async Task<IResult> GetNestingResult(Guid id, IMediator mediator, CancellationToken ct)
    {
        var result = await mediator.Send(new GetNestingResultQuery(id), ct).ConfigureAwait(false);
        return result.IsSuccess ? Results.Ok(result.Value) : Results.NotFound(result.Errors);
    }

    private static async Task<IResult> GetExecutionStatus(Guid id, IMediator mediator, CancellationToken ct)
    {
        var result = await mediator.Send(new GetExecutionStatusQuery(id), ct).ConfigureAwait(false);
        return result.IsSuccess ? Results.Ok(result.Value) : Results.NotFound(result.Errors);
    }

    private static async Task<IResult> GetWasteReport(
        DateTime? from, DateTime? to,
        IMediator mediator, CancellationToken ct)
    {
        var query = new GetWasteReportQuery(from ?? DateTime.UtcNow.AddMonths(-1), to ?? DateTime.UtcNow);
        var result = await mediator.Send(query, ct).ConfigureAwait(false);
        return result.IsSuccess ? Results.Ok(result.Value) : Results.BadRequest(result.Errors);
    }

    private static async Task<IResult> GetAllCuttingPlans(
        ICuttingRepository repo,
        CancellationToken ct)
    {
        var plans = await repo.GetAllDailyCuttingPlansAsync(ct).ConfigureAwait(false);
        var result = plans.Select(p => new
        {
            id = p.Id,
            name = p.Name,
            date = p.PlanDate.ToString("yyyy-MM-dd"),
            status = p.Status.ToString()
        });
        return Results.Ok(result);
    }

    private static async Task<IResult> CreateDailyCuttingPlan(
        CreatePlanRequest request,
        IMediator mediator,
        HttpContext httpContext,
        CancellationToken ct)
    {
        var tenantId = GetTenantId(httpContext);
        if (tenantId == Guid.Empty) return Results.Unauthorized();

        if (!DateTime.TryParse(request.Date, out var rawDate))
            return Results.BadRequest("Invalid date format. Use yyyy-MM-dd.");
        // Npgsql requires DateTimeKind.Utc for 'timestamp with time zone' columns.
        // DateTime.TryParse returns Kind=Unspecified which causes a runtime exception on SaveChanges.
        var planDate = DateTime.SpecifyKind(rawDate.Date, DateTimeKind.Utc);

        var batches = (request.Batches ?? []).Select(b => new CuttingBatchInput(b.MaterialType, b.ThicknessMm, b.SheetIds)).ToList();
        var command = new CreateDailyCuttingPlanCommand(tenantId, request.Name, planDate, batches);

        try
        {
            var result = await mediator.Send(command, ct).ConfigureAwait(false);
            if (!result.IsSuccess)
                return Results.BadRequest(result.Errors);

            var responseObj = new
            {
                id = result.Value,
                name = request.Name,
                date = planDate.ToString("yyyy-MM-dd"),
                status = "Draft"
            };
            return Results.Created($"/api/cutting/plans/{planDate:yyyy-MM-dd}", responseObj);
        }
        catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("23505") == true
                                        || ex.InnerException?.Message.Contains("duplicate key") == true)
        {
            return Results.Conflict(new { error = $"A cutting plan for {planDate:yyyy-MM-dd} already exists." });
        }
    }

    private static async Task<IResult> GetDailyCuttingPlan(
        string date, IMediator mediator, CancellationToken ct)
    {
        if (!DateTime.TryParse(date, out var planDate))
            return Results.BadRequest("Invalid date format. Use yyyy-MM-dd.");
        var result = await mediator.Send(new GetDailyCuttingPlanQuery(planDate), ct).ConfigureAwait(false);
        return result.IsSuccess ? Results.Ok(result.Value) : Results.NotFound(result.Errors);
    }

    private static Guid GetTenantId(HttpContext ctx)
    {
        var claim = ctx.User?.FindFirst("tid")?.Value;
        return Guid.TryParse(claim, out var id) ? id : Guid.Empty;
    }
}

public sealed record SubmitSheetLineRequest(string PartName, string MaterialType, decimal WidthMm, decimal HeightMm, decimal ThicknessMm, int Quantity, string? Notes);
public sealed record SubmitSheetRequest(string OrderReference, IReadOnlyList<SubmitSheetLineRequest> Lines);
public sealed record CreateBatchRequest(string MaterialType, decimal ThicknessMm, IReadOnlyList<Guid> SheetIds);
public sealed record CreatePlanRequest(string Name, string Date, IReadOnlyList<CreateBatchRequest>? Batches = null);
