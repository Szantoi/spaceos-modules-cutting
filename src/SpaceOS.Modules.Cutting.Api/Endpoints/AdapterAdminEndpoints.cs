using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SpaceOS.Modules.Cutting.Application.Adapters.Commands;
using SpaceOS.Modules.Cutting.Application.Adapters.Queries;

namespace SpaceOS.Modules.Cutting.Api.Endpoints;

/// <summary>
/// Admin API for managing per-tenant cutting adapter configuration and health.
/// All endpoints require the ManufacturerOnly authorization policy.
/// </summary>
public static class AdapterAdminEndpoints
{
    public static IEndpointRouteBuilder MapAdapterAdminEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/cutting/adapters")
            .RequireAuthorization("ManufacturerOnly");

        group.MapPost("/config", CreateOrUpdateConfig);
        group.MapGet("/config", GetConfig);
        group.MapPost("/config/test", TestAdapter);
        group.MapGet("/health", GetAdapterHealth);

        return app;
    }

    private static async Task<IResult> CreateOrUpdateConfig(
        ConfigureAdapterRequest request,
        IMediator mediator,
        HttpContext httpContext,
        CancellationToken ct)
    {
        var tenantId = GetTenantId(httpContext);
        if (tenantId == Guid.Empty)
            return Results.Unauthorized();

        var actorId = GetActorId(httpContext);

        var command = new ConfigureAdapterCommand(
            tenantId,
            request.AdapterName,
            request.TransportName,
            request.ConfigJson ?? string.Empty,
            request.ConfigSchemaVersion,
            request.ExpectedVersion,
            request.ChangeReason,
            actorId);

        var result = await mediator.Send(command, ct).ConfigureAwait(false);

        return result.Status switch
        {
            Ardalis.Result.ResultStatus.Ok => Results.Ok(),
            Ardalis.Result.ResultStatus.Invalid => Results.BadRequest(result.ValidationErrors),
            Ardalis.Result.ResultStatus.Conflict => Results.Conflict(result.Errors),
            _ => Results.BadRequest(result.Errors)
        };
    }

    private static async Task<IResult> GetConfig(
        IMediator mediator,
        HttpContext httpContext,
        CancellationToken ct)
    {
        var tenantId = GetTenantId(httpContext);
        if (tenantId == Guid.Empty)
            return Results.Unauthorized();

        var query = new GetAdapterConfigQuery(tenantId);
        var result = await mediator.Send(query, ct).ConfigureAwait(false);

        return result.Status switch
        {
            Ardalis.Result.ResultStatus.Ok => Results.Ok(result.Value),
            Ardalis.Result.ResultStatus.NotFound => Results.NotFound(),
            _ => Results.BadRequest(result.Errors)
        };
    }

    private static async Task<IResult> TestAdapter(
        IMediator mediator,
        HttpContext httpContext,
        CancellationToken ct)
    {
        var tenantId = GetTenantId(httpContext);
        if (tenantId == Guid.Empty)
            return Results.Unauthorized();

        var command = new TestAdapterCommand(tenantId);
        var result = await mediator.Send(command, ct).ConfigureAwait(false);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.BadRequest(result.Errors);
    }

    private static async Task<IResult> GetAdapterHealth(
        IMediator mediator,
        HttpContext httpContext,
        CancellationToken ct)
    {
        var tenantId = GetTenantId(httpContext);
        if (tenantId == Guid.Empty)
            return Results.Unauthorized();

        var query = new GetAdapterHealthQuery(tenantId);
        var result = await mediator.Send(query, ct).ConfigureAwait(false);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.NotFound(result.Errors);
    }

    private static Guid GetTenantId(HttpContext ctx)
    {
        var claim = ctx.User?.FindFirst("tid")?.Value;
        return Guid.TryParse(claim, out var id) ? id : Guid.Empty;
    }

    private static Guid GetActorId(HttpContext ctx)
    {
        // Use sub claim as actor; fall back to tid if sub is absent
        var sub = ctx.User?.FindFirst("sub")?.Value
                  ?? ctx.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(sub, out var id) ? id : GetTenantId(ctx);
    }
}

/// <summary>Request body for the POST /api/cutting/adapters/config endpoint.</summary>
public sealed record ConfigureAdapterRequest(
    string AdapterName,
    string TransportName,
    string? ConfigJson,
    short ConfigSchemaVersion,
    int ExpectedVersion,
    string? ChangeReason);
