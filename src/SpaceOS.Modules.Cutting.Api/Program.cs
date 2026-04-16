using SpaceOS.Modules.Cutting.Api.Endpoints;
using SpaceOS.Modules.Cutting.Api.Extensions;
using SpaceOS.Modules.Cutting.Infrastructure.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCuttingApplication();
builder.Services.AddAuthentication().AddJwtBearer(opts => { opts.MapInboundClaims = false; });
builder.Services.AddAuthorization(opts =>
    opts.AddPolicy("ManufacturerOnly", p => p.RequireAuthenticatedUser()));

var connectionString = builder.Configuration.GetConnectionString("Cutting")
    ?? "Host=localhost;Database=spaceos;Username=spaceos_app;Password=changeme";

builder.Services.AddCuttingInfrastructure(connectionString);

var app = builder.Build();
app.UseAuthentication();
app.UseAuthorization();
app.MapCuttingEndpoints();
app.Run();

public partial class Program { }
