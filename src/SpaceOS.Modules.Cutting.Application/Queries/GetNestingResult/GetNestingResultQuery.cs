using Ardalis.Result;
using MediatR;

namespace SpaceOS.Modules.Cutting.Application.Queries.GetNestingResult;

public sealed record GetNestingResultQuery(Guid SheetId) : IRequest<Result<NestingResultResponse>>;
