using FluentAssertions;
using Moq;
using SpaceOS.Modules.Cutting.Analytics.Application.Queries;
using SpaceOS.Modules.Cutting.Analytics.Domain.Aggregates;
using SpaceOS.Modules.Cutting.Analytics.Domain.Interfaces;
using SpaceOS.Modules.Cutting.Analytics.Domain.ReadModels;
using SpaceOS.Modules.Cutting.Analytics.Domain.ValueObjects;
using Xunit;

namespace SpaceOS.Modules.Cutting.Tests.Analytics.Application;

public class QueryHandlerTests
{
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly DateOnly From = new(2026, 4, 1);
    private static readonly DateOnly To = new(2026, 4, 30);

    // ── GetDailyExecutionMetricsQueryHandler ─────────────────────────────────

    public class GetDailyExecutionMetricsQueryHandlerTests
    {
        private readonly Mock<IAnalyticsQueryRepository> _repo = new();
        private readonly GetDailyExecutionMetricsQueryHandler _sut;

        public GetDailyExecutionMetricsQueryHandlerTests()
            => _sut = new GetDailyExecutionMetricsQueryHandler(_repo.Object);

        [Fact]
        public async Task Handle_ValidQuery_ReturnsPagedResult()
        {
            var metric = DailyExecutionMetric.Create(TenantId, "M1", From, 5, 30m, 80m);
            _repo.Setup(r => r.GetExecutionMetricsAsync(TenantId, null, From, To, 0, 100, default))
                .ReturnsAsync(new[] { metric });

            var result = await _sut.Handle(
                new GetDailyExecutionMetricsQuery(TenantId, null, From, To), default);

            result.IsSuccess.Should().BeTrue();
            result.Value.Items.Should().HaveCount(1);
        }

        [Fact]
        public async Task Handle_EmptyTenantId_ReturnsInvalid()
        {
            var result = await _sut.Handle(
                new GetDailyExecutionMetricsQuery(Guid.Empty, null, From, To), default);
            result.IsSuccess.Should().BeFalse();
        }

        [Fact]
        public async Task Handle_FromAfterTo_ReturnsInvalid()
        {
            var result = await _sut.Handle(
                new GetDailyExecutionMetricsQuery(TenantId, null, To, From), default);
            result.IsSuccess.Should().BeFalse();
        }

        [Fact]
        public async Task Handle_TakeOver500_ClampedTo500()
        {
            _repo.Setup(r => r.GetExecutionMetricsAsync(TenantId, null, From, To, 0, 500, default))
                .ReturnsAsync(Array.Empty<DailyExecutionMetric>());

            var result = await _sut.Handle(
                new GetDailyExecutionMetricsQuery(TenantId, null, From, To, 0, 9999), default);

            result.IsSuccess.Should().BeTrue();
            _repo.Verify(r => r.GetExecutionMetricsAsync(TenantId, null, From, To, 0, 500, default), Times.Once);
        }

        [Fact]
        public async Task Handle_MachineIdFilter_PassedToRepo()
        {
            _repo.Setup(r => r.GetExecutionMetricsAsync(TenantId, "M2", From, To, 0, 100, default))
                .ReturnsAsync(Array.Empty<DailyExecutionMetric>());

            await _sut.Handle(new GetDailyExecutionMetricsQuery(TenantId, "M2", From, To), default);

            _repo.Verify(r => r.GetExecutionMetricsAsync(TenantId, "M2", From, To, 0, 100, default), Times.Once);
        }
    }

    // ── GetMaterialUsageQueryHandler ─────────────────────────────────────────

    public class GetMaterialUsageQueryHandlerTests
    {
        private readonly Mock<IAnalyticsQueryRepository> _repo = new();
        private readonly GetMaterialUsageQueryHandler _sut;

        public GetMaterialUsageQueryHandlerTests()
            => _sut = new GetMaterialUsageQueryHandler(_repo.Object);

        [Fact]
        public async Task Handle_ValidQuery_ReturnsPagedResult()
        {
            var usage = DailyMaterialUsage.Create(TenantId, "MDF-18", From, 1000m, 100m, 2);
            _repo.Setup(r => r.GetMaterialUsageAsync(TenantId, null, From, To, 0, 100, default))
                .ReturnsAsync(new[] { usage });

            var result = await _sut.Handle(
                new GetMaterialUsageQuery(TenantId, null, From, To), default);

            result.IsSuccess.Should().BeTrue();
        }

        [Fact]
        public async Task Handle_EmptyTenantId_ReturnsInvalid()
        {
            var result = await _sut.Handle(
                new GetMaterialUsageQuery(Guid.Empty, null, From, To), default);
            result.IsSuccess.Should().BeFalse();
        }

        [Fact]
        public async Task Handle_FromAfterTo_ReturnsInvalid()
        {
            var result = await _sut.Handle(
                new GetMaterialUsageQuery(TenantId, null, To, From), default);
            result.IsSuccess.Should().BeFalse();
        }

        [Fact]
        public async Task Handle_MaterialCodeFilter_PassedToRepo()
        {
            _repo.Setup(r => r.GetMaterialUsageAsync(TenantId, "HDF-3", From, To, 0, 100, default))
                .ReturnsAsync(Array.Empty<DailyMaterialUsage>());

            await _sut.Handle(new GetMaterialUsageQuery(TenantId, "HDF-3", From, To), default);

            _repo.Verify(r => r.GetMaterialUsageAsync(TenantId, "HDF-3", From, To, 0, 100, default), Times.Once);
        }
    }

    // ── GetMachineOEEQueryHandler ─────────────────────────────────────────────

    public class GetMachineOEEQueryHandlerTests
    {
        private readonly Mock<IAnalyticsQueryRepository> _repo = new();
        private readonly GetMachineOEEQueryHandler _sut;
        private static readonly DateTime DtFrom = new(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc);
        private static readonly DateTime DtTo = new(2026, 4, 30, 23, 0, 0, DateTimeKind.Utc);

        public GetMachineOEEQueryHandlerTests()
            => _sut = new GetMachineOEEQueryHandler(_repo.Object);

        [Fact]
        public async Task Handle_ValidQuery_ReturnsPagedResult()
        {
            _repo.Setup(r => r.GetOEEAsync(TenantId, null, DtFrom, DtTo, 0, 100, default))
                .ReturnsAsync(Array.Empty<MachineOEEHourly>());

            var result = await _sut.Handle(
                new GetMachineOEEQuery(TenantId, null, DtFrom, DtTo), default);

            result.IsSuccess.Should().BeTrue();
        }

        [Fact]
        public async Task Handle_EmptyTenantId_ReturnsInvalid()
        {
            var result = await _sut.Handle(
                new GetMachineOEEQuery(Guid.Empty, null, DtFrom, DtTo), default);
            result.IsSuccess.Should().BeFalse();
        }

        [Fact]
        public async Task Handle_FromEqualsTo_ReturnsInvalid()
        {
            var result = await _sut.Handle(
                new GetMachineOEEQuery(TenantId, null, DtFrom, DtFrom), default);
            result.IsSuccess.Should().BeFalse();
        }

        [Fact]
        public async Task Handle_MachineIdFilter_PassedToRepo()
        {
            _repo.Setup(r => r.GetOEEAsync(TenantId, "LASER-1", DtFrom, DtTo, 0, 100, default))
                .ReturnsAsync(Array.Empty<MachineOEEHourly>());

            await _sut.Handle(new GetMachineOEEQuery(TenantId, "LASER-1", DtFrom, DtTo), default);

            _repo.Verify(r => r.GetOEEAsync(TenantId, "LASER-1", DtFrom, DtTo, 0, 100, default), Times.Once);
        }
    }

    // ── GetOperatorMetricsQueryHandler ───────────────────────────────────────

    public class GetOperatorMetricsQueryHandlerTests
    {
        private readonly Mock<IAnalyticsQueryRepository> _repo = new();
        private readonly GetOperatorMetricsQueryHandler _sut;

        public GetOperatorMetricsQueryHandlerTests()
            => _sut = new GetOperatorMetricsQueryHandler(_repo.Object);

        [Fact]
        public async Task Handle_ValidQuery_ReturnsPagedResult()
        {
            _repo.Setup(r => r.GetOperatorMetricsAnonymizedAsync(
                TenantId, From, To, It.IsAny<AnonymizationPolicy>(), 0, 100, default))
                .ReturnsAsync(Array.Empty<DailyOperatorMetric>());

            var result = await _sut.Handle(
                new GetOperatorMetricsQuery(TenantId, From, To), default);

            result.IsSuccess.Should().BeTrue();
        }

        [Fact]
        public async Task Handle_EmptyTenantId_ReturnsInvalid()
        {
            var result = await _sut.Handle(
                new GetOperatorMetricsQuery(Guid.Empty, From, To), default);
            result.IsSuccess.Should().BeFalse();
        }

        [Fact]
        public async Task Handle_DefaultAnonymizationPolicyUsed()
        {
            _repo.Setup(r => r.GetOperatorMetricsAnonymizedAsync(
                TenantId, From, To, It.IsAny<AnonymizationPolicy>(), 0, 100, default))
                .ReturnsAsync(Array.Empty<DailyOperatorMetric>());

            await _sut.Handle(new GetOperatorMetricsQuery(TenantId, From, To), default);

            _repo.Verify(r => r.GetOperatorMetricsAnonymizedAsync(
                TenantId, From, To, It.IsAny<AnonymizationPolicy>(), 0, 100, default), Times.Once);
        }
    }

    // ── GetRebuildJobStatusQueryHandler ──────────────────────────────────────

    public class GetRebuildJobStatusQueryHandlerTests
    {
        private readonly Mock<IAnalyticsQueryRepository> _repo = new();
        private readonly GetRebuildJobStatusQueryHandler _sut;

        public GetRebuildJobStatusQueryHandlerTests()
            => _sut = new GetRebuildJobStatusQueryHandler(_repo.Object);

        [Fact]
        public async Task Handle_ExistingJobSameTenant_ReturnsSuccess()
        {
            var job = AnalyticsRebuildJob.Create(TenantId);
            _repo.Setup(r => r.GetRebuildJobAsync(job.Id, default)).ReturnsAsync(job);

            var result = await _sut.Handle(
                new GetRebuildJobStatusQuery(TenantId, job.Id), default);

            result.IsSuccess.Should().BeTrue();
            result.Value.Should().Be(job);
        }

        [Fact]
        public async Task Handle_JobNotFound_ReturnsNotFound()
        {
            _repo.Setup(r => r.GetRebuildJobAsync(It.IsAny<Guid>(), default))
                .ReturnsAsync((AnalyticsRebuildJob?)null);

            var result = await _sut.Handle(
                new GetRebuildJobStatusQuery(TenantId, Guid.NewGuid()), default);

            result.Status.Should().Be(Ardalis.Result.ResultStatus.NotFound);
        }

        [Fact]
        public async Task Handle_JobBelongsToDifferentTenant_ReturnsForbidden()
        {
            var otherTenant = Guid.NewGuid();
            var job = AnalyticsRebuildJob.Create(otherTenant);
            _repo.Setup(r => r.GetRebuildJobAsync(job.Id, default)).ReturnsAsync(job);

            var result = await _sut.Handle(
                new GetRebuildJobStatusQuery(TenantId, job.Id), default);

            result.Status.Should().Be(Ardalis.Result.ResultStatus.Forbidden);
        }

        [Fact]
        public async Task Handle_EmptyTenantId_ReturnsInvalid()
        {
            var result = await _sut.Handle(
                new GetRebuildJobStatusQuery(Guid.Empty, Guid.NewGuid()), default);
            result.IsSuccess.Should().BeFalse();
        }

        [Fact]
        public async Task Handle_EmptyJobId_ReturnsInvalid()
        {
            var result = await _sut.Handle(
                new GetRebuildJobStatusQuery(TenantId, Guid.Empty), default);
            result.IsSuccess.Should().BeFalse();
        }
    }
}
