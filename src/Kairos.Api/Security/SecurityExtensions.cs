using System.Globalization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Kairos.Api.Security;

/// <summary>Settings of the "RateLimiting" configuration section, per client address.</summary>
public sealed class RateLimitingOptions
{
    public const string SectionName = "RateLimiting";

    public int ReadPermitsPerMinute { get; set; } = 600;

    public int WritePermitsPerMinute { get; set; } = 60;
}

/// <summary>What protects a public, login-free demo from abuse.</summary>
internal static class SecurityExtensions
{
    public const string WritesPolicy = "writes";

    private const string CorsPolicy = "frontend";
    private const string HealthPathPrefix = "/health";

    private static readonly TimeSpan Window = TimeSpan.FromMinutes(1);

    public static IServiceCollection AddKairosSecurity(this IServiceCollection services, IConfiguration configuration)
    {
        var limits = configuration.GetSection(RateLimitingOptions.SectionName).Get<RateLimitingOptions>() ?? new RateLimitingOptions();
        var allowedOrigins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];

        // Behind a proxy the connection comes from the proxy: the client address is in this header.
        // Only the last entry, written by our own proxy, is trusted.
        services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
            options.KnownIPNetworks.Clear();
            options.KnownProxies.Clear();
        });

        services.AddCors(options => options.AddPolicy(CorsPolicy, policy => policy
            .WithOrigins(allowedOrigins)
            .WithMethods(HttpMethods.Get, HttpMethods.Post, HttpMethods.Put)
            .AllowAnyHeader()
            .AllowCredentials()));

        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.OnRejected = WriteRejectionAsync;

            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
                context.Request.Path.StartsWithSegments(HealthPathPrefix)
                    ? RateLimitPartition.GetNoLimiter(string.Empty)
                    : LimitPerClient(context, limits.ReadPermitsPerMinute));

            options.AddPolicy(WritesPolicy, context => LimitPerClient(context, limits.WritePermitsPerMinute));
        });

        return services;
    }

    public static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder app) =>
        app.Use((context, next) =>
        {
            context.Response.Headers.XContentTypeOptions = "nosniff";
            context.Response.Headers.XFrameOptions = "DENY";
            context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
            return next();
        });

    public static IApplicationBuilder UseFrontendCors(this IApplicationBuilder app) => app.UseCors(CorsPolicy);

    private static RateLimitPartition<string> LimitPerClient(HttpContext context, int permitsPerMinute) =>
        RateLimitPartition.GetFixedWindowLimiter(
            context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions { PermitLimit = permitsPerMinute, Window = Window });

    private static async ValueTask WriteRejectionAsync(OnRejectedContext rejected, CancellationToken cancellationToken)
    {
        if (rejected.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
        {
            rejected.HttpContext.Response.Headers.RetryAfter = ((int)retryAfter.TotalSeconds).ToString(CultureInfo.InvariantCulture);
        }

        await rejected.HttpContext.RequestServices.GetRequiredService<IProblemDetailsService>().WriteAsync(new ProblemDetailsContext
        {
            HttpContext = rejected.HttpContext,
            ProblemDetails = new ProblemDetails
            {
                Status = StatusCodes.Status429TooManyRequests,
                Title = "Trop de requêtes, réessayez dans un instant.",
            },
        });
    }
}
