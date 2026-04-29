using Ardalis.Result;
using MediatR;
using SpaceOS.Modules.Cutting.Application.Adapters.Dtos;

namespace SpaceOS.Modules.Cutting.Application.Adapters.Queries;

/// <summary>Returns the most recent health record for the given tenant's adapter.</summary>
public sealed record GetAdapterHealthQuery(Guid TenantId) : IRequest<Result<AdapterHealthDto>>;
