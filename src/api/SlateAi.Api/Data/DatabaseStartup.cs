using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SlateAi.Api.Data.Seed;

namespace SlateAi.Api.Data;

public static class DatabaseStartup
{
    public static async Task InitialiseDatabaseAsync(this WebApplication app, CancellationToken cancellationToken)
    {
        var options = app.Services.GetRequiredService<IOptions<DatabaseOptions>>().Value;
        if (!options.MigrateOnStartup)
        {
            return;
        }

        await using var scope = app.Services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.MigrateAsync(cancellationToken);

        if (options.SeedOnStartup)
        {
            await scope.ServiceProvider.GetRequiredService<DemoSeeder>().SeedAsync(cancellationToken);
        }
    }
}
