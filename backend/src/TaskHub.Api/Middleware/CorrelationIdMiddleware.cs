namespace TaskHub.Api.Middleware
{
    public class CorrelationIdMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<CorrelationIdMiddleware> _logger;

        public CorrelationIdMiddleware(RequestDelegate next, ILogger<CorrelationIdMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Try to get correlation ID from request header
            if (!context.Request.Headers.TryGetValue("X-Correlation-ID", out var correlationId))
            {
                // Generate new correlation ID
                correlationId = Guid.NewGuid().ToString();
            }

            // Add to context items for easy access
            context.Items["CorrelationId"] = correlationId.ToString();

            // Add to response headers
            context.Response.OnStarting(() =>
            {
                context.Response.Headers["X-Correlation-ID"] = correlationId.ToString();
                return Task.CompletedTask;
            });

            // Add to logging scope
            using (_logger.BeginScope("{CorrelationId}", correlationId))
            {
                await _next(context);
            }
        }
    }
}
