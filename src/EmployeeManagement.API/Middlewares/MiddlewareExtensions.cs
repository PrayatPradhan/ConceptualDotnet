namespace EmployeeManagement.API.Middlewares;

public static class MiddlewareExtensions
{
    public static IApplicationBuilder UseRequestCorrelation(this IApplicationBuilder app)
        => app.UseMiddleware<RequestCorrelationMiddleware>();

    public static IApplicationBuilder UseGlobalExceptionHandler(this IApplicationBuilder app)
        => app.UseMiddleware<GlobalExceptionHandlingMiddleware>();

    public static IApplicationBuilder UseRequestProfiling(this IApplicationBuilder app)
        => app.UseMiddleware<RequestPerformanceProfilingMiddleware>();

    public static IApplicationBuilder UseTokenBucketRateLimiting(this IApplicationBuilder app)
        => app.UseMiddleware<TokenBucketRateLimitingMiddleware>();

    public static IApplicationBuilder UseCustomSecurityHeaders(this IApplicationBuilder app)
        => app.UseMiddleware<SecurityHeadersMiddleware>();
}
