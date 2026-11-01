using System.Text;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace EmployeeManagement.Infrastructure.Health;

/// <summary>
/// Probes Redis distributed cache availability.
/// If Redis fails, reports Degraded (HTTP 200) instead of Unhealthy,
/// because our Cache-Aside architecture safely degrades to querying SQL Server.
/// </summary>
public sealed class RedisHealthCheck : IHealthCheck
{
    private readonly IDistributedCache _cache;
    private const string ProbeKey = "health:ping";
    private static readonly byte[] ProbeValue = "pong"u8.ToArray();

    public RedisHealthCheck(IDistributedCache cache)
    {
        _cache = cache;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Perform a fast write and read probe with 10-second TTL
            await _cache.SetAsync(
                ProbeKey, 
                ProbeValue, 
                new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(10) }, 
                cancellationToken);

            var result = await _cache.GetAsync(ProbeKey, cancellationToken);

            if (result is not null && Encoding.UTF8.GetString(result) == "pong")
            {
                return HealthCheckResult.Healthy("Redis distributed cache is responsive.");
            }

            return HealthCheckResult.Degraded("Redis cache did not return expected probe value.");
        }
        catch (Exception ex)
        {
            // Graceful degradation: API continues to function via SQL Server fallback
            return HealthCheckResult.Degraded("Redis cache is unreachable. System is operating in degraded mode (cache-bypass).", ex);
        }
    }
}
