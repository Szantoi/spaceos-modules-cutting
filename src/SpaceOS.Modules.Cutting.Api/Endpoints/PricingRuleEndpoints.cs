using MediatR;
using Microsoft.AspNetCore.Mvc;
using SpaceOS.Modules.Cutting.Application.Commands.ActivatePricingRule;
using SpaceOS.Modules.Cutting.Application.Commands.CalculatePrice;
using SpaceOS.Modules.Cutting.Application.Commands.CreatePricingRule;
using SpaceOS.Modules.Cutting.Application.Queries.GetPricingRule;
using SpaceOS.Modules.Cutting.Contracts.Dtos;

namespace SpaceOS.Modules.Cutting.Api.Endpoints;

/// <summary>
/// Minimal API endpoints for PricingRule management (Track B - MSG-BACKEND-031).
/// </summary>
public static class PricingRuleEndpoints
{
    public static void MapPricingRuleEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/pricing-rules")
            .WithTags("Pricing Rules");

        // POST /api/pricing-rules — Create new pricing rule
        group.MapPost("/", CreatePricingRule)
            .WithName("CreatePricingRule")
            .WithSummary("Create a new PricingRule in draft state")
            .Produces<PricingRuleDto>(201)
            .ProducesValidationProblem(422)
            .ProducesProblem(400);

        // GET /api/pricing-rules/{id} — Get pricing rule by ID
        group.MapGet("/{id:guid}", GetPricingRule)
            .WithName("GetPricingRule")
            .WithSummary("Get a PricingRule by its ID")
            .Produces<PricingRuleDto>(200)
            .ProducesProblem(404);

        // PUT /api/pricing-rules/{id}/activate — Activate pricing rule
        group.MapPut("/{id:guid}/activate", ActivatePricingRule)
            .WithName("ActivatePricingRule")
            .WithSummary("Activate a PricingRule (transition from Draft to Active)")
            .Produces(200)
            .ProducesProblem(404)
            .ProducesProblem(400);

        // POST /api/pricing-rules/{id}/calculate-price — Calculate price
        group.MapPost("/{id:guid}/calculate-price", CalculatePrice)
            .WithName("CalculatePrice")
            .WithSummary("Calculate price based on a PricingRule")
            .Produces<PriceCalculationResponseDto>(200)
            .ProducesValidationProblem(422)
            .ProducesProblem(404)
            .ProducesProblem(400);
    }

    private static async Task<IResult> CreatePricingRule(
        [FromBody] CreatePricingRuleDto dto,
        [FromServices] IMediator mediator,
        CancellationToken ct)
    {
        var command = new CreatePricingRuleCommand
        {
            SupplierId = dto.SupplierId,
            ProductCategory = dto.ProductCategory,
            BasePricePerUnit = dto.BasePricePerUnit,
            QuantityBreakpoints = dto.QuantityBreakpoints,
            LeadTimeAdjustments = dto.LeadTimeAdjustments,
            MaterialSurcharges = dto.MaterialSurcharges
        };

        var result = await mediator.Send(command, ct).ConfigureAwait(false);

        return result.IsSuccess
            ? Results.Created($"/api/pricing-rules/{result.Value.Id}", result.Value)
            : result.ToMinimalApiResult();
    }

    private static async Task<IResult> GetPricingRule(
        [FromRoute] Guid id,
        [FromServices] IMediator mediator,
        CancellationToken ct)
    {
        var query = new GetPricingRuleQuery { PricingRuleId = id };
        var result = await mediator.Send(query, ct).ConfigureAwait(false);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : result.ToMinimalApiResult();
    }

    private static async Task<IResult> ActivatePricingRule(
        [FromRoute] Guid id,
        [FromServices] IMediator mediator,
        CancellationToken ct)
    {
        var command = new ActivatePricingRuleCommand { PricingRuleId = id };
        var result = await mediator.Send(command, ct).ConfigureAwait(false);

        return result.IsSuccess
            ? Results.Ok()
            : result.ToMinimalApiResult();
    }

    private static async Task<IResult> CalculatePrice(
        [FromRoute] Guid id,
        [FromBody] CalculatePriceRequestDto dto,
        [FromServices] IMediator mediator,
        CancellationToken ct)
    {
        var command = new CalculatePriceCommand
        {
            PricingRuleId = id,
            Quantity = dto.Quantity,
            LeadDays = dto.LeadDays,
            MaterialId = dto.MaterialId
        };

        var result = await mediator.Send(command, ct).ConfigureAwait(false);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : result.ToMinimalApiResult();
    }
}

/// <summary>
/// Extension methods to convert Ardalis.Result to IResult for Minimal API.
/// </summary>
internal static class ResultExtensions
{
    public static IResult ToMinimalApiResult<T>(this Ardalis.Result.Result<T> result)
    {
        return result.Status switch
        {
            Ardalis.Result.ResultStatus.NotFound => Results.NotFound(new { error = result.Errors.FirstOrDefault() ?? "Resource not found." }),
            Ardalis.Result.ResultStatus.Invalid => Results.ValidationProblem(result.ValidationErrors.ToDictionary(e => e.Identifier, e => new[] { e.ErrorMessage })),
            Ardalis.Result.ResultStatus.Error => Results.BadRequest(new { error = result.Errors.FirstOrDefault() ?? "An error occurred." }),
            _ => Results.BadRequest(new { error = "An unexpected error occurred." })
        };
    }

    public static IResult ToMinimalApiResult(this Ardalis.Result.Result result)
    {
        return result.Status switch
        {
            Ardalis.Result.ResultStatus.NotFound => Results.NotFound(new { error = result.Errors.FirstOrDefault() ?? "Resource not found." }),
            Ardalis.Result.ResultStatus.Invalid => Results.ValidationProblem(result.ValidationErrors.ToDictionary(e => e.Identifier, e => new[] { e.ErrorMessage })),
            Ardalis.Result.ResultStatus.Error => Results.BadRequest(new { error = result.Errors.FirstOrDefault() ?? "An error occurred." }),
            _ => Results.BadRequest(new { error = "An unexpected error occurred." })
        };
    }
}
