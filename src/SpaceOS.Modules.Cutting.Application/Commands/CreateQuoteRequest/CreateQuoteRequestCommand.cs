using Ardalis.Result;
using MediatR;
using SpaceOS.Modules.Cutting.Application.DTOs.QuoteRequest;

namespace SpaceOS.Modules.Cutting.Application.Commands.CreateQuoteRequest;

/// <summary>
/// Command to create a new public quote request.
/// </summary>
public sealed record CreateQuoteRequestCommand : IRequest<Result<QuoteRequestResponseDto>>
{
    public required Guid TenantId { get; init; }
    public required CreateQuoteRequestDto Data { get; init; }
}
