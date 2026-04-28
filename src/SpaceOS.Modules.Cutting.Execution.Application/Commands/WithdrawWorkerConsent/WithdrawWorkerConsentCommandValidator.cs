using FluentValidation;
using SpaceOS.Modules.Cutting.Execution.Application.Ports;
using SpaceOS.Modules.Cutting.Execution.Domain.Enums;

namespace SpaceOS.Modules.Cutting.Execution.Application.Commands.WithdrawWorkerConsent;

public sealed class WithdrawWorkerConsentCommandValidator : AbstractValidator<WithdrawWorkerConsentCommand>
{
    public WithdrawWorkerConsentCommandValidator(IConsentWithdrawalRepository consentRepository)
    {
        RuleFor(x => x.TenantId).NotEmpty();
        RuleFor(x => x.WorkerId).NotEmpty();
        RuleFor(x => x.Scope).IsInEnum();

        // Cross-check: no active withdrawal already pending for this worker+scope
        RuleFor(x => x)
            .MustAsync(async (cmd, ct) =>
            {
                var pending = await consentRepository.PickupNextPendingAsync(ct).ConfigureAwait(false);
                if (pending is null) return true;
                return !(pending.WorkerId == cmd.WorkerId && pending.TenantId == cmd.TenantId);
            })
            .WithMessage("An active consent withdrawal is already pending for this worker.");
    }
}
