using Ardalis.Result;
using MediatR;
using SpaceOS.Modules.Cutting.Contracts.Dtos;
using SpaceOS.Modules.Cutting.Domain.Interfaces;
using SpaceOS.Modules.Cutting.Domain.ValueObjects;

namespace SpaceOS.Modules.Cutting.Application.Commands.CalculatePrice;

/// <summary>
/// Handler for CalculatePriceCommand.
/// Calculates price using the PricingRule's calculation logic.
/// </summary>
public class CalculatePriceCommandHandler : IRequestHandler<CalculatePriceCommand, Result<PriceCalculationResponseDto>>
{
    private readonly ICuttingRepository _repository;

    public CalculatePriceCommandHandler(ICuttingRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<PriceCalculationResponseDto>> Handle(CalculatePriceCommand request, CancellationToken ct)
    {
        try
        {
            // Fetch the PricingRule aggregate
            var pricingRule = await _repository.GetPricingRuleByIdAsync(request.PricingRuleId, ct).ConfigureAwait(false);

            if (pricingRule == null)
            {
                return Result<PriceCalculationResponseDto>.NotFound($"PricingRule with ID {request.PricingRuleId} not found.");
            }

            // Validate that the rule is active
            if (pricingRule.Status != PricingRuleStatus.Active)
            {
                return Result<PriceCalculationResponseDto>.Error($"PricingRule {request.PricingRuleId} is not active (status: {pricingRule.Status}).");
            }

            // Calculate price using domain logic
            var result = pricingRule.CalculatePrice(request.Quantity, request.LeadDays, request.MaterialId);

            // Map to DTO
            var dto = new PriceCalculationResponseDto
            {
                Price = result.Price,
                Breakdown = result.Breakdown
            };

            return Result<PriceCalculationResponseDto>.Success(dto);
        }
        catch (ArgumentException ex)
        {
            return Result<PriceCalculationResponseDto>.Invalid(new ValidationError { ErrorMessage = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Result<PriceCalculationResponseDto>.Error(ex.Message);
        }
    }
}
