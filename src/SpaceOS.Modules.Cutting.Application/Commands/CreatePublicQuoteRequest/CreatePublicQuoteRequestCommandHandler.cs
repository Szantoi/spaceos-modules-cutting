using Ardalis.Result;
using MediatR;
using SpaceOS.Modules.Cutting.Application.DTOs.QuoteRequest;
using SpaceOS.Modules.Cutting.Domain.Entities;
using SpaceOS.Modules.Cutting.Domain.Interfaces;

namespace SpaceOS.Modules.Cutting.Application.Commands.CreatePublicQuoteRequest;

/// <summary>
/// Handler for creating a single-item public quote request (Q3 Track A - MSG-BACKEND-030).
/// </summary>
public sealed class CreatePublicQuoteRequestCommandHandler
    : IRequestHandler<CreatePublicQuoteRequestCommand, Result<PublicQuoteResponseDto>>
{
    private readonly ICuttingRepository _repository;

    public CreatePublicQuoteRequestCommandHandler(ICuttingRepository repository)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    }

    public async Task<Result<PublicQuoteResponseDto>> Handle(
        CreatePublicQuoteRequestCommand request,
        CancellationToken ct)
    {
        var dto = request.Data;

        // Create domain entity
        var quoteRequest = PublicQuoteRequest.Create(
            customerName: dto.CustomerName,
            customerEmail: dto.CustomerEmail,
            customerPhone: dto.CustomerPhone,
            companyName: dto.CompanyName,
            material: dto.Material,
            lengthMm: dto.Dimensions.Length,
            widthMm: dto.Dimensions.Width,
            thicknessMm: dto.Dimensions.Thickness,
            quantity: dto.Quantity,
            edgeType: dto.EdgeType,
            surface: dto.Surface,
            urgency: dto.Urgency,
            notes: dto.Notes);

        // Persist to database (Phase 3 - MSG-BACKEND-078)
        await _repository.AddPublicQuoteRequestAsync(quoteRequest, ct).ConfigureAwait(false);
        await _repository.SaveChangesAsync(ct).ConfigureAwait(false);

        // Calculate estimated reply time (2 business days)
        var estimatedReplyTime = dto.Urgency.ToLowerInvariant() == "express"
            ? "1 business day"
            : "2 business days";

        // Return response
        return Result.Success(new PublicQuoteResponseDto
        {
            QuoteId = quoteRequest.Id,
            Status = quoteRequest.Status,
            EstimatedReplyTime = estimatedReplyTime,
            TrackingUrl = $"/public/quote/{quoteRequest.Id}/status"
        });
    }
}
