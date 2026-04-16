using Ardalis.Result;
using MediatR;

namespace SpaceOS.Modules.Cutting.Application.Queries.GetExecutionStatus;

public sealed record GetExecutionStatusQuery(Guid SheetId) : IRequest<Result<ExecutionStatusResponse>>;
