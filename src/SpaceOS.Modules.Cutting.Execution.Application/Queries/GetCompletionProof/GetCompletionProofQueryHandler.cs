using Ardalis.Result;
using MediatR;
using SpaceOS.Modules.Cutting.Execution.Application.DTOs;
using SpaceOS.Modules.Cutting.Execution.Domain.Interfaces;

namespace SpaceOS.Modules.Cutting.Execution.Application.Queries.GetCompletionProof;

public sealed class GetCompletionProofQueryHandler(ICuttingExecutionRepository repository)
    : IRequestHandler<GetCompletionProofQuery, Result<CompletionProofDto>>
{
    public async Task<Result<CompletionProofDto>> Handle(GetCompletionProofQuery request, CancellationToken ct)
    {
        var execution = await repository.GetByIdAsync(request.ExecutionId, ct).ConfigureAwait(false);
        if (execution is null)
            return Result<CompletionProofDto>.NotFound($"Execution {request.ExecutionId} not found.");

        if (execution.CompletionProof is null)
            return Result<CompletionProofDto>.NotFound("Completion proof not yet available.");

        var dto = new CompletionProofDto(
            execution.CompletionProof.Level.ToString(),
            execution.CompletionProof.ProofHash,
            execution.CompletedAt ?? DateTime.UtcNow);

        return Result<CompletionProofDto>.Success(dto);
    }
}
