using Microsoft.AspNetCore.StaticFiles;

namespace SlateAi.Api.Features.Spa;

/// <summary>
/// Serves the built React PWA from wwwroot (D-10: one container). Client-side routes fall back to
/// index.html; unknown /api routes return 404 instead of HTML.
/// </summary>
public static class SpaHosting
{
    // Must revalidate on every load so installed PWAs pick up new builds.
    private static readonly HashSet<string> NoCacheFiles =
        new(StringComparer.OrdinalIgnoreCase) { "index.html", "sw.js", "manifest.webmanifest" };

    public static WebApplication MapSpa(this WebApplication app)
    {
        var contentTypes = new FileExtensionContentTypeProvider();
        contentTypes.Mappings[".webmanifest"] = "application/manifest+json";

        var staticFiles = new StaticFileOptions
        {
            ContentTypeProvider = contentTypes,
            OnPrepareResponse = context =>
            {
                if (NoCacheFiles.Contains(context.File.Name))
                {
                    context.Context.Response.Headers.CacheControl = "no-cache";
                }
            },
        };

        app.UseDefaultFiles();
        app.UseStaticFiles(staticFiles);

        app.MapFallback("/api/{**path}", () => Results.NotFound());
        app.MapFallbackToFile("index.html", staticFiles);

        return app;
    }
}
