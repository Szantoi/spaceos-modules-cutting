using Ardalis.Result;
using MediatR;
using SpaceOS.Modules.Cutting.Execution.Application.DTOs;

namespace SpaceOS.Modules.Cutting.Execution.Application.Queries.ListExecutions;

public sealed record ListExecutionsQuery(Guid TenantId) : IRequest<Result<IReadOnlyList<ExecutionSummaryDto>>>;
