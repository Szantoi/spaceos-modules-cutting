using Ardalis.Result;
using MediatR;
using SpaceOS.Modules.Cutting.Application.DTOs.QuoteRequest;

namespace SpaceOS.Modules.Cutting.Application.Queries.GetQuoteRequests;

/// <summary>
/// Query to get quote requests for a tenant (admin view).
/// </summary>
public sealed record GetQuoteRequestsQuery : IRequest<Result<List<QuoteRequestListItemDto>>>
{
    public required Guid TenantId { get; init; }
    public string? Status { get; init; }
}
