using Ardalis.Result;
using MediatR;
using SpaceOS.Modules.Cutting.Application.Adapters.Dtos;

namespace SpaceOS.Modules.Cutting.Application.Adapters.Commands;

/// <summary>
/// Resolves the configured adapter for the given tenant and performs a connectivity test.
/// Returns <see cref="AdapterTestResultDto"/> indicating whether the adapter is reachable.
/// </summary>
public sealed record TestAdapterCommand(Guid TenantId) : IRequest<Result<AdapterTestResultDto>>;
