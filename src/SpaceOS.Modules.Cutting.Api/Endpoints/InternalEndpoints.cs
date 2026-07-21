using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SpaceOS.Modules.Cutting.Application.Commands.IngestOrder;
using SpaceOS.Modules.Cutting.Domain.Enums;
using SpaceOS.Modules.Cutting.Domain.Interfaces;
using SpaceOS.Modules.Cutting.Infrastructure.Persistence;
using System.Security.Cryptography;
using System.Text;

namespace SpaceOS.Modules.Cutting.Api.Endpoints;

public static class InternalEndpoints
{
    private const string InternalHeader = "X-SpaceOS-Internal";
    private const string InternalSecretConfigKey = "SpaceOS:InternalSecret";
    private const string InternalSecretEnvKey = "SPACEOS_INTERNAL_SECRET";
    private const string AllowlistEnvKey = "TEST_TENANT_ALLOWLIST";

    public static IEndpointRouteBuilder MapInternalEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapDelete("/internal/cutting-sheets/by-tenant/{tenantId}", DeleteByTenant)
            .AllowAnonymous();

        app.MapPost("/internal/ingest-order", IngestOrder)
            .AllowAnonymous();

        return app;
    }

    private static async Task<IResult> DeleteByTenant(
        string tenantId,
        string? confirm,
        HttpRequest request,
        IConfiguration configuration,
        ICuttingRepository repo,
        CuttingDbContext dbContext,
        ILogger<Program> logger,
        CancellationToken ct)
    {
        var authenticationFailure = AuthenticateInternalRequest(request, configuration);
        if (authenticationFailure is not null)
            return authenticationFailure;

        // 2. ?confirm=true required
        if (!string.Equals(confirm, "true", StringComparison.OrdinalIgnoreCase))
            return Results.BadRequest(new { error = "confirm=true query parameter is required." });

        // 3. GUID format validation
        if (!Guid.TryParse(tenantId, out var tenantGuid))
            return Results.BadRequest(new { error = "tenantId must be a valid GUID." });

        // 4. Allowlist check
        var allowlist = Environment.GetEnvironmentVariable(AllowlistEnvKey) ?? string.Empty;
        var allowed = allowlist
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(s => Guid.TryParse(s, out var g) ? g : Guid.Empty)
            .Where(g => g != Guid.Empty)
            .ToHashSet();

        if (!allowed.Contains(tenantGuid))
        {
            logger.LogWarning(
                "DeleteByTenant rejected: tenant {TenantId} not in TEST_TENANT_ALLOWLIST", tenantGuid);
            return Results.StatusCode(403);
        }

        // Pin a single connection so set_config and DeleteByTenantAsync share the same physical connection.
        // Without this, pool may hand out different connections and the GUC is lost.
        if (dbContext.Database.IsRelational())
        {
            await dbContext.Database.OpenConnectionAsync(ct).ConfigureAwait(false);
            var tenantIdStr = tenantGuid.ToString();
            await dbContext.Database.ExecuteSqlAsync(
                $"SELECT set_config('app.current_tenant_id', {tenantIdStr}, false)", ct)
                .ConfigureAwait(false);
        }

        (int sheets, int plans) counts;
        try
        {
            counts = await repo.DeleteByTenantAsync(tenantGuid, ct).ConfigureAwait(false);
        }
        finally
        {
            if (dbContext.Database.IsRelational())
                await dbContext.Database.CloseConnectionAsync().ConfigureAwait(false);
        }

        return Results.Ok(new
        {
            tenantId = tenantGuid,
            deletedCounts = new { cuttingSheets = counts.sheets, dailyCuttingPlans = counts.plans }
        });
    }

    private static async Task<IResult> IngestOrder(
        IngestOrderDto dto,
        HttpRequest request,
        IConfiguration configuration,
        IMediator mediator,
        CancellationToken ct)
    {
        var authenticationFailure = AuthenticateInternalRequest(request, configuration);
        if (authenticationFailure is not null)
            return authenticationFailure;

        if (dto.Items is null || dto.Items.Count == 0)
            return Results.BadRequest(new { error = "Items list must not be empty." });

        var items = dto.Items.Select(i => new IngestOrderItem(
            i.Name ?? string.Empty,
            i.WidthMm,
            i.HeightMm,
            i.Material ?? string.Empty,
            Enum.TryParse<GrainDirection>(i.GrainDirection, true, out var gd) ? gd : GrainDirection.None,
            i.Quantity > 0 ? i.Quantity : 1
        )).ToList();

        var command = new IngestOrderCommand(dto.OrderId, dto.TenantId, items);
        var result = await mediator.Send(command, ct).ConfigureAwait(false);

        if (!result.IsSuccess)
        {
            if (result.Status == Ardalis.Result.ResultStatus.Invalid)
                return Results.BadRequest(result.ValidationErrors);
            return Results.BadRequest(result.Errors);
        }

        return Results.Ok(new { orderId = dto.OrderId, jobsCreated = result.Value });
    }

    private static IResult? AuthenticateInternalRequest(
        HttpRequest request,
        IConfiguration configuration)
    {
        var expectedSecret = configuration[InternalSecretConfigKey]
            ?? Environment.GetEnvironmentVariable(InternalSecretEnvKey);

        // Fail closed when service-to-service authentication has not been configured.
        if (string.IsNullOrWhiteSpace(expectedSecret))
            return Results.StatusCode(StatusCodes.Status503ServiceUnavailable);

        if (!request.Headers.TryGetValue(InternalHeader, out var headerValues)
            || headerValues.Count != 1)
        {
            return Results.StatusCode(StatusCodes.Status403Forbidden);
        }

        var providedSecret = headerValues[0] ?? string.Empty;
        var expectedHash = SHA256.HashData(Encoding.UTF8.GetBytes(expectedSecret));
        var providedHash = SHA256.HashData(Encoding.UTF8.GetBytes(providedSecret));

        return CryptographicOperations.FixedTimeEquals(providedHash, expectedHash)
            ? null
            : Results.StatusCode(StatusCodes.Status403Forbidden);
    }
}

public sealed record IngestOrderDto(
    Guid OrderId,
    Guid TenantId,
    List<IngestOrderItemDto>? Items);

public sealed record IngestOrderItemDto(
    string? Name,
    decimal WidthMm,
    decimal HeightMm,
    string? Material,
    string? GrainDirection,
    int Quantity);
