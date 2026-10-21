using Microsoft.EntityFrameworkCore;
using SlateAi.Api.Ai;
using SlateAi.Api.Curriculum;
using SlateAi.Api.Data;
using SlateAi.Api.Data.Seed;
using SlateAi.Api.Features.Attendance;
using SlateAi.Api.Features.Auth;
using SlateAi.Api.Features.Docs;
using SlateAi.Api.Features.Health;
using SlateAi.Api.Features.Ledger;
using SlateAi.Api.Features.Spa;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<DatabaseOptions>(builder.Configuration.GetSection(DatabaseOptions.Section));
builder.Services.Configure<BuildInfoOptions>(builder.Configuration.GetSection(BuildInfoOptions.Section));
builder.Services.Configure<DemoOptions>(builder.Configuration.GetSection(DemoOptions.Section));

// Resolve the connection string lazily so test hosts can override configuration.
builder.Services.AddDbContext<AppDbContext>((sp, options) =>
    options.UseNpgsql(
        sp.GetRequiredService<IConfiguration>().GetConnectionString("Db")
        ?? throw new InvalidOperationException("ConnectionStrings:Db is not configured.")));

builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddScoped<DemoSeeder>();
builder.Services.AddCurriculum(builder.Configuration);
builder.Services.AddAi(builder.Configuration);
builder.Services.AddAuth();
builder.Services.AddAttendance();
builder.Services.AddLedger();
builder.Services.AddHealthChecks().AddDbContextCheck<AppDbContext>();
builder.Services.AddApiDocs(builder.Configuration);

var app = builder.Build();

app.LoadCurriculum();
app.CheckAiSetup();

await app.InitialiseDatabaseAsync(app.Lifetime.ApplicationStopping);

app.UseAuthentication();
app.UseAuthorization();

app.MapHealthEndpoints();
app.MapAuthEndpoints();
app.MapAttendanceEndpoints();
app.MapLedgerEndpoints();
app.MapApiDocs();
app.MapSpa();

await app.RunAsync();

public partial class Program;
