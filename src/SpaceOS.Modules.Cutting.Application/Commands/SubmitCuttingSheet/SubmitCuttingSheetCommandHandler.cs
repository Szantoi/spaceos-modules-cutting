using Ardalis.Result;
using MediatR;
using SpaceOS.Modules.Cutting.Domain.Aggregates;
using SpaceOS.Modules.Cutting.Domain.Entities;
using SpaceOS.Modules.Cutting.Domain.Interfaces;

namespace SpaceOS.Modules.Cutting.Application.Commands.SubmitCuttingSheet;

public sealed class SubmitCuttingSheetCommandHandler : IRequestHandler<SubmitCuttingSheetCommand, Result<Guid>>
{
    private readonly ICuttingRepository _repository;

    public SubmitCuttingSheetCommandHandler(ICuttingRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<Guid>> Handle(SubmitCuttingSheetCommand request, CancellationToken ct)
    {
        // CuttingSheet.Create needs CuttingLine instances; we create temp ones for the factory
        var tempId = Guid.NewGuid();
        var lines = request.Lines.Select(l =>
            CuttingLine.Create(tempId, l.PartName, l.MaterialType, l.WidthMm, l.HeightMm, l.ThicknessMm, l.Quantity, l.Notes));

        var sheet = CuttingSheet.Create(request.TenantId, request.OrderReference, lines);
        sheet.Submit();

        sheet.PopDomainEvents();

        await _repository.AddCuttingSheetAsync(sheet, ct).ConfigureAwait(false);
        await _repository.SaveChangesAsync(ct).ConfigureAwait(false);

        return Result<Guid>.Success(sheet.Id);
    }
}
