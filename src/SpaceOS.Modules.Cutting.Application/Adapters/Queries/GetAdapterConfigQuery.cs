using Ardalis.Result;
using MediatR;
using SpaceOS.Modules.Cutting.Application.Adapters.Dtos;

namespace SpaceOS.Modules.Cutting.Application.Adapters.Queries;

/// <summary>Returns the current adapter configuration for the given tenant.</summary>
public sealed record GetAdapterConfigQuery(Guid TenantId) : IRequest<Result<AdapterConfigDto>>;
