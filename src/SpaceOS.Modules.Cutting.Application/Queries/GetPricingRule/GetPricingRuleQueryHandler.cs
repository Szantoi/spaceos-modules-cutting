using Ardalis.Result;
using MediatR;
using SpaceOS.Modules.Cutting.Contracts.Dtos;
using SpaceOS.Modules.Cutting.Domain.Interfaces;

namespace SpaceOS.Modules.Cutting.Application.Queries.GetPricingRule;

/// <summary>
/// Handler for GetPricingRuleQuery.
/// Fetches a PricingRule by ID and returns it as a DTO.
/// </summary>
public class GetPricingRuleQueryHandler : IRequestHandler<GetPricingRuleQuery, Result<PricingRuleDto>>
{
    private readonly ICuttingRepository _repository;

    public GetPricingRuleQueryHandler(ICuttingRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<PricingRuleDto>> Handle(GetPricingRuleQuery request, CancellationToken ct)
    {
        var pricingRule = await _repository.GetPricingRuleByIdAsync(request.PricingRuleId, ct).ConfigureAwait(false);

        if (pricingRule == null)
        {
            return Result<PricingRuleDto>.NotFound($"PricingRule with ID {request.PricingRuleId} not found.");
        }

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
}
