using MediatR;
using Microsoft.EntityFrameworkCore;
using SpaceOS.Modules.Cutting.Analytics.Application.Projections;
using SpaceOS.Modules.Cutting.Analytics.Infrastructure.Extensions;
using SpaceOS.Modules.Cutting.Api.Endpoints;
using SpaceOS.Modules.Cutting.Api.Extensions;
using SpaceOS.Modules.Cutting.Execution.Application.Commands.ScheduleExecution;
using SpaceOS.Modules.Cutting.Execution.Infrastructure.Extensions;
using SpaceOS.Modules.Cutting.Execution.Infrastructure.Realtime;
using SpaceOS.Modules.Cutting.Infrastructure.Extensions;
using SpaceOS.Modules.Cutting.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCuttingApplication();

// Register MediatR handlers from Execution.Application assembly
builder.Services.AddMediatR(cfg =>
    cfg.RegisterServicesFromAssemblyContaining<ScheduleExecutionCommand>());

builder.Services.AddHealthChecks();
builder.Services.AddAuthentication().AddJwtBearer(opts => { opts.MapInboundClaims = false; });
builder.Services.AddAuthorization(opts =>
    opts.AddPolicy("ManufacturerOnly", p => p.RequireAuthenticatedUser()));

var connectionString = builder.Configuration.GetConnectionString("Cutting")
    ?? "Host=localhost;Database=spaceos;Username=spaceos_app;Password=changeme";

builder.Services.AddCuttingInfrastructure(connectionString, builder.Configuration);
builder.Services.AddCuttingExecutionInfrastructure();

var analyticsConnectionString = builder.Configuration.GetConnectionString("CuttingAnalytics")
    ?? connectionString;
builder.Services.AddCuttingAnalyticsInfrastructure(analyticsConnectionString);

// Register Analytics Application projectors
builder.Services.AddScoped<SpaceOS.Modules.Cutting.Analytics.Domain.Interfaces.IExecutionMetricProjector, ExecutionMetricProjector>();
builder.Services.AddScoped<SpaceOS.Modules.Cutting.Analytics.Domain.Interfaces.IMaterialUsageProjector, MaterialUsageProjector>();
builder.Services.AddScoped<SpaceOS.Modules.Cutting.Analytics.Domain.Interfaces.IOEEProjector, OEEProjector>();
builder.Services.AddScoped<SpaceOS.Modules.Cutting.Analytics.Domain.Interfaces.IOperatorMetricProjector, OperatorMetricProjector>();

// Register Analytics MediatR handlers
builder.Services.AddMediatR(cfg =>
    cfg.RegisterServicesFromAssemblyContaining<SpaceOS.Modules.Cutting.Analytics.Application.Queries.GetDailyExecutionMetricsQuery>());

// SignalR for real-time execution updates
builder.Services.AddSignalR();

var app = builder.Build();

// Auto-apply pending migrations on startup (safe: EF skips already-applied migrations)
if (app.Environment.IsProduction() || app.Environment.IsEnvironment("Staging"))
{
    using var scope = app.Services.CreateScope();
    scope.ServiceProvider.GetRequiredService<CuttingDbContext>().Database.Migrate();
}

app.UseAuthentication();
app.UseAuthorization();
app.MapHealthChecks("/healthz").AllowAnonymous();
app.MapCuttingEndpoints();
app.MapCuttingPlanningEndpoints();
app.MapInternalEndpoints();
app.MapCuttingExecutionEndpoints();
app.MapHub<ExecutionHub>("/hubs/execution");
app.MapAnalyticsEndpoints();
app.Run();

public partial class Program { }
