using Ardalis.Result;
using MediatR;
using SpaceOS.Modules.Cutting.Application.DTOs.QuoteRequest;
using SpaceOS.Modules.Cutting.Domain.Enums;
using SpaceOS.Modules.Cutting.Domain.Interfaces;

namespace SpaceOS.Modules.Cutting.Application.Queries.TrackQuote;

/// <summary>
/// Handler for tracking a quote by token.
/// </summary>
public sealed class TrackQuoteQueryHandler : IRequestHandler<TrackQuoteQuery, Result<QuoteTrackingDto>>
{
    private readonly IQuoteRequestRepository _repository;

    public TrackQuoteQueryHandler(IQuoteRequestRepository repository)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    }

    public async Task<Result<QuoteTrackingDto>> Handle(TrackQuoteQuery request, CancellationToken ct)
    {
        var quote = await _repository.GetByTrackingTokenAsync(request.TrackingToken, ct)
            .ConfigureAwait(false);

        if (quote == null)
        {
            return Result.NotFound($"Quote with tracking token {request.TrackingToken} not found.");
        }

        QuoteActionDto? actionRequired = null;
        if (quote.Status == QuoteStatus.Quoted)
        {
            actionRequired = new QuoteActionDto
            {
                Type = "CUSTOMER_ACCEPTANCE",
                Description = "Kattintson az 'Elfogadom' gombra az ajánlat véglegesítéséhez",
                AcceptUrl = $"/public/cutting/quotes/track/{quote.TrackingToken}/accept"
            };
        }

        var dto = new QuoteTrackingDto
        {
            QuoteNumber = quote.QuoteNumber,
            Status = quote.Status.ToString(),
            SubmittedAt = quote.CreatedAt,
            QuotedPrice = quote.QuotedPrice != null
                ? new QuotedPriceDto
                {
                    Amount = quote.QuotedPrice.Amount,
                    Currency = quote.QuotedPrice.Currency
                }
                : null,
            EstimatedDelivery = quote.Delivery.RequestedDate,
            ActionRequired = actionRequired
        };

        return Result.Success(dto);
    }
}
