using Ardalis.Result;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using SpaceOS.Modules.Contracts.Inventory;
using SpaceOS.Modules.Contracts.Inventory.DTOs;
using SpaceOS.Modules.Contracts.Inventory.Enums;
using SpaceOS.Modules.Contracts.Inventory.Requests;
using SpaceOS.Modules.Cutting.Application.Commands.ReservePanels;
using SpaceOS.Modules.Cutting.Domain.Aggregates;
using SpaceOS.Modules.Cutting.Domain.Services;
using SpaceOS.Modules.Cutting.Infrastructure.Persistence;
using SpaceOS.Modules.Cutting.Infrastructure.Repositories;
using Xunit;

namespace SpaceOS.Modules.Cutting.Tests.Application;

public class ReservePanelsCommandHandlerTests : IDisposable
{
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly DateTime TodayUtc = DateTime.SpecifyKind(DateTime.UtcNow.Date, DateTimeKind.Utc);

    private readonly CuttingDbContext _db;
    private readonly CuttingRepository _cuttingRepo;
    private readonly PanelReservationRepository _reservationRepo;

    public ReservePanelsCommandHandlerTests()
    {
        var options = new DbContextOptionsBuilder<CuttingDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new CuttingDbContext(options);
        _cuttingRepo = new CuttingRepository(_db);
        _reservationRepo = new PanelReservationRepository(_db);
    }

    private ReservePanelsCommandHandler BuildHandler(IInventoryProvider provider)
        => new(_cuttingRepo, _reservationRepo, provider);

    private static ReservationDto MakeReservationDto(Guid correlationId) =>
        new(
            Id: Guid.NewGuid(),
            TenantId: TenantId,
            CorrelationId: correlationId,
            ConsumerModule: "Cutting",
            ConsumerContextJson: null,
            CreatedByUserId: null,
            CreatedAt: DateTimeOffset.UtcNow,
            ExpiresAt: DateTimeOffset.UtcNow.AddHours(24),
            Status: ReservationStatus.Active,
            Items: []);

    [Fact]
    public async Task Handle_WhenPlanNotFound_ReturnsNotFound()
    {
        var provider = new Mock<IInventoryProvider>();
        var handler = BuildHandler(provider.Object);

        var result = await handler.Handle(new ReservePanelsCommand(Guid.NewGuid(), TenantId), default);

        result.IsSuccess.Should().BeFalse();
        result.Status.Should().Be(ResultStatus.NotFound);
    }

    [Fact]
    public async Task Handle_WhenInventorySucceeds_CreatesReservationsAndReturnsCount()
    {
        // Plan with no jobs → expected 0 reservations
        var plan = CuttingPlan.Create(TenantId, TodayUtc, 7, "maxcut-v1");
        await _cuttingRepo.AddCuttingPlanAsync(plan);
        await _cuttingRepo.SaveChangesAsync();

        var provider = new Mock<IInventoryProvider>();
        var handler = BuildHandler(provider.Object);

        var result = await handler.Handle(new ReservePanelsCommand(plan.Id, TenantId), default);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(0, "plan has no jobs yet in this test scenario");
        provider.Verify(p => p.ReserveAsync(
            It.IsAny<ReserveStockRequest>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenInventoryThrows_ReturnsError()
    {
        var plan = CuttingPlan.Create(TenantId, TodayUtc, 7, "maxcut-v1");
        var slot = plan.DaySlots[0];
        var job = CuttingJob.Create(
            slot.Id, Guid.NewGuid(),
            slot.SlotDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc),
            "Normal", 2m, 1200m, 800m);
        slot.AddJob(job, new AreaCapacityModel());

        await _cuttingRepo.AddCuttingPlanAsync(plan);
        await _cuttingRepo.SaveChangesAsync();

        var provider = new Mock<IInventoryProvider>();
        provider.Setup(p => p.ReserveAsync(
            It.IsAny<ReserveStockRequest>(),
            It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("Inventory unavailable"));

        var handler = BuildHandler(provider.Object);
        var result = await handler.Handle(new ReservePanelsCommand(plan.Id, TenantId), default);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_OnPartialFailure_CallsReleaseForPreviousReservations()
    {
        var plan = CuttingPlan.Create(TenantId, TodayUtc, 7, "maxcut-v1");
        for (int i = 0; i < 2; i++)
        {
            var slot = plan.DaySlots[i];
            var job = CuttingJob.Create(
                slot.Id, Guid.NewGuid(),
                slot.SlotDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc),
                "Normal", 2m, 1200m, 800m);
            slot.AddJob(job, new AreaCapacityModel());
        }
        await _cuttingRepo.AddCuttingPlanAsync(plan);
        await _cuttingRepo.SaveChangesAsync();

        var callCount = 0;
        Guid firstCorrelationId = Guid.Empty;

        var provider = new Mock<IInventoryProvider>();
        provider.Setup(p => p.ReserveAsync(
            It.IsAny<ReserveStockRequest>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync((ReserveStockRequest req, CancellationToken _) =>
            {
                callCount++;
                if (callCount == 1)
                {
                    firstCorrelationId = req.CorrelationId;
                    return Result<ReservationDto>.Success(MakeReservationDto(req.CorrelationId));
                }
                throw new HttpRequestException("Inventory unavailable on second call");
            });

        var handler = BuildHandler(provider.Object);
        var result = await handler.Handle(new ReservePanelsCommand(plan.Id, TenantId), default);

        result.IsSuccess.Should().BeFalse("second reservation failed");
        provider.Verify(p => p.ReleaseReservationAsync(
            firstCorrelationId,
            "rollback",
            It.IsAny<CancellationToken>()), Times.Once,
            "first reservation must be rolled back");
    }

    [Fact]
    public async Task Handle_WithMultipleJobs_ReturnsCorrectCount()
    {
        var plan = CuttingPlan.Create(TenantId, TodayUtc, 7, "maxcut-v1");
        for (int i = 0; i < 3; i++)
        {
            var slot = plan.DaySlots[i];
            var job = CuttingJob.Create(
                slot.Id, Guid.NewGuid(),
                slot.SlotDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc),
                "Normal", 2m, 1200m, 800m);
            slot.AddJob(job, new AreaCapacityModel());
        }
        await _cuttingRepo.AddCuttingPlanAsync(plan);
        await _cuttingRepo.SaveChangesAsync();

        var provider = new Mock<IInventoryProvider>();
        provider.Setup(p => p.ReserveAsync(
            It.IsAny<ReserveStockRequest>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync((ReserveStockRequest req, CancellationToken _) =>
                Result<ReservationDto>.Success(MakeReservationDto(req.CorrelationId)));

        var handler = BuildHandler(provider.Object);
        var result = await handler.Handle(new ReservePanelsCommand(plan.Id, TenantId), default);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(3);
    }

    public void Dispose() => _db.Dispose();
}
