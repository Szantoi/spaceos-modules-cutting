using Ardalis.Result;
using MediatR;

namespace SpaceOS.Modules.Cutting.Application.Queries.GetWasteReport;

public sealed record GetWasteReportQuery(DateTime From, DateTime To) : IRequest<Result<WasteReportResponse>>;
