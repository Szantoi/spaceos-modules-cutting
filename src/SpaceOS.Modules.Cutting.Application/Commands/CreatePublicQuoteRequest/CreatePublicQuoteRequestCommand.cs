using Ardalis.Result;
using MediatR;
using SpaceOS.Modules.Cutting.Application.DTOs.QuoteRequest;

namespace SpaceOS.Modules.Cutting.Application.Commands.CreatePublicQuoteRequest;

/// <summary>
/// Command to create a single-item public quote request (Q3 Track A - MSG-BACKEND-030).
/// B2C endpoint: Simplified single-item format.
/// </summary>
public sealed record CreatePublicQuoteRequestCommand : IRequest<Result<PublicQuoteResponseDto>>
{
    public required PublicQuoteRequestDto Data { get; init; }
}
