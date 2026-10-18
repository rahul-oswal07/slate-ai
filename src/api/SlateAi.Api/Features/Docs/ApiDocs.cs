using Microsoft.Extensions.Options;
using Microsoft.OpenApi;

namespace SlateAi.Api.Features.Docs;

public sealed class ApiDocsOptions
{
    public const string Section = "OpenApi";

    /// <summary>Serve /openapi/v1.json and Swagger UI at /swagger. On in Development and staging, off in production.</summary>
    public bool Enabled { get; set; }
}

public static class ApiDocs
{
    private const string DocumentPath = "/openapi/v1.json";

    public static IServiceCollection AddApiDocs(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<ApiDocsOptions>(configuration.GetSection(ApiDocsOptions.Section));
        services.AddOpenApi(options => options.AddDocumentTransformer((document, _, _) =>
        {
            document.Info = new OpenApiInfo
            {
                Title = "Tappida Paatha API",
                Version = "v1",
                Description = "Home-language catch-up of the lesson an absent child missed. Hackathon MVP.",
            };
            return Task.CompletedTask;
        }));
        return services;
    }

    /// <summary>Must run before MapSpa so /swagger isn't swallowed by the static/SPA fallbacks.</summary>
    public static WebApplication MapApiDocs(this WebApplication app)
    {
        if (!app.Services.GetRequiredService<IOptions<ApiDocsOptions>>().Value.Enabled)
        {
            return app;
        }

        app.MapOpenApi(DocumentPath);
        app.UseSwaggerUI(options =>
        {
            options.SwaggerEndpoint(DocumentPath, "Tappida Paatha API v1");
            options.RoutePrefix = "swagger";
            options.DocumentTitle = "Tappida Paatha API";
        });
        return app;
    }
}
