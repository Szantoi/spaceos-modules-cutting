using Microsoft.EntityFrameworkCore;
using SpaceOS.Modules.Cutting.Api.Endpoints;
using SpaceOS.Modules.Cutting.Api.Extensions;
using SpaceOS.Modules.Cutting.Infrastructure.Extensions;
using SpaceOS.Modules.Cutting.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCuttingApplication();
builder.Services.AddAuthentication().AddJwtBearer(opts => { opts.MapInboundClaims = false; });
builder.Services.AddAuthorization(opts =>
    opts.AddPolicy("ManufacturerOnly", p => p.RequireAuthenticatedUser()));

var connectionString = builder.Configuration.GetConnectionString("Cutting")
    ?? "Host=localhost;Database=spaceos;Username=spaceos_app;Password=changeme";

builder.Services.AddCuttingInfrastructure(connectionString, builder.Configuration);

var app = builder.Build();

// Auto-apply pending migrations on startup (safe: EF skips already-applied migrations)
if (app.Environment.IsProduction() || app.Environment.IsEnvironment("Staging"))
{
    using var scope = app.Services.CreateScope();
    scope.ServiceProvider.GetRequiredService<CuttingDbContext>().Database.Migrate();
}

app.UseAuthentication();
app.UseAuthorization();
app.MapCuttingEndpoints();
app.MapCuttingPlanningEndpoints();
app.MapInternalEndpoints();
app.Run();

public partial class Program { }
