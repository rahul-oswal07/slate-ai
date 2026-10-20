using Microsoft.Extensions.Options;

namespace SlateAi.Api.Curriculum;

public static class CurriculumSetup
{
    public static IServiceCollection AddCurriculum(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<CurriculumOptions>(configuration.GetSection(CurriculumOptions.Section));
        services.AddSingleton(sp =>
        {
            var directory = Path.Combine(
                AppContext.BaseDirectory, sp.GetRequiredService<IOptions<CurriculumOptions>>().Value.Directory);
            var graph = CurriculumLoader.LoadDirectory(directory);
            var logger = sp.GetRequiredService<ILogger<CurriculumGraph>>();
            if (graph.All.Count == 0)
            {
                // Expected until the competency list (T-22) lands; lesson mapping can't validate IDs without it.
                logger.LogWarning("No curriculum loaded from {Directory}", directory);
            }
            else
            {
                logger.LogInformation("Loaded {Count} competencies from {Directory}", graph.All.Count, directory);
            }

            return graph;
        });
        return services;
    }

    /// <summary>Loads the curriculum now so a bad file stops startup instead of the first request.</summary>
    public static void LoadCurriculum(this WebApplication app) => app.Services.GetRequiredService<CurriculumGraph>();
}
