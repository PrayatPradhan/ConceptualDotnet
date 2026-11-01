using System.Collections.Concurrent;
using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;

namespace EmployeeManagement.API.Middlewares;

/// <summary>
/// Enforces rate limiting per IP address using the Token Bucket algorithm.
/// Allows burst traffic up to Capacity, replenishing tokens at RefillRatePerSecond.
/// Returns HTTP 429 Too Many Requests when the client exhausts their bucket.
/// </summary>
public sealed class TokenBucketRateLimitingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<TokenBucketRateLimitingMiddleware> _logger;

    // Default configuration: 10 burst capacity, refilling 2 tokens per second
    private const double BucketCapacity = 10.0;
    private const double RefillRatePerSecond = 2.0;

    private static readonly ConcurrentDictionary<string, TokenBucket> ClientBuckets = new();

    public TokenBucketRateLimitingMiddleware(RequestDelegate next, ILogger<TokenBucketRateLimitingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var clientKey = GetClientIdentifier(context);
        var bucket = ClientBuckets.GetOrAdd(clientKey, _ => new TokenBucket(BucketCapacity, RefillRatePerSecond));

        if (!bucket.TryConsume(1.0))
        {
            _logger.LogWarning("Rate limit exceeded for client: {ClientKey} on {Path}", clientKey, context.Request.Path);

            context.Response.StatusCode = (int)HttpStatusCode.TooManyRequests;
            context.Response.ContentType = "application/problem+json";
            context.Response.Headers.Append("Retry-After", "1");

            var problemDetails = new ProblemDetails
            {
                Status = StatusCodes.Status429TooManyRequests,
                Title = "Too Many Requests",
                Detail = "Rate limit exceeded. Please wait before retrying your request.",
                Instance = context.Request.Path
            };

            var json = JsonSerializer.Serialize(problemDetails);
            await context.Response.WriteAsync(json);
            return;
        }

        await _next(context);
    }

    private static string GetClientIdentifier(HttpContext context)
    {
        // If authenticated, rate limit per user identity; otherwise rate limit per remote IP
        if (context.User.Identity?.IsAuthenticated == true && !string.IsNullOrWhiteSpace(context.User.Identity.Name))
        {
            return $"user:{context.User.Identity.Name}";
        }

        var ip = context.Connection.RemoteIpAddress?.ToString();
        return string.IsNullOrWhiteSpace(ip) ? "unknown-client" : $"ip:{ip}";
    }

    /// <summary>
    /// Thread-safe in-memory Token Bucket representation.
    /// </summary>
    private sealed class TokenBucket
    {
        private readonly double _capacity;
        private readonly double _refillRatePerSecond;
        private double _currentTokens;
        private DateTime _lastRefillUtc;
        private readonly object _lock = new();

        public TokenBucket(double capacity, double refillRatePerSecond)
        {
            _capacity = capacity;
            _refillRatePerSecond = refillRatePerSecond;
            _currentTokens = capacity;
            _lastRefillUtc = DateTime.UtcNow;
        }

        public bool TryConsume(double tokens)
        {
            lock (_lock)
            {
                Refill();

                if (_currentTokens >= tokens)
                {
                    _currentTokens -= tokens;
                    return true;
                }

                return false;
            }
        }

        private void Refill()
        {
            var now = DateTime.UtcNow;
            var elapsedSeconds = (now - _lastRefillUtc).TotalSeconds;

            if (elapsedSeconds > 0)
            {
                var newTokens = elapsedSeconds * _refillRatePerSecond;
                _currentTokens = Math.Min(_capacity, _currentTokens + newTokens);
                _lastRefillUtc = now;
            }
        }
    }
}
