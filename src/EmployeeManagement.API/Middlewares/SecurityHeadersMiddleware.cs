namespace EmployeeManagement.API.Middlewares;

/// <summary>
/// Enforces OWASP recommended security headers on all HTTP responses.
/// Prevents MIME sniffing, clickjacking, and cross-site scripting vulnerabilities.
/// </summary>
public sealed class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;

    public SecurityHeadersMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public Task InvokeAsync(HttpContext context)
    {
        var headers = context.Response.Headers;

        headers.Append("X-Content-Type-Options", "nosniff");
        headers.Append("X-Frame-Options", "DENY");
        headers.Append("X-XSS-Protection", "0");
        headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");
        headers.Append("Content-Security-Policy", "default-src 'self'");
        headers.Remove("Server");
        headers.Remove("X-Powered-By");

        return _next(context);
    }
}
