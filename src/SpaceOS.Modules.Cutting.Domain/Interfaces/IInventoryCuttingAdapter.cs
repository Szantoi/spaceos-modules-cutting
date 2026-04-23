using Ardalis.Result;

namespace SpaceOS.Modules.Cutting.Domain.Interfaces;

public interface IInventoryCuttingAdapter
{
    Task<Result> RegisterOffcutsAsync(
        Guid planId,
        Guid tenantId,
        IReadOnlyList<OffcutRegistrationItem> items,
        CancellationToken ct);
}

public sealed record OffcutRegistrationItem(
    string MaterialCode,
    decimal WidthMm,
    decimal HeightMm,
    decimal X,
    decimal Y
);
