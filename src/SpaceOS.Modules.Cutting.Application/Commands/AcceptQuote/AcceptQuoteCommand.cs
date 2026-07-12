using Ardalis.Result;
using MediatR;

namespace SpaceOS.Modules.Cutting.Application.Commands.AcceptQuote;

/// <summary>
/// Command for customer to accept a quote (converts to order).
/// </summary>
public sealed record AcceptQuoteCommand : IRequest<Result>
{
    public required string TrackingToken { get; init; }
}
