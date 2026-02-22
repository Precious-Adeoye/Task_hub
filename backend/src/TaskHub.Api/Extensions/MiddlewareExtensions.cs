using AspNetCoreRateLimit;
using TaskHub.Api.Middleware;

namespace TaskHub.Api.Extensions;

public static class MiddlewareExtensions
{
    public static WebApplication UseAppMiddleware(this WebApplication app)
    {
        app.UseMiddleware<ProblemDetailsMiddleware>();
        app.UseMiddleware<ETagMiddleware>();
        app.UseMiddleware<CorrelationIdMiddleware>();
        app.UseCors("Frontend");
        app.UseIpRateLimiting();
        app.UseAuthentication();
        app.UseAuthorization();

        // Health endpoints
        app.MapGet("/health/live", () => Results.Ok(new { status = "Healthy" }))
            .ExcludeFromDescription();
        app.MapGet("/health/ready", (Task_hub.Application.Abstractions.IStorage storage) =>
        {
            try
            {
                return Results.Ok(new { status = "Ready", storage = "Available" });
            }
            catch
            {
                return Results.Json(new { status = "Unavailable" }, statusCode: 503);
            }
        }).ExcludeFromDescription();

        app.MapControllers();

        return app;
    }
}
