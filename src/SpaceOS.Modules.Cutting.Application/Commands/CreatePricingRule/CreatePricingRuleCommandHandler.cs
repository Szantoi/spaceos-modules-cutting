using Ardalis.Result;
using MediatR;
using SpaceOS.Modules.Cutting.Contracts.Dtos;
using SpaceOS.Modules.Cutting.Domain.Aggregates;
using SpaceOS.Modules.Cutting.Domain.Interfaces;

namespace SpaceOS.Modules.Cutting.Application.Commands.CreatePricingRule;

/// <summary>
/// Handler for CreatePricingRuleCommand.
/// Creates a new PricingRule in draft state with all breakpoints, adjustments, and surcharges.
/// </summary>
public class CreatePricingRuleCommandHandler : IRequestHandler<CreatePricingRuleCommand, Result<PricingRuleDto>>
{
    private readonly ICuttingRepository _repository;

    public CreatePricingRuleCommandHandler(ICuttingRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<PricingRuleDto>> Handle(CreatePricingRuleCommand request, CancellationToken ct)
    {
        try
        {
            // Create the PricingRule aggregate
            var pricingRule = PricingRule.Create(
                request.SupplierId,
                request.ProductCategory,
                request.BasePricePerUnit
            );

            // Add quantity breakpoints
            foreach (var breakpoint in request.QuantityBreakpoints)
            {
                pricingRule.AddQuantityBreakpoint(
                    breakpoint.MinQuantity,
                    breakpoint.MaxQuantity,
                    breakpoint.DiscountPercent
                );
            }

            // Add lead time adjustments
            foreach (var adjustment in request.LeadTimeAdjustments)
            {
                pricingRule.AddLeadTimeAdjustment(
                    adjustment.LeadDays,
                    adjustment.AdjustmentFactor
                );
            }

            // Add material surcharges
            foreach (var surcharge in request.MaterialSurcharges)
            {
                pricingRule.AddMaterialSurcharge(
                    surcharge.MaterialId,
                    surcharge.SurchargePercent
                );
            }

            // Persist to database
            await _repository.AddPricingRuleAsync(pricingRule, ct).ConfigureAwait(false);
            await _repository.SaveChangesAsync(ct).ConfigureAwait(false);

            // Map to DTO
            var dto = new PricingRuleDto
            {
                Id = pricingRule.Id,
                SupplierId = pricingRule.SupplierId,
                ProductCategory = pricingRule.ProductCategory,
                BasePricePerUnit = pricingRule.BasePricePerUnit,
                Status = pricingRule.Status.ToString(),
                CreatedAt = pricingRule.CreatedAt,
                UpdatedAt = pricingRule.UpdatedAt,
                Version = pricingRule.Version,
                QuantityBreakpoints = pricingRule.QuantityBreakpoints.Select(b => new QuantityBreakpointDto
                {
                    Id = b.Id,
                    MinQuantity = b.MinQuantity,
                    MaxQuantity = b.MaxQuantity,
                    DiscountPercent = b.DiscountPercent
                }).ToList(),
                LeadTimeAdjustments = pricingRule.LeadTimeAdjustments.Select(a => new LeadTimeAdjustmentDto
                {
                    Id = a.Id,
                    LeadDays = a.LeadDays,
                    AdjustmentFactor = a.AdjustmentFactor
                }).ToList(),
                MaterialSurcharges = pricingRule.MaterialSurcharges.Select(s => new MaterialSurchargeDto
                {
                    Id = s.Id,
                    MaterialId = s.MaterialId,
                    SurchargePercent = s.SurchargePercent
                }).ToList()
            };

            return Result<PricingRuleDto>.Success(dto);
        }
        catch (ArgumentException ex)
        {
            return Result<PricingRuleDto>.Invalid(new ValidationError { ErrorMessage = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Result<PricingRuleDto>.Error(ex.Message);
        }
    }
}
