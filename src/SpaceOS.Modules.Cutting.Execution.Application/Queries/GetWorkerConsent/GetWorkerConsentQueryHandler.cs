using Ardalis.Result;
using MediatR;
using SpaceOS.Modules.Cutting.Execution.Application.DTOs;
using SpaceOS.Modules.Cutting.Execution.Application.Ports;

namespace SpaceOS.Modules.Cutting.Execution.Application.Queries.GetWorkerConsent;

public sealed class GetWorkerConsentQueryHandler(IConsentWithdrawalRepository consentRepository)
    : IRequestHandler<GetWorkerConsentQuery, Result<WorkerConsentStatusDto>>
{
    public async Task<Result<WorkerConsentStatusDto>> Handle(GetWorkerConsentQuery request, CancellationToken ct)
    {
        var pending = await consentRepository.PickupNextPendingAsync(ct).ConfigureAwait(false);
        var isWithdrawn = pending is not null &&
                          pending.WorkerId == request.WorkerId &&
                          pending.TenantId == request.TenantId;

        return Result<WorkerConsentStatusDto>.Success(
            new WorkerConsentStatusDto(
                request.WorkerId,
                IsActive: !isWithdrawn,
                WithdrawnAt: isWithdrawn ? pending!.RequestedAt : null));
    }
}
