using Microsoft.EntityFrameworkCore;
using SpaceOS.Modules.Cutting.Infrastructure.Persistence;
using Xunit;

namespace SpaceOS.Modules.Cutting.Infrastructure.Tests.Persistence;

/// <summary>
/// Base class for integration tests using EF Core InMemory provider.
/// Faster and simpler than Testcontainers, sufficient for unit test coverage.
/// </summary>
public abstract class InfrastructureIntegrationTestBase : IAsyncLifetime
{
    protected CuttingDbContext? _dbContext;
    private readonly string _databaseName;

    protected InfrastructureIntegrationTestBase()
    {
        // Use unique database name per test class to ensure isolation
        _databaseName = $"CuttingTests_{Guid.NewGuid()}";
    }

    public virtual Task InitializeAsync()
    {
        var options = new DbContextOptionsBuilder<CuttingDbContext>()
            .UseInMemoryDatabase(_databaseName)
            .Options;

        _dbContext = new CuttingDbContext(options);

        // EF InMemory doesn't need migrations - schema is created automatically
        return Task.CompletedTask;
    }

    public virtual async Task DisposeAsync()
    {
        if (_dbContext is not null)
        {
            await _dbContext.DisposeAsync().ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Creates a fresh DbContext for scenarios requiring multiple contexts (e.g., testing AsNoTracking).
    /// </summary>
    protected CuttingDbContext CreateNewDbContext()
    {
        var options = new DbContextOptionsBuilder<CuttingDbContext>()
            .UseInMemoryDatabase(_databaseName)
            .Options;

        return new CuttingDbContext(options);
    }
}
