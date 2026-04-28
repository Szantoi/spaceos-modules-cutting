using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SpaceOS.Modules.Cutting.Execution.Application.Commands.CancelExecution;
using SpaceOS.Modules.Cutting.Execution.Application.Commands.CompleteExecution;
using SpaceOS.Modules.Cutting.Execution.Application.Commands.EvaluateMilestones;
using SpaceOS.Modules.Cutting.Execution.Application.Commands.RecordOffcut;
using SpaceOS.Modules.Cutting.Execution.Application.Commands.RecordProgress;
using SpaceOS.Modules.Cutting.Execution.Application.Commands.ScheduleExecution;
using SpaceOS.Modules.Cutting.Execution.Application.Commands.StartExecution;
using SpaceOS.Modules.Cutting.Execution.Application.Commands.WithdrawWorkerConsent;
using SpaceOS.Modules.Cutting.Execution.Application.Queries.GetCompletionProof;
using SpaceOS.Modules.Cutting.Execution.Application.Queries.GetExecution;
using SpaceOS.Modules.Cutting.Execution.Application.Queries.GetMilestones;
using SpaceOS.Modules.Cutting.Execution.Application.Queries.GetProgress;
using SpaceOS.Modules.Cutting.Execution.Application.Queries.GetWorkerConsent;
using SpaceOS.Modules.Cutting.Execution.Application.Queries.ListExecutions;
using SpaceOS.Modules.Cutting.Execution.Domain.Enums;

namespace SpaceOS.Modules.Cutting.Api.Endpoints;

/// <summary>Minimal API endpoints for the Phase 4 Execution aggregate.</summary>
public static class CuttingExecutionEndpoints
{
    public static IEndpointRouteBuilder MapCuttingExecutionEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/cutting/executions")
            .RequireAuthorization("ManufacturerOnly");

        // Commands
        group.MapPost("/", ScheduleExecution);
        group.MapPost("/{id:guid}/start", StartExecution);
        group.MapPost("/{id:guid}/progress", RecordProgress);
        group.MapPost("/{id:guid}/offcut", RecordOffcut);
        group.MapPost("/{id:guid}/complete", CompleteExecution);
        group.MapPost("/{id:guid}/cancel", CancelExecution);
        group.MapPost("/{id:guid}/milestones/evaluate", EvaluateMilestones);
        group.MapPost("/{id:guid}/consent/withdraw", WithdrawWorkerConsent);

        // Queries
        group.MapGet("/", ListExecutions);
        group.MapGet("/{id:guid}", GetExecution);
        group.MapGet("/{id:guid}/progress", GetProgress);
        group.MapGet("/{id:guid}/milestones", GetMilestones);
        group.MapGet("/{id:guid}/proof", GetCompletionProof);
        group.MapGet("/{id:guid}/consent", GetWorkerConsent);

        return app;
    }

    // ── Commands ─────────────────────────────────────────────────────────────────

    private static async Task<IResult> ScheduleExecution(
        ScheduleExecutionRequest request,
        IMediator mediator,
        HttpContext ctx,
        CancellationToken ct)
    {
        var tenantId = GetTenantId(ctx);
        if (tenantId == Guid.Empty) return TypedResults.Unauthorized();

        var command = new ScheduleExecutionCommand(
            tenantId,
            request.SheetId,
            request.WorkerId,
            request.EnrollmentId,
            request.MachineId,
            request.ScheduleStart,
            request.ScheduleEnd,
            request.TotalPanels);

        var result = await mediator.Send(command, ct).ConfigureAwait(false);
        return result.Status switch
        {
            Ardalis.Result.ResultStatus.Ok => TypedResults.Created($"/api/cutting/executions/{result.Value}", new { Id = result.Value }),
            Ardalis.Result.ResultStatus.Invalid => TypedResults.UnprocessableEntity(result.ValidationErrors),
            _ => TypedResults.StatusCode(500)
        };
    }

    private static async Task<IResult> StartExecution(
        Guid id,
        StartExecutionRequest request,
        IMediator mediator,
        HttpContext ctx,
        CancellationToken ct)
    {
        var tenantId = GetTenantId(ctx);
        if (tenantId == Guid.Empty) return TypedResults.Unauthorized();

        var command = new StartExecutionCommand(id, tenantId, request.WorkerId, request.BadgeHmacBase64, request.HmacKeyVersion);
        var result = await mediator.Send(command, ct).ConfigureAwait(false);
        return MapResult(result);
    }

    private static async Task<IResult> RecordProgress(
        Guid id,
        RecordProgressRequest request,
        IMediator mediator,
        HttpContext ctx,
        CancellationToken ct)
    {
        var tenantId = GetTenantId(ctx);
        if (tenantId == Guid.Empty) return TypedResults.Unauthorized();

        var command = new RecordProgressCommand(
            id, tenantId, request.EventId, request.Kind, request.Panel,
            request.OccurredAt, request.EventHmacBase64, request.HmacKeyVersion);
        var result = await mediator.Send(command, ct).ConfigureAwait(false);
        return MapResult(result);
    }

    private static async Task<IResult> RecordOffcut(
        Guid id,
        RecordOffcutRequest request,
        IMediator mediator,
        HttpContext ctx,
        CancellationToken ct)
    {
        var tenantId = GetTenantId(ctx);
        if (tenantId == Guid.Empty) return TypedResults.Unauthorized();

        var command = new RecordOffcutCommand(id, tenantId, request.MaterialId, request.WidthMm, request.HeightMm);
        var result = await mediator.Send(command, ct).ConfigureAwait(false);
        return MapResult(result);
    }

    private static async Task<IResult> CompleteExecution(
        Guid id,
        CompleteExecutionRequest request,
        IMediator mediator,
        HttpContext ctx,
        CancellationToken ct)
    {
        var tenantId = GetTenantId(ctx);
        if (tenantId == Guid.Empty) return TypedResults.Unauthorized();

        var command = new CompleteExecutionCommand(
            id, tenantId, request.ProofLevel, request.ProofHash,
            request.Signature, request.BlobRef, request.EncryptedWith);
        var result = await mediator.Send(command, ct).ConfigureAwait(false);
        return MapResult(result);
    }

    private static async Task<IResult> CancelExecution(
        Guid id,
        CancelExecutionRequest request,
        IMediator mediator,
        HttpContext ctx,
        CancellationToken ct)
    {
        var tenantId = GetTenantId(ctx);
        if (tenantId == Guid.Empty) return TypedResults.Unauthorized();

        var command = new CancelExecutionCommand(id, tenantId, request.Reason);
        var result = await mediator.Send(command, ct).ConfigureAwait(false);
        return MapResult(result);
    }

    private static async Task<IResult> EvaluateMilestones(
        Guid id,
        IMediator mediator,
        HttpContext ctx,
        CancellationToken ct)
    {
        var tenantId = GetTenantId(ctx);
        if (tenantId == Guid.Empty) return TypedResults.Unauthorized();

        var command = new EvaluateMilestonesCommand(id, tenantId);
        var result = await mediator.Send(command, ct).ConfigureAwait(false);
        return MapResult(result);
    }

    private static async Task<IResult> WithdrawWorkerConsent(
        Guid id,
        WithdrawWorkerConsentRequest request,
        IMediator mediator,
        HttpContext ctx,
        CancellationToken ct)
    {
        var tenantId = GetTenantId(ctx);
        if (tenantId == Guid.Empty) return TypedResults.Unauthorized();

        var command = new WithdrawWorkerConsentCommand(tenantId, request.WorkerId, request.Scope);
        var result = await mediator.Send(command, ct).ConfigureAwait(false);
        return result.Status switch
        {
            Ardalis.Result.ResultStatus.Ok => TypedResults.Ok(new { WithdrawalId = result.Value }),
            Ardalis.Result.ResultStatus.Invalid => TypedResults.UnprocessableEntity(result.ValidationErrors),
            _ => TypedResults.StatusCode(500)
        };
    }

    // ── Queries ──────────────────────────────────────────────────────────────────

    private static async Task<IResult> ListExecutions(
        IMediator mediator,
        HttpContext ctx,
        CancellationToken ct)
    {
        var tenantId = GetTenantId(ctx);
        if (tenantId == Guid.Empty) return TypedResults.Unauthorized();

        var result = await mediator.Send(new ListExecutionsQuery(tenantId), ct).ConfigureAwait(false);
        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : TypedResults.StatusCode(500);
    }

    private static async Task<IResult> GetExecution(
        Guid id,
        IMediator mediator,
        HttpContext ctx,
        CancellationToken ct)
    {
        var tenantId = GetTenantId(ctx);
        if (tenantId == Guid.Empty) return TypedResults.Unauthorized();

        var result = await mediator.Send(new GetExecutionQuery(id, tenantId), ct).ConfigureAwait(false);
        return result.Status switch
        {
            Ardalis.Result.ResultStatus.Ok => TypedResults.Ok(result.Value),
            Ardalis.Result.ResultStatus.NotFound => TypedResults.NotFound(),
            _ => TypedResults.StatusCode(500)
        };
    }

    private static async Task<IResult> GetProgress(
        Guid id,
        IMediator mediator,
        HttpContext ctx,
        CancellationToken ct)
    {
        var tenantId = GetTenantId(ctx);
        if (tenantId == Guid.Empty) return TypedResults.Unauthorized();

        var result = await mediator.Send(new GetProgressQuery(id, tenantId), ct).ConfigureAwait(false);
        return result.Status switch
        {
            Ardalis.Result.ResultStatus.Ok => TypedResults.Ok(result.Value),
            Ardalis.Result.ResultStatus.NotFound => TypedResults.NotFound(),
            _ => TypedResults.StatusCode(500)
        };
    }

    private static async Task<IResult> GetMilestones(
        Guid id,
        IMediator mediator,
        HttpContext ctx,
        CancellationToken ct)
    {
        var tenantId = GetTenantId(ctx);
        if (tenantId == Guid.Empty) return TypedResults.Unauthorized();

        var result = await mediator.Send(new GetMilestonesQuery(id, tenantId), ct).ConfigureAwait(false);
        return result.Status switch
        {
            Ardalis.Result.ResultStatus.Ok => TypedResults.Ok(result.Value),
            Ardalis.Result.ResultStatus.NotFound => TypedResults.NotFound(),
            _ => TypedResults.StatusCode(500)
        };
    }

    private static async Task<IResult> GetCompletionProof(
        Guid id,
        IMediator mediator,
        HttpContext ctx,
        CancellationToken ct)
    {
        var tenantId = GetTenantId(ctx);
        if (tenantId == Guid.Empty) return TypedResults.Unauthorized();

        var result = await mediator.Send(new GetCompletionProofQuery(id, tenantId), ct).ConfigureAwait(false);
        return result.Status switch
        {
            Ardalis.Result.ResultStatus.Ok => TypedResults.Ok(result.Value),
            Ardalis.Result.ResultStatus.NotFound => TypedResults.NotFound(),
            _ => TypedResults.StatusCode(500)
        };
    }

    private static async Task<IResult> GetWorkerConsent(
        Guid id,
        IMediator mediator,
        HttpContext ctx,
        CancellationToken ct)
    {
        var tenantId = GetTenantId(ctx);
        if (tenantId == Guid.Empty) return TypedResults.Unauthorized();

        // WorkerId comes from the execution id route param in this context — the spec passes
        // WorkerId explicitly; here we use the route id as executionId and read WorkerId from query.
        var workerIdStr = ctx.Request.Query["workerId"].FirstOrDefault();
        if (!Guid.TryParse(workerIdStr, out var workerId))
            return TypedResults.BadRequest(new { error = "workerId query param is required." });

        var result = await mediator.Send(new GetWorkerConsentQuery(tenantId, workerId), ct).ConfigureAwait(false);
        return result.Status switch
        {
            Ardalis.Result.ResultStatus.Ok => TypedResults.Ok(result.Value),
            Ardalis.Result.ResultStatus.NotFound => TypedResults.NotFound(),
            _ => TypedResults.StatusCode(500)
        };
    }

    // ── Helpers ──────────────────────────────────────────────────────────────────

    private static Guid GetTenantId(HttpContext ctx)
    {
        var claim = ctx.User?.FindFirst("tid")?.Value ?? ctx.User?.FindFirst("tenant_id")?.Value;
        return Guid.TryParse(claim, out var id) ? id : Guid.Empty;
    }

    private static IResult MapResult(Ardalis.Result.Result result)
        => result.Status switch
        {
            Ardalis.Result.ResultStatus.Ok => TypedResults.Ok(),
            Ardalis.Result.ResultStatus.NotFound => TypedResults.NotFound(),
            Ardalis.Result.ResultStatus.Invalid => TypedResults.UnprocessableEntity(result.ValidationErrors),
            Ardalis.Result.ResultStatus.Conflict => TypedResults.Conflict(),
            _ => TypedResults.StatusCode(500)
        };

    // ── Request DTOs ─────────────────────────────────────────────────────────────

    internal sealed record ScheduleExecutionRequest(
        Guid SheetId,
        Guid WorkerId,
        Guid EnrollmentId,
        string MachineId,
        DateTime ScheduleStart,
        DateTime ScheduleEnd,
        int TotalPanels);

    internal sealed record StartExecutionRequest(
        Guid WorkerId,
        string BadgeHmacBase64,
        string HmacKeyVersion);

    internal sealed record RecordProgressRequest(
        Guid EventId,
        ProgressEventKind Kind,
        int? Panel,
        DateTime OccurredAt,
        string EventHmacBase64,
        string HmacKeyVersion);

    internal sealed record RecordOffcutRequest(
        Guid MaterialId,
        decimal WidthMm,
        decimal HeightMm);

    internal sealed record CompleteExecutionRequest(
        ProofLevel ProofLevel,
        string ProofHash,
        string? Signature,
        string? BlobRef,
        string? EncryptedWith);

    internal sealed record CancelExecutionRequest(CancelReason Reason);

    internal sealed record WithdrawWorkerConsentRequest(Guid WorkerId, ConsentScope Scope);
}
