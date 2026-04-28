using FluentValidation;
using SpaceOS.Modules.Cutting.Execution.Domain.Enums;

namespace SpaceOS.Modules.Cutting.Execution.Application.Commands.CompleteExecution;

public sealed class CompleteExecutionCommandValidator : AbstractValidator<CompleteExecutionCommand>
{
    public CompleteExecutionCommandValidator()
    {
        RuleFor(x => x.ExecutionId).NotEmpty();
        RuleFor(x => x.TenantId).NotEmpty();
        RuleFor(x => x.ProofHash).NotEmpty().MaximumLength(256);
        When(x => x.ProofLevel >= ProofLevel.SignedEvidence, () =>
            RuleFor(x => x.Signature).NotEmpty().MaximumLength(512));
        When(x => x.ProofLevel == ProofLevel.PhotoEvidence, () =>
        {
            RuleFor(x => x.BlobRef).NotEmpty().MaximumLength(512);
            RuleFor(x => x.EncryptedWith).NotEmpty().MaximumLength(256);
        });
    }
}
