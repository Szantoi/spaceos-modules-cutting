using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using SpaceOS.Modules.Cutting.Analytics.Application.Projections;
using SpaceOS.Modules.Cutting.Analytics.Infrastructure.Extensions;
using SpaceOS.Modules.Cutting.Api.Endpoints;
using SpaceOS.Modules.Cutting.Api.Extensions;
using SpaceOS.Modules.Cutting.Execution.Application.Commands.ScheduleExecution;
using SpaceOS.Modules.Cutting.Execution.Infrastructure.Extensions;
using SpaceOS.Modules.Cutting.Execution.Infrastructure.Realtime;
using SpaceOS.Modules.Cutting.Infrastructure.Extensions;
using SpaceOS.Modules.Cutting.Infrastructure.Persistence;
using System.Security.Claims;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCuttingApplication();

// Register MediatR handlers from Execution.Application assembly
builder.Services.AddMediatR(cfg =>
    cfg.RegisterServicesFromAssemblyContaining<ScheduleExecutionCommand>());

builder.Services.AddHealthChecks();

var jwtAuthority = builder.Configuration["Jwt:Authority"]
    ?? Environment.GetEnvironmentVariable("JWT_AUTHORITY");
var jwtAudience = builder.Configuration["Jwt:Audience"]
    ?? Environment.GetEnvironmentVariable("JWT_AUDIENCE")
    ?? "kernel-api";

if (builder.Environment.IsProduction())
    ArgumentNullException.ThrowIfNullOrEmpty(jwtAuthority,
        "Jwt:Authority / JWT_AUTHORITY must be configured");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opts =>
    {
        opts.MapInboundClaims = false;
        opts.Authority = jwtAuthority;
        opts.Audience = jwtAudience;
        opts.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
        opts.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer   = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ClockSkew        = TimeSpan.FromSeconds(30),
            NameClaimType    = "preferred_username",
            RoleClaimType    = ClaimTypes.Role,
        };
    });

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
app.MapAdapterAdminEndpoints();
app.Run();

public partial class Program { }
