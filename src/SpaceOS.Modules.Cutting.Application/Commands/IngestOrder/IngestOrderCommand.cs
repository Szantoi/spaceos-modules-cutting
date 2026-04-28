using Ardalis.Result;
using MediatR;
using SpaceOS.Modules.Cutting.Domain.Enums;

namespace SpaceOS.Modules.Cutting.Application.Commands.IngestOrder;

public sealed record IngestOrderCommand(
    Guid OrderId,
    Guid TenantId,
    IReadOnlyList<IngestOrderItem> Items
) : IRequest<Result<int>>;

public sealed record IngestOrderItem(
    string Name,
    decimal WidthMm,
    decimal HeightMm,
    string Material,
    GrainDirection GrainDirection,
    int Quantity
);
