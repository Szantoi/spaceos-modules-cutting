using Ardalis.Result;
using MediatR;
using SpaceOS.Modules.Cutting.Execution.Application.DTOs;

namespace SpaceOS.Modules.Cutting.Execution.Application.Queries.GetWorkerConsent;

public sealed record GetWorkerConsentQuery(Guid TenantId, Guid WorkerId) : IRequest<Result<WorkerConsentStatusDto>>;
