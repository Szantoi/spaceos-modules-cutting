using Ardalis.Result;
using MediatR;
using SpaceOS.Modules.Cutting.Application.DTOs.QuoteRequest;
using SpaceOS.Modules.Cutting.Domain.Enums;
using SpaceOS.Modules.Cutting.Domain.Interfaces;

namespace SpaceOS.Modules.Cutting.Application.Queries.GetQuoteRequests;

/// <summary>
/// Handler for getting quote requests.
/// </summary>
public sealed class GetQuoteRequestsQueryHandler
    : IRequestHandler<GetQuoteRequestsQuery, Result<List<QuoteRequestListItemDto>>>
{
    private readonly IQuoteRequestRepository _repository;

    public GetQuoteRequestsQueryHandler(IQuoteRequestRepository repository)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    }

    public async Task<Result<List<QuoteRequestListItemDto>>> Handle(
        GetQuoteRequestsQuery request,
        CancellationToken ct)
    {
        QuoteStatus? status = null;
        if (!string.IsNullOrWhiteSpace(request.Status))
        {
            if (!Enum.TryParse<QuoteStatus>(request.Status, out var parsedStatus))
            {
                return Result.Error($"Invalid status value: {request.Status}");
            }
            status = parsedStatus;
        }

        var quotes = await _repository.GetByTenantAsync(request.TenantId, status, ct)
            .ConfigureAwait(false);

        var dtos = quotes.Select(q => new QuoteRequestListItemDto
        {
            Id = q.Id,
            QuoteNumber = q.QuoteNumber,
            Status = q.Status.ToString(),
            CustomerEmail = q.CustomerContact.Email,
            CustomerName = q.CustomerContact.Name,
            ItemCount = q.Items.Count,
            CreatedAt = q.CreatedAt,
            QuotedPrice = q.QuotedPrice != null
                ? new QuotedPriceDto
                {
                    Amount = q.QuotedPrice.Amount,
                    Currency = q.QuotedPrice.Currency
                }
                : null
        }).ToList();

        return Result.Success(dtos);
    }
}
