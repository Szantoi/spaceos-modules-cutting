using Ardalis.Result;
using MediatR;

namespace SpaceOS.Modules.Cutting.Application.Commands.ApproveQuote;

/// <summary>
/// Command to approve a quote request with a price.
/// </summary>
public sealed record ApproveQuoteCommand : IRequest<Result>
{
    public required Guid QuoteId { get; init; }
    public required decimal QuotedPriceAmount { get; init; }
    public required string QuotedPriceCurrency { get; init; }
    public required Guid UserId { get; init; }
}
