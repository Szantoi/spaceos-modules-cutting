using FluentValidation;

namespace SpaceOS.Modules.Cutting.Execution.Application.Commands.RecordProgress;

public sealed class RecordProgressCommandValidator : AbstractValidator<RecordProgressCommand>
{
    public RecordProgressCommandValidator()
    {
        RuleFor(x => x.ExecutionId).NotEmpty();
        RuleFor(x => x.TenantId).NotEmpty();
        RuleFor(x => x.EventId).NotEmpty();
        RuleFor(x => x.OccurredAt).NotEmpty();
        RuleFor(x => x.EventHmacBase64).NotEmpty().MaximumLength(512);
        RuleFor(x => x.HmacKeyVersion).NotEmpty().MaximumLength(50);
    }
}
