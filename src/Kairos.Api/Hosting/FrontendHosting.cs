using Microsoft.AspNetCore.StaticFiles;

namespace Kairos.Api.Hosting;

/// <summary>
/// In the container image, the built front lives in wwwroot and the API serves it:
/// one origin, so no CORS and the client address reaches the rate limiter untouched.
/// In development Vite serves the front, wwwroot is empty and nothing here applies.
/// </summary>
internal static class FrontendHosting
{
    private const string IndexFile = "index.html";
    private const string HashedAssetsPath = "/assets";

    private static readonly StaticFileOptions Files = new()
    {
        ContentTypeProvider = new FileExtensionContentTypeProvider { Mappings = { [".webmanifest"] = "application/manifest+json" } },
        OnPrepareResponse = context =>
        {
            // Vite puts a hash in the name of every built asset: a new build means a new name.
            // The page itself must always be checked again, or visitors would keep an old build.
            context.Context.Response.Headers.CacheControl = context.Context.Request.Path.StartsWithSegments(HashedAssetsPath)
                ? "public, max-age=31536000, immutable"
                : "no-cache";
        },
    };

    public static bool HasFrontend(this WebApplication app) =>
        app.Environment.WebRootFileProvider.GetFileInfo(IndexFile).Exists;

    public static void UseFrontendFiles(this WebApplication app)
    {
        if (app.HasFrontend())
        {
            app.UseStaticFiles(Files);
        }
    }

    /// <summary>Any address the API does not know belongs to the single-page application.</summary>
    public static void MapFrontend(this WebApplication app)
    {
        if (!app.HasFrontend())
        {
            return;
        }

        // An unknown API address stays a 404 rather than becoming the application page.
        app.Map("/api/{**path}", () => Results.NotFound());
        app.MapFallbackToFile(IndexFile, Files);
    }
}
