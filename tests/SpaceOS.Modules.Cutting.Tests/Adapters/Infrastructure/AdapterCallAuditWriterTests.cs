using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using SpaceOS.Modules.Cutting.Infrastructure.Adapters;
using SpaceOS.Modules.Cutting.Infrastructure.Persistence;
using Xunit;

namespace SpaceOS.Modules.Cutting.Tests.Adapters.Infrastructure;

public class AdapterCallAuditWriterTests
{
    private static CuttingDbContext BuildInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<CuttingDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new CuttingDbContext(options);
    }

    [Fact]
    public async Task RecordSubmitStartedAsync_CreatesRowWithStatusStarted()
    {
        await using var db = BuildInMemoryContext();
        var writer = new AdapterCallAuditWriter(db, TimeProvider.System, NullLogger<AdapterCallAuditWriter>.Instance);

        var callId = Guid.NewGuid();
        await writer.RecordSubmitStartedAsync(callId, "opticut", "SubmitSheet", Guid.NewGuid(), CancellationToken.None);

        var row = db.AdapterCallAudits.Single(x => x.CallId == callId);
        row.Status.Should().Be("started");
        row.AdapterName.Should().Be("opticut");
        row.MethodName.Should().Be("SubmitSheet");
    }

    [Fact]
    public async Task RecordFailureAsync_SanitizesErrorMessage()
    {
        await using var db = BuildInMemoryContext();
        var writer = new AdapterCallAuditWriter(db, TimeProvider.System, NullLogger<AdapterCallAuditWriter>.Instance);

        var callId = Guid.NewGuid();
        await writer.RecordFailureAsync(callId, new[] { "Error\r\nInjected\x00" }, CancellationToken.None);

        var row = db.AdapterCallAudits.Single(x => x.CallId == callId);
        row.Status.Should().Be("failed");
        row.ErrorMessage.Should().NotContain("\r");
        row.ErrorMessage.Should().NotContain("\n");
        row.ErrorMessage.Should().NotContain("\x00");
    }

    [Fact]
    public async Task RecordExceptionAsync_CreatesRowWithStatusException()
    {
        await using var db = BuildInMemoryContext();
        var writer = new AdapterCallAuditWriter(db, TimeProvider.System, NullLogger<AdapterCallAuditWriter>.Instance);

        var callId = Guid.NewGuid();
        var ex = new InvalidOperationException("something went wrong");
        await writer.RecordExceptionAsync(callId, ex, CancellationToken.None);

        var row = db.AdapterCallAudits.Single(x => x.CallId == callId);
        row.Status.Should().Be("exception");
        row.ErrorMessage.Should().Contain("something went wrong");
    }

    [Fact]
    public async Task RecordFailureAsync_TruncatesErrorAt8000Chars()
    {
        await using var db = BuildInMemoryContext();
        var writer = new AdapterCallAuditWriter(db, TimeProvider.System, NullLogger<AdapterCallAuditWriter>.Instance);

        var callId = Guid.NewGuid();
        var longError = new string('X', 10000);
        await writer.RecordFailureAsync(callId, new[] { longError }, CancellationToken.None);

        var row = db.AdapterCallAudits.Single(x => x.CallId == callId);
        row.ErrorMessage!.Length.Should().BeLessThanOrEqualTo(8000);
    }

    [Fact]
    public async Task RecordCapabilityFallbackAsync_CreatesRowWithStatusFallback()
    {
        await using var db = BuildInMemoryContext();
        var writer = new AdapterCallAuditWriter(db, TimeProvider.System, NullLogger<AdapterCallAuditWriter>.Instance);

        var tenantId = Guid.NewGuid();
        await writer.RecordCapabilityFallbackAsync("opticut", "waste-reporting", tenantId, CancellationToken.None);

        var row = db.AdapterCallAudits.First(x => x.TenantId == tenantId);
        row.Status.Should().Be("fallback");
        row.AdapterName.Should().Be("opticut");
    }

    [Fact]
    public async Task RecordExceptionAsync_NullException_ThrowsArgumentNullException()
    {
        await using var db = BuildInMemoryContext();
        var writer = new AdapterCallAuditWriter(db, TimeProvider.System, NullLogger<AdapterCallAuditWriter>.Instance);

        var act = async () => await writer.RecordExceptionAsync(Guid.NewGuid(), null!, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }
}
