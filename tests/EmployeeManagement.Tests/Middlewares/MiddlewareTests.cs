using System.Text.Json;
using EmployeeManagement.API.Middlewares;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace EmployeeManagement.Tests.Middlewares;

public class MiddlewareTests
{
    [Fact]
    public async Task RequestCorrelationMiddleware_ShouldGenerateCorrelationId_WhenNoneProvided()
    {
        var context = new DefaultHttpContext();
        var middleware = new RequestCorrelationMiddleware(
            _ => Task.CompletedTask,
            NullLogger<RequestCorrelationMiddleware>.Instance);

        await middleware.InvokeAsync(context);

        Assert.True(context.Response.Headers.ContainsKey(RequestCorrelationMiddleware.CorrelationIdHeader));
        Assert.False(string.IsNullOrWhiteSpace(context.Response.Headers[RequestCorrelationMiddleware.CorrelationIdHeader]));
    }

    [Fact]
    public async Task RequestCorrelationMiddleware_ShouldPreserveExistingCorrelationId()
    {
        var context = new DefaultHttpContext();
        var existingId = "client-trace-12345";
        context.Request.Headers[RequestCorrelationMiddleware.CorrelationIdHeader] = existingId;

        var middleware = new RequestCorrelationMiddleware(
            _ => Task.CompletedTask,
            NullLogger<RequestCorrelationMiddleware>.Instance);

        await middleware.InvokeAsync(context);

        Assert.Equal(existingId, context.Response.Headers[RequestCorrelationMiddleware.CorrelationIdHeader].ToString());
    }

    [Fact]
    public async Task SecurityHeadersMiddleware_ShouldSetOwaspHeaders()
    {
        var context = new DefaultHttpContext();
        var middleware = new SecurityHeadersMiddleware(_ => Task.CompletedTask);

        await middleware.InvokeAsync(context);

        var headers = context.Response.Headers;
        Assert.Equal("nosniff", headers["X-Content-Type-Options"].ToString());
        Assert.Equal("DENY", headers["X-Frame-Options"].ToString());
        Assert.Equal("0", headers["X-XSS-Protection"].ToString());
    }

    [Fact]
    public async Task GlobalExceptionHandlingMiddleware_ShouldCatchKeyNotFound_AndReturn404ProblemDetails()
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        var middleware = new GlobalExceptionHandlingMiddleware(
            _ => throw new KeyNotFoundException("Employee was not found."),
            NullLogger<GlobalExceptionHandlingMiddleware>.Instance);

        await middleware.InvokeAsync(context);

        Assert.Equal(StatusCodes.Status404NotFound, context.Response.StatusCode);
        Assert.Equal("application/problem+json", context.Response.ContentType);

        context.Response.Body.Seek(0, SeekOrigin.Begin);
        var problem = await JsonSerializer.DeserializeAsync<ProblemDetails>(context.Response.Body, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        Assert.NotNull(problem);
        Assert.Equal("Resource Not Found", problem.Title);
        Assert.Equal("Employee was not found.", problem.Detail);
    }

    [Fact]
    public async Task TokenBucketRateLimitingMiddleware_ShouldReturn429_WhenLimitExceeded()
    {
        var middleware = new TokenBucketRateLimitingMiddleware(
            _ => Task.CompletedTask,
            NullLogger<TokenBucketRateLimitingMiddleware>.Instance);

        var ip = "192.168.1.100";
        var lastStatusCode = 200;

        // BucketCapacity is 10. Sending 15 rapid requests should trigger 429
        for (int i = 0; i < 15; i++)
        {
            var context = new DefaultHttpContext();
            context.Connection.RemoteIpAddress = System.Net.IPAddress.Parse(ip);
            context.Response.Body = new MemoryStream();

            await middleware.InvokeAsync(context);
            lastStatusCode = context.Response.StatusCode;
        }

        Assert.Equal(StatusCodes.Status429TooManyRequests, lastStatusCode);
    }
}
