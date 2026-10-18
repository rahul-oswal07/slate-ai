using Microsoft.EntityFrameworkCore;
using SlateAi.Api.Data;
using SlateAi.Api.Data.Seed;
using SlateAi.Api.Features.Health;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<DatabaseOptions>(builder.Configuration.GetSection(DatabaseOptions.Section));

// Resolve the connection string lazily so test hosts can override configuration.
builder.Services.AddDbContext<AppDbContext>((sp, options) =>
    options.UseNpgsql(
        sp.GetRequiredService<IConfiguration>().GetConnectionString("Db")
        ?? throw new InvalidOperationException("ConnectionStrings:Db is not configured.")));

builder.Services.AddScoped<DemoSeeder>();
builder.Services.AddHealthChecks().AddDbContextCheck<AppDbContext>();

var app = builder.Build();

await app.InitialiseDatabaseAsync(app.Lifetime.ApplicationStopping);

app.MapHealthEndpoints();

await app.RunAsync();

public partial class Program;
