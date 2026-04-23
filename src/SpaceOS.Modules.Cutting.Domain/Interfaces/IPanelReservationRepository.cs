using SpaceOS.Modules.Cutting.Domain.Aggregates;

namespace SpaceOS.Modules.Cutting.Domain.Interfaces;

public interface IPanelReservationRepository
{
    Task<IReadOnlyList<PanelReservation>> GetByPlanAsync(Guid planId, CancellationToken ct = default);
    Task AddAsync(PanelReservation reservation, CancellationToken ct = default);
    Task UpdateAsync(PanelReservation reservation, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
