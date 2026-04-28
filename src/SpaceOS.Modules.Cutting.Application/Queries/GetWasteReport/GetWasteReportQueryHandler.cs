using Ardalis.Result;
using MediatR;
using SpaceOS.Modules.Cutting.Domain.Interfaces;

namespace SpaceOS.Modules.Cutting.Application.Queries.GetWasteReport;

/// <summary>
/// Returns a waste report. Phase 3 implementation — execution-level waste data is available
/// in Phase 4 via the CuttingExecution aggregate. This handler returns an empty stub.
/// </summary>
public sealed class GetWasteReportQueryHandler : IRequestHandler<GetWasteReportQuery, Result<WasteReportResponse>>
{
    // Repository kept for future Phase 4 integration
    private readonly ICuttingRepository _repository;

    public GetWasteReportQueryHandler(ICuttingRepository repository)
    {
        _repository = repository;
    }

    public Task<Result<WasteReportResponse>> Handle(GetWasteReportQuery request, CancellationToken ct)
    {
        // Phase 3 stub — execution waste data is tracked in Phase 4 via CuttingExecution aggregate.
        // Returns an empty report until Phase 4 waste query is wired.
        return Task.FromResult(Result<WasteReportResponse>.Success(new WasteReportResponse(0m, 0m, 0)));
    }
}
