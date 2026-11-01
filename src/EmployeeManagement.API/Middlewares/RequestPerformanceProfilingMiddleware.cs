using System.Diagnostics;

namespace EmployeeManagement.API.Middlewares;

/// <summary>
/// Profiles request execution duration using high-precision Stopwatch.
/// Logs slow requests (exceeding 500ms) and adds X-Response-Time-Ms header to response.
/// </summary>
public sealed class RequestPerformanceProfilingMiddleware
{
    private const long SlowRequestThresholdMs = 500;
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestPerformanceProfilingMiddleware> _logger;

    public RequestPerformanceProfilingMiddleware(RequestDelegate next, ILogger<RequestPerformanceProfilingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();

        context.Response.OnStarting(() =>
        {
            stopwatch.Stop();
            context.Response.Headers.Append("X-Response-Time-Ms", stopwatch.ElapsedMilliseconds.ToString());
            return Task.CompletedTask;
        });

        try
        {
            await _next(context);
        }
        finally
        {
            if (stopwatch.IsRunning)
            {
                stopwatch.Stop();
            }

            var elapsed = stopwatch.ElapsedMilliseconds;
            if (elapsed > SlowRequestThresholdMs)
            {
                _logger.LogWarning("SLOW REQUEST: {Method} {Path} completed in {Elapsed}ms (threshold: {Threshold}ms)",
                    context.Request.Method, context.Request.Path, elapsed, SlowRequestThresholdMs);
            }
            else
            {
                _logger.LogInformation("HTTP {Method} {Path} completed with status {StatusCode} in {Elapsed}ms",
                    context.Request.Method, context.Request.Path, context.Response.StatusCode, elapsed);
            }
        }
    }
}
