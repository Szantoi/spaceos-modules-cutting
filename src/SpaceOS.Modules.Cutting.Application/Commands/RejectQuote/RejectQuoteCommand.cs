using Ardalis.Result;
using MediatR;

namespace SpaceOS.Modules.Cutting.Application.Commands.RejectQuote;

/// <summary>
/// Command to reject a quote request.
/// </summary>
public sealed record RejectQuoteCommand : IRequest<Result>
{
    public required Guid QuoteId { get; init; }
    public required string Reason { get; init; }
    public required Guid UserId { get; init; }
}
