using Ardalis.Result;
using MediatR;
using SpaceOS.Modules.Cutting.Application.DTOs.QuoteRequest;
using SpaceOS.Modules.Cutting.Domain.Aggregates;
using SpaceOS.Modules.Cutting.Domain.Enums;
using SpaceOS.Modules.Cutting.Domain.Interfaces;
using SpaceOS.Modules.Cutting.Domain.ValueObjects;
using System.Security.Cryptography;

namespace SpaceOS.Modules.Cutting.Application.Commands.CreateQuoteRequest;

/// <summary>
/// Handler for creating a new quote request.
/// </summary>
public sealed class CreateQuoteRequestCommandHandler
    : IRequestHandler<CreateQuoteRequestCommand, Result<QuoteRequestResponseDto>>
{
    private readonly IQuoteRequestRepository _repository;
    private readonly ICuttingRepository _cuttingRepository;

    public CreateQuoteRequestCommandHandler(
        IQuoteRequestRepository repository,
        ICuttingRepository cuttingRepository)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _cuttingRepository = cuttingRepository ?? throw new ArgumentNullException(nameof(cuttingRepository));
    }

    public async Task<Result<QuoteRequestResponseDto>> Handle(
        CreateQuoteRequestCommand request,
        CancellationToken ct)
    {
        var dto = request.Data;

        // Generate unique quote number
        var quoteNumber = await GenerateQuoteNumberAsync(request.TenantId, ct).ConfigureAwait(false);

        // Generate tracking token (12-char hex)
        var trackingToken = GenerateTrackingToken();

        // Map DTOs to ValueObjects
        var contactInfo = new ContactInfo(dto.CustomerEmail, dto.CustomerName, dto.CustomerPhone);

        var items = dto.Items.Select(i => new QuoteLineItem(
            Enum.Parse<MaterialType>(i.MaterialType),
            i.WidthMm,
            i.HeightMm,
            i.Quantity,
            Enum.Parse<EdgingType>(i.EdgingType),
            i.Notes)).ToList();

        var delivery = new DeliveryDetails(dto.DeliveryAddress, dto.RequestedDeliveryDate);

        // Create aggregate
        var quoteRequest = CuttingQuoteRequest.CreatePublic(
            request.TenantId,
            quoteNumber,
            trackingToken,
            contactInfo,
            items,
            delivery);

        // Persist
        await _repository.AddAsync(quoteRequest, ct).ConfigureAwait(false);
        await _cuttingRepository.SaveChangesAsync(ct).ConfigureAwait(false);

        // TODO: Raise domain event → email notification

        // Return response
        return Result.Success(new QuoteRequestResponseDto
        {
            QuoteId = quoteRequest.Id.ToString(),
            QuoteNumber = quoteRequest.QuoteNumber,
            TrackingToken = quoteRequest.TrackingToken,
            Status = quoteRequest.Status.ToString(),
            EstimatedResponseTime = "24 hours",
            TrackingUrl = $"/public/cutting/quotes/track/{quoteRequest.TrackingToken}"
        });
    }

    private async Task<string> GenerateQuoteNumberAsync(Guid tenantId, CancellationToken ct)
    {
        var year = DateTime.UtcNow.Year;
        var prefix = $"QT-{year}-";

        for (int attempt = 0; attempt < 100; attempt++)
        {
            var sequence = Random.Shared.Next(100000, 999999);
            var quoteNumber = $"{prefix}{sequence:D6}";

            var exists = await _repository.ExistsAsync(tenantId, quoteNumber, ct).ConfigureAwait(false);
            if (!exists)
            {
                return quoteNumber;
            }
        }

        throw new InvalidOperationException("Failed to generate unique quote number after 100 attempts.");
    }

    private static string GenerateTrackingToken()
    {
        var bytes = new byte[6]; // 6 bytes = 12 hex chars
        RandomNumberGenerator.Fill(bytes);
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
