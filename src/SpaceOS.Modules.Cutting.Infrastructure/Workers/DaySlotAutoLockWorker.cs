using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SpaceOS.Modules.Cutting.Domain.Interfaces;

namespace SpaceOS.Modules.Cutting.Infrastructure.Workers;

/// <summary>
/// Background service that automatically locks past Open DaySlots.
/// Runs every 15 minutes and locks any slot whose SlotDate is before today.
/// Only Open slots are affected — Locked and Closed slots are left untouched.
/// </summary>
public sealed class DaySlotAutoLockWorker : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromMinutes(15);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<DaySlotAutoLockWorker> _logger;

    public DaySlotAutoLockWorker(
        IServiceScopeFactory scopeFactory,
        ILogger<DaySlotAutoLockWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await LockPastSlotsAsync(stoppingToken).ConfigureAwait(false);
            await Task.Delay(Interval, stoppingToken).ConfigureAwait(false);
        }
    }

    private async Task LockPastSlotsAsync(CancellationToken ct)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var repo = scope.ServiceProvider.GetRequiredService<ICuttingRepository>();

        var today = DateOnly.FromDateTime(DateTime.Today);
        var slots = await repo.GetOpenSlotsBeforeDateAsync(today, ct).ConfigureAwait(false);

        int locked = 0;
        int errors = 0;

        foreach (var slot in slots)
        {
            var result = slot.Lock();
            if (result.IsSuccess)
            {
                locked++;
            }
            else
            {
                errors++;
                _logger.LogWarning(
                    "DaySlotAutoLockWorker: failed to lock DaySlot {SlotId} (date={SlotDate}): {Errors}",
                    slot.Id, slot.SlotDate, string.Join("; ", result.Errors));
            }
        }

        if (locked > 0 || errors > 0)
        {
            await repo.SaveChangesAsync(ct).ConfigureAwait(false);
        }

        _logger.LogInformation(
            "DaySlotAutoLockWorker: run complete — {Locked} slot(s) locked, {Errors} error(s).",
            locked, errors);
    }
}
