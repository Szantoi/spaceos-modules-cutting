using Ardalis.Result;
using MediatR;
using SpaceOS.Modules.Cutting.Execution.Domain.Enums;
using SpaceOS.Modules.Cutting.Execution.Domain.Interfaces;
using SpaceOS.Modules.Cutting.Execution.Domain.ValueObjects;

namespace SpaceOS.Modules.Cutting.Execution.Application.Commands.CompleteExecution;

public sealed class CompleteExecutionCommandHandler(
    ICuttingExecutionRepository repository,
    ICuttingProofPolicy proofPolicy)
    : IRequestHandler<CompleteExecutionCommand, Result>
{
    public async Task<Result> Handle(CompleteExecutionCommand request, CancellationToken ct)
    {
        var execution = await repository.GetByIdWithProgressAsync(request.ExecutionId, ct).ConfigureAwait(false);
        if (execution is null)
            return Result.NotFound($"Execution {request.ExecutionId} not found.");

        var proofResult = request.ProofLevel switch
        {
            ProofLevel.HashOnly => CompletionProof.CreateHashOnly(request.ProofHash),
            ProofLevel.SignedEvidence => CompletionProof.CreateSignedEvidence(request.ProofHash, request.Signature!),
            ProofLevel.PhotoEvidence => CompletionProof.CreatePhotoEvidence(request.ProofHash, request.Signature!, request.BlobRef!, request.EncryptedWith!),
            _ => Result<CompletionProof>.Invalid(new ValidationError("Unknown proof level."))
        };

        if (!proofResult.IsSuccess)
            return Result.Invalid(proofResult.ValidationErrors);

        var result = execution.Complete(proofResult.Value, proofPolicy, DateTime.UtcNow);
        if (!result.IsSuccess)
            return result;

        await repository.SaveChangesAsync(ct).ConfigureAwait(false);
        return Result.Success();
    }
}
