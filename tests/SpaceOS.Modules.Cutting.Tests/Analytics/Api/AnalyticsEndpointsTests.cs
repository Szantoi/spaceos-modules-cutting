using System.Net;
using System.Net.Http.Json;
using Ardalis.Result;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using SpaceOS.Modules.Cutting.Analytics.Application.Queries;
using SpaceOS.Modules.Cutting.Analytics.Domain.Aggregates;
using SpaceOS.Modules.Cutting.Analytics.Domain.Common;
using SpaceOS.Modules.Cutting.Analytics.Domain.Interfaces;
using SpaceOS.Modules.Cutting.Analytics.Domain.ReadModels;
using SpaceOS.Modules.Cutting.Api.Endpoints;
using OEEHourly = SpaceOS.Modules.Cutting.Analytics.Domain.ReadModels.MachineOEEHourly;
using SpaceOS.Modules.Cutting.Tests.Api;
using Xunit;

namespace SpaceOS.Modules.Cutting.Tests.Analytics.Api;

/// <summary>
/// Integration-style unit tests for AnalyticsEndpoints using TestServer.
/// Mocks ISender and IRebuildJobRepository — no real DB or PostgreSQL required.
/// </summary>
public class AnalyticsEndpointsTests
{
    private static readonly Guid TenantId = Guid.NewGuid();

    private static AnalyticsPagedResult<T> EmptyPage<T>()
        => new(Array.Empty<T>(), 0, 0, 10);

    private HttpClient CreateClient(
        Mock<IMediator>? mediatorMock = null,
        Mock<IRebuildJobRepository>? repoMock = null)
    {
        mediatorMock ??= new Mock<IMediator>();
        repoMock ??= new Mock<IRebuildJobRepository>();

        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddSingleton<ISender>(mediatorMock.Object);
        builder.Services.AddSingleton<IMediator>(mediatorMock.Object);
        builder.Services.AddSingleton(repoMock.Object);
        builder.Services.AddAuthentication("Test")
            .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>("Test", _ => { });
        builder.Services.AddAuthorization(opts =>
            opts.AddPolicy("ManufacturerOnly", p => p.RequireAuthenticatedUser()));
        builder.Services.AddRouting();

        var app = builder.Build();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapAnalyticsEndpoints();
        app.StartAsync().GetAwaiter().GetResult();

        var testServer = app.Services.GetRequiredService<IServer>() as TestServer;
        var client = testServer!.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Test");
        return client;
    }

    // ══════════════════════════════════════════════════════════════════
    //  GetExecutionMetrics (5 tests)
    // ══════════════════════════════════════════════════════════════════

    // 1. Valid query → 200 OK
    [Fact]
    public async Task GetExecutionMetrics_ValidQuery_Returns200()
    {
        var sender = new Mock<IMediator>();
        sender.Setup(s => s.Send(It.IsAny<GetDailyExecutionMetricsQuery>(), It.IsAny<CancellationToken>()))
              .ReturnsAsync(Result<AnalyticsPagedResult<DailyExecutionMetric>>.Success(EmptyPage<DailyExecutionMetric>()));

        var client = CreateClient(sender);
        var resp = await client.GetAsync($"/api/cutting/analytics/execution-metrics?tenantId={TenantId}");

        resp.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // 2. Sender returns Invalid → 400
    [Fact]
    public async Task GetExecutionMetrics_InvalidResult_Returns400()
    {
        var sender = new Mock<IMediator>();
        sender.Setup(s => s.Send(It.IsAny<GetDailyExecutionMetricsQuery>(), It.IsAny<CancellationToken>()))
              .ReturnsAsync(Result<AnalyticsPagedResult<DailyExecutionMetric>>.Invalid(new ValidationError("bad")));

        var client = CreateClient(sender);
        var resp = await client.GetAsync($"/api/cutting/analytics/execution-metrics?tenantId={TenantId}");

        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // 3. Default date range applied when not specified
    [Fact]
    public async Task GetExecutionMetrics_NoDatesSpecified_SenderCalledWithDefaultRange()
    {
        GetDailyExecutionMetricsQuery? captured = null;
        var sender = new Mock<IMediator>();
        sender.Setup(s => s.Send(It.IsAny<GetDailyExecutionMetricsQuery>(), It.IsAny<CancellationToken>()))
              .Callback<IRequest<Result<AnalyticsPagedResult<DailyExecutionMetric>>>, CancellationToken>((q, _) => captured = (GetDailyExecutionMetricsQuery)q)
              .ReturnsAsync(Result<AnalyticsPagedResult<DailyExecutionMetric>>.Success(EmptyPage<DailyExecutionMetric>()));

        var client = CreateClient(sender);
        await client.GetAsync($"/api/cutting/analytics/execution-metrics?tenantId={TenantId}");

        captured.Should().NotBeNull();
        var days = captured!.To.DayNumber - captured.From.DayNumber;
        days.Should().BeCloseTo(30, 2);
    }

    // 4. MachineId filter passed to query
    [Fact]
    public async Task GetExecutionMetrics_MachineIdQueryParam_PassedToQuery()
    {
        GetDailyExecutionMetricsQuery? captured = null;
        var sender = new Mock<IMediator>();
        sender.Setup(s => s.Send(It.IsAny<GetDailyExecutionMetricsQuery>(), It.IsAny<CancellationToken>()))
              .Callback<IRequest<Result<AnalyticsPagedResult<DailyExecutionMetric>>>, CancellationToken>((q, _) => captured = (GetDailyExecutionMetricsQuery)q)
              .ReturnsAsync(Result<AnalyticsPagedResult<DailyExecutionMetric>>.Success(EmptyPage<DailyExecutionMetric>()));

        var client = CreateClient(sender);
        await client.GetAsync($"/api/cutting/analytics/execution-metrics?tenantId={TenantId}&machineId=CNC-1");

        captured!.MachineId.Should().Be("CNC-1");
    }

    // 5. Skip/take query params passed
    [Fact]
    public async Task GetExecutionMetrics_SkipTakeParams_PassedToQuery()
    {
        GetDailyExecutionMetricsQuery? captured = null;
        var sender = new Mock<IMediator>();
        sender.Setup(s => s.Send(It.IsAny<GetDailyExecutionMetricsQuery>(), It.IsAny<CancellationToken>()))
              .Callback<IRequest<Result<AnalyticsPagedResult<DailyExecutionMetric>>>, CancellationToken>((q, _) => captured = (GetDailyExecutionMetricsQuery)q)
              .ReturnsAsync(Result<AnalyticsPagedResult<DailyExecutionMetric>>.Success(EmptyPage<DailyExecutionMetric>()));

        var client = CreateClient(sender);
        await client.GetAsync($"/api/cutting/analytics/execution-metrics?tenantId={TenantId}&skip=20&take=50");

        captured!.Skip.Should().Be(20);
        captured.Take.Should().Be(50);
    }

    // ══════════════════════════════════════════════════════════════════
    //  GetMaterialUsage (4 tests)
    // ══════════════════════════════════════════════════════════════════

    // 6. Valid → 200
    [Fact]
    public async Task GetMaterialUsage_ValidQuery_Returns200()
    {
        var sender = new Mock<IMediator>();
        sender.Setup(s => s.Send(It.IsAny<GetMaterialUsageQuery>(), It.IsAny<CancellationToken>()))
              .ReturnsAsync(Result<AnalyticsPagedResult<DailyMaterialUsage>>.Success(EmptyPage<DailyMaterialUsage>()));

        var client = CreateClient(sender);
        var resp = await client.GetAsync($"/api/cutting/analytics/material-usage?tenantId={TenantId}");

        resp.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // 7. Invalid → 400
    [Fact]
    public async Task GetMaterialUsage_InvalidResult_Returns400()
    {
        var sender = new Mock<IMediator>();
        sender.Setup(s => s.Send(It.IsAny<GetMaterialUsageQuery>(), It.IsAny<CancellationToken>()))
              .ReturnsAsync(Result<AnalyticsPagedResult<DailyMaterialUsage>>.Invalid(new ValidationError("bad")));

        var client = CreateClient(sender);
        var resp = await client.GetAsync($"/api/cutting/analytics/material-usage?tenantId={TenantId}");

        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // 8. MaterialCode filter
    [Fact]
    public async Task GetMaterialUsage_MaterialCodeParam_PassedToQuery()
    {
        GetMaterialUsageQuery? captured = null;
        var sender = new Mock<IMediator>();
        sender.Setup(s => s.Send(It.IsAny<GetMaterialUsageQuery>(), It.IsAny<CancellationToken>()))
              .Callback<IRequest<Result<AnalyticsPagedResult<DailyMaterialUsage>>>, CancellationToken>((q, _) => captured = (GetMaterialUsageQuery)q)
              .ReturnsAsync(Result<AnalyticsPagedResult<DailyMaterialUsage>>.Success(EmptyPage<DailyMaterialUsage>()));

        var client = CreateClient(sender);
        await client.GetAsync($"/api/cutting/analytics/material-usage?tenantId={TenantId}&materialCode=MDF-18");

        captured!.MaterialCode.Should().Be("MDF-18");
    }

    // 9. Default date range (30 days)
    [Fact]
    public async Task GetMaterialUsage_NoDates_DefaultsTo30Days()
    {
        GetMaterialUsageQuery? captured = null;
        var sender = new Mock<IMediator>();
        sender.Setup(s => s.Send(It.IsAny<GetMaterialUsageQuery>(), It.IsAny<CancellationToken>()))
              .Callback<IRequest<Result<AnalyticsPagedResult<DailyMaterialUsage>>>, CancellationToken>((q, _) => captured = (GetMaterialUsageQuery)q)
              .ReturnsAsync(Result<AnalyticsPagedResult<DailyMaterialUsage>>.Success(EmptyPage<DailyMaterialUsage>()));

        var client = CreateClient(sender);
        await client.GetAsync($"/api/cutting/analytics/material-usage?tenantId={TenantId}");

        var days = captured!.To.DayNumber - captured.From.DayNumber;
        days.Should().BeCloseTo(30, 2);
    }

    // ══════════════════════════════════════════════════════════════════
    //  GetOEE (4 tests)
    // ══════════════════════════════════════════════════════════════════

    // 10. Valid → 200
    [Fact]
    public async Task GetOEE_ValidQuery_Returns200()
    {
        var sender = new Mock<IMediator>();
        sender.Setup(s => s.Send(It.IsAny<GetMachineOEEQuery>(), It.IsAny<CancellationToken>()))
              .ReturnsAsync(Result<AnalyticsPagedResult<OEEHourly>>.Success(EmptyPage<OEEHourly>()));

        var client = CreateClient(sender);
        var resp = await client.GetAsync($"/api/cutting/analytics/oee?tenantId={TenantId}");

        resp.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // 11. Invalid → 400
    [Fact]
    public async Task GetOEE_InvalidResult_Returns400()
    {
        var sender = new Mock<IMediator>();
        sender.Setup(s => s.Send(It.IsAny<GetMachineOEEQuery>(), It.IsAny<CancellationToken>()))
              .ReturnsAsync(Result<AnalyticsPagedResult<OEEHourly>>.Invalid(new ValidationError("bad")));

        var client = CreateClient(sender);
        var resp = await client.GetAsync($"/api/cutting/analytics/oee?tenantId={TenantId}");

        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // 12. MachineId filter passed
    [Fact]
    public async Task GetOEE_MachineIdParam_PassedToQuery()
    {
        GetMachineOEEQuery? captured = null;
        var sender = new Mock<IMediator>();
        sender.Setup(s => s.Send(It.IsAny<GetMachineOEEQuery>(), It.IsAny<CancellationToken>()))
              .Callback<IRequest<Result<AnalyticsPagedResult<OEEHourly>>>, CancellationToken>((q, _) => captured = (GetMachineOEEQuery)q)
              .ReturnsAsync(Result<AnalyticsPagedResult<OEEHourly>>.Success(EmptyPage<OEEHourly>()));

        var client = CreateClient(sender);
        await client.GetAsync($"/api/cutting/analytics/oee?tenantId={TenantId}&machineId=CNC-2");

        captured!.MachineId.Should().Be("CNC-2");
    }

    // 13. Default date range (7 days)
    [Fact]
    public async Task GetOEE_NoDates_DefaultsTo7Days()
    {
        GetMachineOEEQuery? captured = null;
        var sender = new Mock<IMediator>();
        sender.Setup(s => s.Send(It.IsAny<GetMachineOEEQuery>(), It.IsAny<CancellationToken>()))
              .Callback<IRequest<Result<AnalyticsPagedResult<OEEHourly>>>, CancellationToken>((q, _) => captured = (GetMachineOEEQuery)q)
              .ReturnsAsync(Result<AnalyticsPagedResult<OEEHourly>>.Success(EmptyPage<OEEHourly>()));

        var client = CreateClient(sender);
        await client.GetAsync($"/api/cutting/analytics/oee?tenantId={TenantId}");

        var days = (captured!.To - captured.From).TotalDays;
        days.Should().BeApproximately(7, 1);
    }

    // ══════════════════════════════════════════════════════════════════
    //  GetOperatorMetrics (3 tests)
    // ══════════════════════════════════════════════════════════════════

    // 14. Valid → 200
    [Fact]
    public async Task GetOperatorMetrics_ValidQuery_Returns200()
    {
        var sender = new Mock<IMediator>();
        sender.Setup(s => s.Send(It.IsAny<GetOperatorMetricsQuery>(), It.IsAny<CancellationToken>()))
              .ReturnsAsync(Result<AnalyticsPagedResult<DailyOperatorMetric>>.Success(EmptyPage<DailyOperatorMetric>()));

        var client = CreateClient(sender);
        var resp = await client.GetAsync($"/api/cutting/analytics/operator-metrics?tenantId={TenantId}");

        resp.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // 15. Invalid → 400
    [Fact]
    public async Task GetOperatorMetrics_InvalidResult_Returns400()
    {
        var sender = new Mock<IMediator>();
        sender.Setup(s => s.Send(It.IsAny<GetOperatorMetricsQuery>(), It.IsAny<CancellationToken>()))
              .ReturnsAsync(Result<AnalyticsPagedResult<DailyOperatorMetric>>.Invalid(new ValidationError("bad")));

        var client = CreateClient(sender);
        var resp = await client.GetAsync($"/api/cutting/analytics/operator-metrics?tenantId={TenantId}");

        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // 16. Default date range (30 days)
    [Fact]
    public async Task GetOperatorMetrics_NoDates_DefaultsTo30Days()
    {
        GetOperatorMetricsQuery? captured = null;
        var sender = new Mock<IMediator>();
        sender.Setup(s => s.Send(It.IsAny<GetOperatorMetricsQuery>(), It.IsAny<CancellationToken>()))
              .Callback<IRequest<Result<AnalyticsPagedResult<DailyOperatorMetric>>>, CancellationToken>((q, _) => captured = (GetOperatorMetricsQuery)q)
              .ReturnsAsync(Result<AnalyticsPagedResult<DailyOperatorMetric>>.Success(EmptyPage<DailyOperatorMetric>()));

        var client = CreateClient(sender);
        await client.GetAsync($"/api/cutting/analytics/operator-metrics?tenantId={TenantId}");

        var days = captured!.To.DayNumber - captured.From.DayNumber;
        days.Should().BeCloseTo(30, 2);
    }

    // ══════════════════════════════════════════════════════════════════
    //  GetRebuildStatus (4 tests)
    // ══════════════════════════════════════════════════════════════════

    // 17. Found → 200
    [Fact]
    public async Task GetRebuildStatus_Found_Returns200()
    {
        var jobId = Guid.NewGuid();
        var job = AnalyticsRebuildJob.Create(TenantId);
        var sender = new Mock<IMediator>();
        sender.Setup(s => s.Send(It.IsAny<GetRebuildJobStatusQuery>(), It.IsAny<CancellationToken>()))
              .ReturnsAsync(Result<AnalyticsRebuildJob>.Success(job));

        var client = CreateClient(sender);
        var resp = await client.GetAsync($"/api/cutting/analytics/rebuild-status?tenantId={TenantId}&jobId={jobId}");

        resp.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // 18. Not found → 404
    [Fact]
    public async Task GetRebuildStatus_NotFound_Returns404()
    {
        var sender = new Mock<IMediator>();
        sender.Setup(s => s.Send(It.IsAny<GetRebuildJobStatusQuery>(), It.IsAny<CancellationToken>()))
              .ReturnsAsync(Result<AnalyticsRebuildJob>.NotFound());

        var client = CreateClient(sender);
        var resp = await client.GetAsync($"/api/cutting/analytics/rebuild-status?tenantId={TenantId}&jobId={Guid.NewGuid()}");

        resp.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // 19. Forbidden (different tenant) → 400
    [Fact]
    public async Task GetRebuildStatus_Forbidden_Returns400()
    {
        var sender = new Mock<IMediator>();
        sender.Setup(s => s.Send(It.IsAny<GetRebuildJobStatusQuery>(), It.IsAny<CancellationToken>()))
              .ReturnsAsync(Result<AnalyticsRebuildJob>.Forbidden());

        var client = CreateClient(sender);
        var resp = await client.GetAsync($"/api/cutting/analytics/rebuild-status?tenantId={TenantId}&jobId={Guid.NewGuid()}");

        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // 20. Invalid → 400
    [Fact]
    public async Task GetRebuildStatus_Invalid_Returns400()
    {
        var sender = new Mock<IMediator>();
        sender.Setup(s => s.Send(It.IsAny<GetRebuildJobStatusQuery>(), It.IsAny<CancellationToken>()))
              .ReturnsAsync(Result<AnalyticsRebuildJob>.Invalid(new ValidationError("err")));

        var client = CreateClient(sender);
        var resp = await client.GetAsync($"/api/cutting/analytics/rebuild-status?tenantId={TenantId}&jobId={Guid.NewGuid()}");

        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ══════════════════════════════════════════════════════════════════
    //  TriggerRebuild (5 tests)
    // ══════════════════════════════════════════════════════════════════

    // 21. No active job → 202 Accepted
    [Fact]
    public async Task TriggerRebuild_NoActiveJob_Returns202()
    {
        var repo = new Mock<IRebuildJobRepository>();
        repo.Setup(r => r.GetActiveForTenantAsync(TenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((AnalyticsRebuildJob?)null);
        repo.Setup(r => r.AddAsync(It.IsAny<AnalyticsRebuildJob>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        repo.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var client = CreateClient(repoMock: repo);
        var resp = await client.PostAsync($"/api/cutting/analytics/rebuild?tenantId={TenantId}", null);

        resp.StatusCode.Should().Be(HttpStatusCode.Accepted);
    }

    // 22. Active Pending job → 409 Conflict
    [Fact]
    public async Task TriggerRebuild_ActivePendingJob_Returns409()
    {
        var repo = new Mock<IRebuildJobRepository>();
        var active = AnalyticsRebuildJob.Create(TenantId);
        repo.Setup(r => r.GetActiveForTenantAsync(TenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(active);

        var client = CreateClient(repoMock: repo);
        var resp = await client.PostAsync($"/api/cutting/analytics/rebuild?tenantId={TenantId}", null);

        resp.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    // 23. Active Running job → 409 Conflict
    [Fact]
    public async Task TriggerRebuild_ActiveRunningJob_Returns409()
    {
        var repo = new Mock<IRebuildJobRepository>();
        var active = AnalyticsRebuildJob.Create(TenantId);
        active.Start(5);
        repo.Setup(r => r.GetActiveForTenantAsync(TenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(active);

        var client = CreateClient(repoMock: repo);
        var resp = await client.PostAsync($"/api/cutting/analytics/rebuild?tenantId={TenantId}", null);

        resp.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    // 24. Job added to repo and saved
    [Fact]
    public async Task TriggerRebuild_NoActiveJob_AddsAndSaves()
    {
        var repo = new Mock<IRebuildJobRepository>();
        repo.Setup(r => r.GetActiveForTenantAsync(TenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((AnalyticsRebuildJob?)null);
        repo.Setup(r => r.AddAsync(It.IsAny<AnalyticsRebuildJob>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        repo.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var client = CreateClient(repoMock: repo);
        await client.PostAsync($"/api/cutting/analytics/rebuild?tenantId={TenantId}", null);

        repo.Verify(r => r.AddAsync(It.IsAny<AnalyticsRebuildJob>(), It.IsAny<CancellationToken>()), Times.Once);
        repo.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    // 25. Response contains jobId
    [Fact]
    public async Task TriggerRebuild_NoActiveJob_ResponseBodyContainsJobId()
    {
        var repo = new Mock<IRebuildJobRepository>();
        repo.Setup(r => r.GetActiveForTenantAsync(TenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((AnalyticsRebuildJob?)null);
        repo.Setup(r => r.AddAsync(It.IsAny<AnalyticsRebuildJob>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        repo.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var client = CreateClient(repoMock: repo);
        var resp = await client.PostAsync($"/api/cutting/analytics/rebuild?tenantId={TenantId}", null);

        var json = await resp.Content.ReadAsStringAsync();
        json.Should().Contain("jobId");
    }

    // ══════════════════════════════════════════════════════════════════
    //  GetDashboardSummary (5 tests)
    // ══════════════════════════════════════════════════════════════════

    // 26. Valid → 200
    [Fact]
    public async Task GetDashboardSummary_Valid_Returns200()
    {
        var sender = SetupDashboardSender();
        var client = CreateClient(sender);
        var resp = await client.GetAsync($"/api/cutting/analytics/dashboard-summary?tenantId={TenantId}");
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // 27. Both queries are called
    [Fact]
    public async Task GetDashboardSummary_BothQueriesCalled()
    {
        var sender = SetupDashboardSender();
        var client = CreateClient(sender);
        await client.GetAsync($"/api/cutting/analytics/dashboard-summary?tenantId={TenantId}");
        sender.Verify(s => s.Send(It.IsAny<GetDailyExecutionMetricsQuery>(), It.IsAny<CancellationToken>()), Times.Once);
        sender.Verify(s => s.Send(It.IsAny<GetMaterialUsageQuery>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    // 28. TenantId passed to both queries
    [Fact]
    public async Task GetDashboardSummary_TenantIdPassedToBothQueries()
    {
        Guid? capturedExec = null;
        Guid? capturedMat = null;
        var sender = new Mock<IMediator>();
        sender.Setup(s => s.Send(It.IsAny<GetDailyExecutionMetricsQuery>(), It.IsAny<CancellationToken>()))
              .Callback<IRequest<Result<AnalyticsPagedResult<DailyExecutionMetric>>>, CancellationToken>((q, _) => capturedExec = ((GetDailyExecutionMetricsQuery)q).TenantId)
              .ReturnsAsync(Result<AnalyticsPagedResult<DailyExecutionMetric>>.Success(EmptyPage<DailyExecutionMetric>()));
        sender.Setup(s => s.Send(It.IsAny<GetMaterialUsageQuery>(), It.IsAny<CancellationToken>()))
              .Callback<IRequest<Result<AnalyticsPagedResult<DailyMaterialUsage>>>, CancellationToken>((q, _) => capturedMat = ((GetMaterialUsageQuery)q).TenantId)
              .ReturnsAsync(Result<AnalyticsPagedResult<DailyMaterialUsage>>.Success(EmptyPage<DailyMaterialUsage>()));

        var client = CreateClient(sender);
        await client.GetAsync($"/api/cutting/analytics/dashboard-summary?tenantId={TenantId}");

        capturedExec.Should().Be(TenantId);
        capturedMat.Should().Be(TenantId);
    }

    // 29. Date range is 30 days
    [Fact]
    public async Task GetDashboardSummary_DateRange_Is30Days()
    {
        GetDailyExecutionMetricsQuery? captured = null;
        var sender = new Mock<IMediator>();
        sender.Setup(s => s.Send(It.IsAny<GetDailyExecutionMetricsQuery>(), It.IsAny<CancellationToken>()))
              .Callback<IRequest<Result<AnalyticsPagedResult<DailyExecutionMetric>>>, CancellationToken>((q, _) => captured = (GetDailyExecutionMetricsQuery)q)
              .ReturnsAsync(Result<AnalyticsPagedResult<DailyExecutionMetric>>.Success(EmptyPage<DailyExecutionMetric>()));
        sender.Setup(s => s.Send(It.IsAny<GetMaterialUsageQuery>(), It.IsAny<CancellationToken>()))
              .ReturnsAsync(Result<AnalyticsPagedResult<DailyMaterialUsage>>.Success(EmptyPage<DailyMaterialUsage>()));

        var client = CreateClient(sender);
        await client.GetAsync($"/api/cutting/analytics/dashboard-summary?tenantId={TenantId}");

        var days = captured!.To.DayNumber - captured.From.DayNumber;
        days.Should().BeCloseTo(30, 2);
    }

    // 30. Response contains executionMetrics and materialUsage
    [Fact]
    public async Task GetDashboardSummary_ResponseContainsExpectedFields()
    {
        var sender = SetupDashboardSender();
        var client = CreateClient(sender);
        var resp = await client.GetAsync($"/api/cutting/analytics/dashboard-summary?tenantId={TenantId}");
        var body = await resp.Content.ReadAsStringAsync();
        body.Should().Contain("executionMetrics");
        body.Should().Contain("materialUsage");
    }

    private static Mock<IMediator> SetupDashboardSender()
    {
        var sender = new Mock<IMediator>();
        sender.Setup(s => s.Send(It.IsAny<GetDailyExecutionMetricsQuery>(), It.IsAny<CancellationToken>()))
              .ReturnsAsync(Result<AnalyticsPagedResult<DailyExecutionMetric>>.Success(EmptyPage<DailyExecutionMetric>()));
        sender.Setup(s => s.Send(It.IsAny<GetMaterialUsageQuery>(), It.IsAny<CancellationToken>()))
              .ReturnsAsync(Result<AnalyticsPagedResult<DailyMaterialUsage>>.Success(EmptyPage<DailyMaterialUsage>()));
        return sender;
    }
}
