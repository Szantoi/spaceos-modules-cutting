using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SpaceOS.Modules.Cutting.Application.Commands.CreateQuoteRequest;
using SpaceOS.Modules.Cutting.Application.Commands.CreatePublicQuoteRequest;
using SpaceOS.Modules.Cutting.Application.Commands.ApproveQuote;
using SpaceOS.Modules.Cutting.Application.Commands.RejectQuote;
using SpaceOS.Modules.Cutting.Application.Commands.AcceptQuote;
using SpaceOS.Modules.Cutting.Application.Queries.GetQuoteRequests;
using SpaceOS.Modules.Cutting.Application.Queries.TrackQuote;
using SpaceOS.Modules.Cutting.Application.DTOs.QuoteRequest;
using SpaceOS.Modules.Cutting.Application.Services;

namespace SpaceOS.Modules.Cutting.Api.Endpoints;

/// <summary>
/// API endpoints for customer quote requests (Q3 feature).
/// </summary>
public static class QuoteRequestEndpoints
{
    public static IEndpointRouteBuilder MapQuoteRequestEndpoints(this IEndpointRouteBuilder app)
    {
        // Q3 Track A: Public API endpoints (MSG-BACKEND-030)
        var apiPublicGroup = app.MapGroup("/api/public/cutting");

        apiPublicGroup.MapPost("/quote-request", CreatePublicQuoteRequest)
            .AllowAnonymous()
            .RequireRateLimiting("PublicCuttingLimiter");  // Phase 5: 50 req/hour per IP

        // Legacy public endpoints (unauthenticated)
        var publicGroup = app.MapGroup("/public/cutting");

        publicGroup.MapPost("/quote-request", CreateQuoteRequest)
            .AllowAnonymous();

        publicGroup.MapGet("/quotes/track/{trackingToken}", TrackQuote)
            .AllowAnonymous();

        publicGroup.MapPost("/quotes/track/{trackingToken}/accept", AcceptQuote)
            .AllowAnonymous();

        // Admin endpoints (authenticated, tenant-scoped)
        var adminGroup = app.MapGroup("/api/cutting/quotes")
            .RequireAuthorization("ManufacturerOnly");

        adminGroup.MapGet("/", GetQuoteRequests);
        adminGroup.MapPut("/{quoteId:guid}/approve", ApproveQuote);
        adminGroup.MapPut("/{quoteId:guid}/reject", RejectQuote);

        return app;
    }

    /// <summary>
    /// POST /api/public/cutting/quote-request
    /// Creates a single-item public quote request (Q3 Track A - MSG-BACKEND-030, Phase 5).
    /// B2C endpoint: No tenant resolution, simplified single-item format.
    /// Phase 5: FluentValidation added for request validation.
    /// </summary>
    private static async Task<IResult> CreatePublicQuoteRequest(
        PublicQuoteRequestDto request,
        IValidator<PublicQuoteRequestDto> validator,
        IMediator mediator,
        ILogger<Program> logger,
        CancellationToken ct)
    {
        try
        {
            // Phase 5: Validate request
            var validationResult = await validator.ValidateAsync(request, ct).ConfigureAwait(false);
            if (!validationResult.IsValid)
            {
                var errors = validationResult.Errors
                    .GroupBy(e => e.PropertyName)
                    .ToDictionary(
                        g => g.Key,
                        g => g.Select(e => e.ErrorMessage).ToArray()
                    );

                logger.LogWarning(
                    "Public quote request validation failed. Email: {CustomerEmail}, ValidationErrors: {ErrorCount}",
                    request.CustomerEmail,
                    validationResult.Errors.Count);

                return Results.ValidationProblem(errors);
            }

            var command = new CreatePublicQuoteRequestCommand
            {
                Data = request
            };

            var result = await mediator.Send(command, ct).ConfigureAwait(false);

            if (!result.IsSuccess)
            {
                logger.LogError(
                    "Failed to create public quote request. Email: {CustomerEmail}, Errors: {Errors}",
                    request.CustomerEmail,
                    string.Join(", ", result.Errors));

                return Results.BadRequest(new { Errors = result.Errors });
            }

            var response = result.Value!;

            // Phase 5: Audit log successful creation
            logger.LogInformation(
                "Public quote request created successfully. QuoteId: {QuoteId}, CustomerEmail: {CustomerEmail}, Material: {Material}, Quantity: {Quantity}",
                response.QuoteId,
                request.CustomerEmail,
                request.Material,
                request.Quantity);

            return Results.Created($"/api/public/cutting/quote-request/{response.QuoteId}", response);
        }
        catch (Exception ex)
        {
            // Phase 5: Log exception details (MSG-BACKEND-079)
            logger.LogError(ex,
                "Unhandled exception in CreatePublicQuoteRequest. CustomerEmail: {CustomerEmail}",
                request.CustomerEmail);

            return Results.Problem(detail: "An error occurred processing your request.", statusCode: 500);
        }
    }

    /// <summary>
    /// POST /public/cutting/quote-request
    /// Creates a new quote request (public, unauthenticated).
    /// Q3 Track A: Subdomain-based tenant resolution + email notifications.
    /// </summary>
    private static async Task<IResult> CreateQuoteRequest(
        CreateQuoteRequestDto request,
        IMediator mediator,
        ITenantResolver tenantResolver,
        IEmailService emailService,
        HttpContext httpContext,
        CancellationToken ct)
    {
        try
        {
            // Q3 Track A: Extract hostname from X-Original-Host header (nginx forwarded) or Host
            var hostname = httpContext.Request.Headers["X-Original-Host"].FirstOrDefault()
                ?? httpContext.Request.Host.Host;

            // Resolve tenant from subdomain (e.g., "doorstar.joinerytech.hu" → Doorstar tenant)
            var tenantId = await tenantResolver.GetTenantIdFromSubdomain(hostname, ct).ConfigureAwait(false);

            var command = new CreateQuoteRequestCommand
            {
                TenantId = tenantId,
                Data = request
            };

            var result = await mediator.Send(command, ct).ConfigureAwait(false);

            if (!result.IsSuccess)
                return Results.BadRequest(new { Errors = result.Errors });

            // Q3 Track A: Send email notifications (customer confirmation + admin notification)
            var response = result.Value!;
            var adminEmail = "support@joinerytech.hu"; // TODO: Get from tenant configuration

            await emailService.SendQuoteRequestNotification(
                customerEmail: request.CustomerEmail,
                adminEmail: adminEmail,
                quoteNumber: response.QuoteNumber,
                trackingToken: response.TrackingToken,
                trackingUrl: response.TrackingUrl,
                ct: ct).ConfigureAwait(false);

            return Results.Ok(response);
        }
        catch (TenantNotFoundException ex)
        {
            return Results.NotFound(new { Error = ex.Message });
        }
        catch (Exception ex)
        {
            // Log exception (ILogger should be injected)
            return Results.Problem(detail: "An error occurred processing your request.", statusCode: 500);
        }
    }

    /// <summary>
    /// GET /public/cutting/quotes/track/{trackingToken}
    /// Tracks a quote by tracking token (public, unauthenticated).
    /// </summary>
    private static async Task<IResult> TrackQuote(
        string trackingToken,
        IMediator mediator,
        CancellationToken ct)
    {
        var query = new TrackQuoteQuery { TrackingToken = trackingToken };
        var result = await mediator.Send(query, ct).ConfigureAwait(false);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.NotFound(new { Errors = result.Errors });
    }

    /// <summary>
    /// POST /public/cutting/quotes/track/{trackingToken}/accept
    /// Customer accepts a quote (public, unauthenticated).
    /// </summary>
    private static async Task<IResult> AcceptQuote(
        string trackingToken,
        IMediator mediator,
        CancellationToken ct)
    {
        var command = new AcceptQuoteCommand { TrackingToken = trackingToken };
        var result = await mediator.Send(command, ct).ConfigureAwait(false);

        return result.IsSuccess
            ? Results.Ok(new { Message = "Quote accepted. Order has been created." })
            : Results.BadRequest(new { Errors = result.Errors });
    }

    /// <summary>
    /// GET /api/cutting/quotes
    /// Lists quote requests for tenant admin.
    /// </summary>
    private static async Task<IResult> GetQuoteRequests(
        string? status,
        IMediator mediator,
        HttpContext httpContext,
        CancellationToken ct)
    {
        var tenantId = GetTenantId(httpContext);
        if (tenantId == Guid.Empty) return Results.Unauthorized();

        var query = new GetQuoteRequestsQuery
        {
            TenantId = tenantId,
            Status = status
        };

        var result = await mediator.Send(query, ct).ConfigureAwait(false);
        return result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.BadRequest(new { Errors = result.Errors });
    }

    /// <summary>
    /// PUT /api/cutting/quotes/{quoteId}/approve
    /// Approves a quote request with a price.
    /// Q3 Track A: Email notification hook.
    /// </summary>
    private static async Task<IResult> ApproveQuote(
        Guid quoteId,
        ApproveQuoteRequestDto request,
        IMediator mediator,
        IEmailService emailService,
        HttpContext httpContext,
        CancellationToken ct)
    {
        var userId = GetUserId(httpContext);
        if (userId == Guid.Empty) return Results.Unauthorized();

        var command = new ApproveQuoteCommand
        {
            QuoteId = quoteId,
            QuotedPriceAmount = request.QuotedPriceAmount,
            QuotedPriceCurrency = request.QuotedPriceCurrency,
            UserId = userId
        };

        var result = await mediator.Send(command, ct).ConfigureAwait(false);

        if (!result.IsSuccess)
            return Results.BadRequest(new { Errors = result.Errors });

        // Q3 Track A: Send email notification to customer
        // TODO: Get customer email and tracking URL from quote aggregate
        var customerEmail = request.CustomerEmail; // Temporary - should come from aggregate
        var trackingUrl = $"https://joinerytech.hu/track/{quoteId}"; // Placeholder
        var acceptUrl = $"{trackingUrl}/accept";

        await emailService.SendQuoteApprovedNotification(
            customerEmail: customerEmail,
            quoteNumber: $"Q-{quoteId:N}",
            price: request.QuotedPriceAmount,
            currency: request.QuotedPriceCurrency,
            acceptUrl: acceptUrl,
            ct: ct).ConfigureAwait(false);

        return Results.Ok(new { Message = "Quote approved successfully." });
    }

    /// <summary>
    /// PUT /api/cutting/quotes/{quoteId}/reject
    /// Rejects a quote request.
    /// Q3 Track A: Email notification hook.
    /// </summary>
    private static async Task<IResult> RejectQuote(
        Guid quoteId,
        RejectQuoteRequestDto request,
        IMediator mediator,
        IEmailService emailService,
        HttpContext httpContext,
        CancellationToken ct)
    {
        var userId = GetUserId(httpContext);
        if (userId == Guid.Empty) return Results.Unauthorized();

        var command = new RejectQuoteCommand
        {
            QuoteId = quoteId,
            Reason = request.Reason,
            UserId = userId
        };

        var result = await mediator.Send(command, ct).ConfigureAwait(false);

        if (!result.IsSuccess)
            return Results.BadRequest(new { Errors = result.Errors });

        // Q3 Track A: Send email notification to customer
        // TODO: Get customer email from quote aggregate
        var customerEmail = request.CustomerEmail; // Temporary - should come from aggregate

        await emailService.SendQuoteRejectedNotification(
            customerEmail: customerEmail,
            quoteNumber: $"Q-{quoteId:N}",
            reason: request.Reason,
            ct: ct).ConfigureAwait(false);

        return Results.Ok(new { Message = "Quote rejected successfully." });
    }

    // Helper methods
    private static Guid GetTenantId(HttpContext httpContext)
    {
        var claim = httpContext.User.FindFirst("tenant_id");
        return claim != null && Guid.TryParse(claim.Value, out var id) ? id : Guid.Empty;
    }

    private static Guid GetUserId(HttpContext httpContext)
    {
        var claim = httpContext.User.FindFirst("sub");
        return claim != null && Guid.TryParse(claim.Value, out var id) ? id : Guid.Empty;
    }

    private static Guid GetTenantIdFromContext(HttpContext httpContext)
    {
        // For public endpoints, tenant might come from subdomain, header, or query param
        // Example: header "X-Tenant-Id" or subdomain parsing
        if (httpContext.Request.Headers.TryGetValue("X-Tenant-Id", out var tenantHeader)
            && Guid.TryParse(tenantHeader.FirstOrDefault(), out var tenantId))
        {
            return tenantId;
        }

        // TODO: Implement subdomain-based tenant resolution
        // For now, return a default tenant for testing
        return Guid.Empty;
    }
}

// Request DTOs
public sealed record ApproveQuoteRequestDto
{
    public required decimal QuotedPriceAmount { get; init; }
    public required string QuotedPriceCurrency { get; init; }
    public required string CustomerEmail { get; init; } // Q3 Track A: Temporary - TODO get from aggregate
}

public sealed record RejectQuoteRequestDto
{
    public required string Reason { get; init; }
    public required string CustomerEmail { get; init; } // Q3 Track A: Temporary - TODO get from aggregate
}
