using Microsoft.EntityFrameworkCore;
using SpaceOS.Modules.Cutting.Domain.Aggregates;
using SpaceOS.Modules.Cutting.Domain.Interfaces;
using SpaceOS.Modules.Cutting.Infrastructure.Persistence;

namespace SpaceOS.Modules.Cutting.Infrastructure.Repositories;

public class PanelReservationRepository : IPanelReservationRepository
{
    private readonly CuttingDbContext _db;

    public PanelReservationRepository(CuttingDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<PanelReservation>> GetByPlanAsync(Guid planId, CancellationToken ct = default)
        => await _db.PanelReservations.AsNoTracking()
            .Where(r => r.CuttingPlanId == planId)
            .ToListAsync(ct)
            .ConfigureAwait(false);

    public async Task AddAsync(PanelReservation reservation, CancellationToken ct = default)
        => await _db.PanelReservations.AddAsync(reservation, ct).ConfigureAwait(false);

    public Task UpdateAsync(PanelReservation reservation, CancellationToken ct = default)
    {
        _db.PanelReservations.Update(reservation);
        return Task.CompletedTask;
    }

    public async Task SaveChangesAsync(CancellationToken ct = default)
        => await _db.SaveChangesAsync(ct).ConfigureAwait(false);
}
