using Ardalis.Result;
using MediatR;
using SpaceOS.Modules.Cutting.Execution.Domain.Enums;

namespace SpaceOS.Modules.Cutting.Execution.Application.Commands.WithdrawWorkerConsent;

public sealed record WithdrawWorkerConsentCommand(
    Guid TenantId,
    Guid WorkerId,
    ConsentScope Scope) : IRequest<Result<Guid>>;
