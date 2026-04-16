using Ardalis.Result;
using MediatR;
using SpaceOS.Modules.Cutting.Domain.Interfaces;

namespace SpaceOS.Modules.Cutting.Application.Queries.GetWasteReport;

public sealed class GetWasteReportQueryHandler : IRequestHandler<GetWasteReportQuery, Result<WasteReportResponse>>
{
    private readonly ICuttingRepository _repository;

    public GetWasteReportQueryHandler(ICuttingRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<WasteReportResponse>> Handle(GetWasteReportQuery request, CancellationToken ct)
    {
        var executions = await _repository.GetCompletedExecutionsInRangeAsync(request.From, request.To, ct).ConfigureAwait(false);
        var totalWaste = executions.Sum(e => e.WasteAreaCm2);
        var avg = executions.Count > 0 ? totalWaste / executions.Count : 0m;

        return Result<WasteReportResponse>.Success(new WasteReportResponse(totalWaste, avg, executions.Count));
    }
}
