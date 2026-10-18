using Microsoft.EntityFrameworkCore;
using SlateAi.Api.Data;
using SlateAi.Api.Data.Seed;
using SlateAi.Api.Features.Docs;
using SlateAi.Api.Features.Health;
using SlateAi.Api.Features.Spa;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<DatabaseOptions>(builder.Configuration.GetSection(DatabaseOptions.Section));

// Resolve the connection string lazily so test hosts can override configuration.
builder.Services.AddDbContext<AppDbContext>((sp, options) =>
    options.UseNpgsql(
        sp.GetRequiredService<IConfiguration>().GetConnectionString("Db")
        ?? throw new InvalidOperationException("ConnectionStrings:Db is not configured.")));

builder.Services.AddScoped<DemoSeeder>();
builder.Services.AddHealthChecks().AddDbContextCheck<AppDbContext>();
builder.Services.AddApiDocs(builder.Configuration);

var app = builder.Build();

await app.InitialiseDatabaseAsync(app.Lifetime.ApplicationStopping);

app.MapHealthEndpoints();
app.MapApiDocs();
app.MapSpa();

await app.RunAsync();

public partial class Program;
