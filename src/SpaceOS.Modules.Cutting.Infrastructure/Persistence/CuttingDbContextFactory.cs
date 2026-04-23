using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace SpaceOS.Modules.Cutting.Infrastructure.Persistence;

/// <summary>Design-time factory used by EF Core CLI for migrations.</summary>
public sealed class CuttingDbContextFactory : IDesignTimeDbContextFactory<CuttingDbContext>
{
    public CuttingDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<CuttingDbContext>()
            .UseNpgsql("Host=localhost;Database=spaceos;Username=spaceos_app;Password=dev",
                npg => npg.MigrationsHistoryTable("__EFMigrationsHistory", "spaceos_cutting"))
            .Options;
        return new CuttingDbContext(options);
    }
}
