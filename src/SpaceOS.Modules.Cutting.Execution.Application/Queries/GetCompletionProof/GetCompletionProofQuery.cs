using Ardalis.Result;
using MediatR;
using SpaceOS.Modules.Cutting.Execution.Application.DTOs;

namespace SpaceOS.Modules.Cutting.Execution.Application.Queries.GetCompletionProof;

public sealed record GetCompletionProofQuery(Guid ExecutionId, Guid TenantId) : IRequest<Result<CompletionProofDto>>;
