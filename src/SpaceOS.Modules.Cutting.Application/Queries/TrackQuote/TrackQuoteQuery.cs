using Ardalis.Result;
using MediatR;
using SpaceOS.Modules.Cutting.Application.DTOs.QuoteRequest;

namespace SpaceOS.Modules.Cutting.Application.Queries.TrackQuote;

/// <summary>
/// Query to track a quote by tracking token (public, unauthenticated).
/// </summary>
public sealed record TrackQuoteQuery : IRequest<Result<QuoteTrackingDto>>
{
    public required string TrackingToken { get; init; }
}
