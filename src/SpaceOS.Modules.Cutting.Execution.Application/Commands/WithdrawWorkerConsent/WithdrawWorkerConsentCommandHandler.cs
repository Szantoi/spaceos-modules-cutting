using Ardalis.Result;
using MediatR;
using SpaceOS.Modules.Cutting.Execution.Application.Entities;
using SpaceOS.Modules.Cutting.Execution.Application.Ports;
using SpaceOS.Modules.Cutting.Execution.Domain.Events;

namespace SpaceOS.Modules.Cutting.Execution.Application.Commands.WithdrawWorkerConsent;

public sealed class WithdrawWorkerConsentCommandHandler(
    IConsentWithdrawalRepository consentRepository,
    IPublisher publisher)
    : IRequestHandler<WithdrawWorkerConsentCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(WithdrawWorkerConsentCommand request, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var withdrawal = ConsentWithdrawal.Create(request.TenantId, request.WorkerId, request.Scope, now);
        await consentRepository.SaveAsync(withdrawal, ct).ConfigureAwait(false);

        await publisher.Publish(
            new WorkerConsentWithdrawalRequested(request.TenantId, request.WorkerId, request.Scope, now),
            ct).ConfigureAwait(false);

        return Result<Guid>.Success(withdrawal.Id);
    }
}
