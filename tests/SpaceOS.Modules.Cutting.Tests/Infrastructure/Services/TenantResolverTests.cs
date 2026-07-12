using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using SpaceOS.Modules.Cutting.Application.Services;
using SpaceOS.Modules.Cutting.Infrastructure.Persistence;
using SpaceOS.Modules.Cutting.Infrastructure.Services;
using System.Data.Common;
using Xunit;

namespace SpaceOS.Modules.Cutting.Tests.Infrastructure.Services;

/// <summary>
/// Unit tests for TenantResolver.
/// Tests subdomain extraction and tenant resolution from hostname.
/// Note: Since TenantResolver uses raw SQL (SqlQueryRaw), we test behavior via actual Sqlite database.
/// </summary>
public class TenantResolverTests : IDisposable
{
    private readonly Mock<ILogger<TenantResolver>> _loggerMock;
    private readonly DbConnection _connection;
    private readonly CuttingDbContext _dbContext;
    private readonly Guid _doorstarId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private readonly Guid _lapszabaszId = Guid.Parse("22222222-2222-2222-2222-222222222222");

    public TenantResolverTests()
    {
        _loggerMock = new Mock<ILogger<TenantResolver>>();

        // Use Sqlite in-memory database for raw SQL support
        var options = new DbContextOptionsBuilder<CuttingDbContext>()
            .UseSqlite("Data Source=InMemoryTestDb;Mode=Memory;Cache=Shared")
            .Options;

        _dbContext = new CuttingDbContext(options);
        _connection = _dbContext.Database.GetDbConnection();
        _connection.Open();

        // Create Tenants table and seed test data
        _dbContext.Database.ExecuteSqlRaw(
            """
            CREATE TABLE IF NOT EXISTS "Tenants" (
                "Id" TEXT PRIMARY KEY,
                "Subdomain" TEXT NOT NULL,
                "Name" TEXT NOT NULL
            )
            """);

        _dbContext.Database.ExecuteSqlRaw(
            """
            INSERT INTO "Tenants" ("Id", "Subdomain", "Name")
            VALUES ({0}, {1}, {2}), ({3}, {4}, {5})
            """,
            _doorstarId.ToString(), "doorstar", "Doorstar Kft.",
            _lapszabaszId.ToString(), "lapszabasz-kft", "Lapszabász Kft.");
    }

    private IDbContextFactory<CuttingDbContext> CreateDbContextFactory()
    {
        var factoryMock = new Mock<IDbContextFactory<CuttingDbContext>>();
        factoryMock
            .Setup(f => f.CreateDbContextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(_dbContext);
        return factoryMock.Object;
    }

    [Fact]
    public async Task GetTenantIdFromSubdomain_ValidSubdomain_ExtractsCorrectly()
    {
        // Arrange
        var factory = CreateDbContextFactory();
        var resolver = new TenantResolver(factory, _loggerMock.Object);

        // Act
        var result = await resolver.GetTenantIdFromSubdomain("doorstar.joinerytech.hu", CancellationToken.None);

        // Assert
        result.Should().Be(_doorstarId);
    }

    [Fact]
    public async Task GetTenantIdFromSubdomain_ValidSubdomain_ReturnsCorrectTenantId()
    {
        // Arrange
        var factory = CreateDbContextFactory();
        var resolver = new TenantResolver(factory, _loggerMock.Object);

        // Act
        var result = await resolver.GetTenantIdFromSubdomain("lapszabasz-kft.joinerytech.hu", CancellationToken.None);

        // Assert
        result.Should().Be(_lapszabaszId);
    }

    [Fact]
    public async Task GetTenantIdFromSubdomain_SubdomainNotFound_ThrowsTenantNotFoundException()
    {
        // Arrange
        var factory = CreateDbContextFactory();
        var resolver = new TenantResolver(factory, _loggerMock.Object);

        // Act & Assert
        var act = async () => await resolver.GetTenantIdFromSubdomain("nonexistent.joinerytech.hu", CancellationToken.None);

        await act.Should().ThrowAsync<TenantNotFoundException>()
            .WithMessage("No tenant found for subdomain: nonexistent");
    }

    [Fact]
    public async Task GetTenantIdFromSubdomain_MultipleTenantsMatch_ReturnsFirstMatch()
    {
        // Arrange
        var duplicateId1 = Guid.Parse("33333333-3333-3333-3333-333333333333");
        var duplicateId2 = Guid.Parse("44444444-4444-4444-4444-444444444444");

        _dbContext.Database.ExecuteSqlRaw(
            """
            INSERT INTO "Tenants" ("Id", "Subdomain", "Name")
            VALUES ({0}, {1}, {2}), ({3}, {4}, {5})
            """,
            duplicateId1.ToString(), "duplicate", "Tenant 1",
            duplicateId2.ToString(), "duplicate", "Tenant 2");

        var factory = CreateDbContextFactory();
        var resolver = new TenantResolver(factory, _loggerMock.Object);

        // Act
        var result = await resolver.GetTenantIdFromSubdomain("duplicate.joinerytech.hu", CancellationToken.None);

        // Assert
        // SQL LIMIT 1 ensures only first match is returned
        result.Should().NotBe(Guid.Empty);
        (result == duplicateId1 || result == duplicateId2).Should().BeTrue();
    }

    [Fact]
    public async Task GetTenantIdFromSubdomain_NullHostname_ThrowsInvalidOperationException()
    {
        // Arrange
        var factory = CreateDbContextFactory();
        var resolver = new TenantResolver(factory, _loggerMock.Object);

        // Act & Assert
        var act = async () => await resolver.GetTenantIdFromSubdomain(null!, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Hostname cannot be null or empty.");
    }

    [Fact]
    public async Task GetTenantIdFromSubdomain_EmptyHostname_ThrowsInvalidOperationException()
    {
        // Arrange
        var factory = CreateDbContextFactory();
        var resolver = new TenantResolver(factory, _loggerMock.Object);

        // Act & Assert
        var act = async () => await resolver.GetTenantIdFromSubdomain("", CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Hostname cannot be null or empty.");
    }

    [Fact]
    public async Task GetTenantIdFromSubdomain_HostnameWithoutSubdomain_ThrowsInvalidOperationException()
    {
        // Arrange
        var factory = CreateDbContextFactory();
        var resolver = new TenantResolver(factory, _loggerMock.Object);

        // Act & Assert
        var act = async () => await resolver.GetTenantIdFromSubdomain("joinerytech", CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Invalid hostname format: joinerytech. Expected at least 2 parts (subdomain.domain).");
    }

    [Fact]
    public async Task GetTenantIdFromSubdomain_ComplexHostnameMultipleDots_ExtractsFirstPart()
    {
        // Arrange
        var apiId = Guid.Parse("55555555-5555-5555-5555-555555555555");

        _dbContext.Database.ExecuteSqlRaw(
            """
            INSERT INTO "Tenants" ("Id", "Subdomain", "Name")
            VALUES ({0}, {1}, {2})
            """,
            apiId.ToString(), "api", "API Tenant");

        var factory = CreateDbContextFactory();
        var resolver = new TenantResolver(factory, _loggerMock.Object);

        // Act
        var result = await resolver.GetTenantIdFromSubdomain("api.doorstar.joinerytech.hu", CancellationToken.None);

        // Assert
        result.Should().Be(apiId);
    }

    [Fact]
    public async Task GetTenantIdFromSubdomain_CaseInsensitiveMatching_ReturnsCorrectTenant()
    {
        // Arrange
        var factory = CreateDbContextFactory();
        var resolver = new TenantResolver(factory, _loggerMock.Object);

        // Act
        var result = await resolver.GetTenantIdFromSubdomain("DOORSTAR.joinerytech.hu", CancellationToken.None);

        // Assert
        result.Should().Be(_doorstarId);
    }

    [Fact]
    public async Task GetTenantIdFromSubdomain_LogsWarning_WhenTenantNotFound()
    {
        // Arrange
        var factory = CreateDbContextFactory();
        var resolver = new TenantResolver(factory, _loggerMock.Object);

        // Act
        try
        {
            await resolver.GetTenantIdFromSubdomain("missing.joinerytech.hu", CancellationToken.None);
        }
        catch (TenantNotFoundException)
        {
            // Expected exception
        }

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("No tenant found for subdomain: missing")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    public void Dispose()
    {
        _connection?.Dispose();
        _dbContext?.Dispose();
    }
}
