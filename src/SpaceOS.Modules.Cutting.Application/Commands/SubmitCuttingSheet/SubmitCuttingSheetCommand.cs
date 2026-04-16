using Ardalis.Result;
using MediatR;

namespace SpaceOS.Modules.Cutting.Application.Commands.SubmitCuttingSheet;

public sealed record CuttingLineInput(
    string PartName,
    string MaterialType,
    decimal WidthMm,
    decimal HeightMm,
    decimal ThicknessMm,
    int Quantity,
    string? Notes);

public sealed record SubmitCuttingSheetCommand(
    Guid TenantId,
    string OrderReference,
    IReadOnlyList<CuttingLineInput> Lines) : IRequest<Result<Guid>>;
