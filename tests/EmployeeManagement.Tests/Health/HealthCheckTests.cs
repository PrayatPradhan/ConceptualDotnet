using System.Text;
using System.Text.Json;
using EmployeeManagement.API.Health;
using EmployeeManagement.Infrastructure.Health;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Xunit;

namespace EmployeeManagement.Tests.Health;

public class HealthCheckTests
{
    [Fact]
    public async Task RedisHealthCheck_WhenCacheResponsive_ShouldReturnHealthy()
    {
        var cache = new InMemoryProbeDistributedCache();
        var healthCheck = new RedisHealthCheck(cache);

        var context = new HealthCheckContext();
        var result = await healthCheck.CheckHealthAsync(context);

        Assert.Equal(HealthStatus.Healthy, result.Status);
        Assert.Contains("responsive", result.Description);
    }

    [Fact]
    public async Task RedisHealthCheck_WhenCacheThrowsException_ShouldReturnDegraded()
    {
        var failingCache = new FailingDistributedCache();
        var healthCheck = new RedisHealthCheck(failingCache);

        var context = new HealthCheckContext();
        var result = await healthCheck.CheckHealthAsync(context);

        // Crucial Production Test: Must be Degraded, NOT Unhealthy
        Assert.Equal(HealthStatus.Degraded, result.Status);
        Assert.Contains("degraded mode", result.Description);
    }

    [Fact]
    public async Task FileStorageHealthCheck_WhenDirectoryAccessible_ShouldReturnHealthy()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "EM_HealthTest_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);

        try
        {
            var fakeEnv = new FakeWebHostEnvironment { WebRootPath = tempDir, ContentRootPath = tempDir };
            var healthCheck = new FileStorageHealthCheck(fakeEnv);

            var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

            Assert.Equal(HealthStatus.Healthy, result.Status);
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, recursive: true);
            }
        }
    }

    [Fact]
    public async Task HealthCheckResponseWriter_ShouldWriteStructuredJsonPayload()
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Response.Body = new MemoryStream();

        var entries = new Dictionary<string, HealthReportEntry>
        {
            ["sqlserver"] = new(HealthStatus.Healthy, "Database OK", TimeSpan.FromMilliseconds(12), null, null, ["ready", "db"]),
            ["redis"] = new(HealthStatus.Degraded, "Cache offline", TimeSpan.FromMilliseconds(5), null, null, ["ready", "cache"])
        };

        var report = new HealthReport(entries, TimeSpan.FromMilliseconds(17));

        await HealthCheckResponseWriter.WriteResponse(httpContext, report);

        httpContext.Response.Body.Seek(0, SeekOrigin.Begin);
        using var jsonDoc = await JsonDocument.ParseAsync(httpContext.Response.Body);
        var root = jsonDoc.RootElement;

        Assert.Equal("Degraded", root.GetProperty("status").GetString());
        Assert.True(root.TryGetProperty("entries", out var entriesElement));
        Assert.Equal(2, entriesElement.GetArrayLength());
    }

    // ── Test doubles for Health Checks ────────────────────────────────────────

    private sealed class InMemoryProbeDistributedCache : IDistributedCache
    {
        private readonly Dictionary<string, byte[]> _store = new();

        public byte[]? Get(string key) => _store.TryGetValue(key, out var val) ? val : null;
        public Task<byte[]?> GetAsync(string key, CancellationToken token = default) => Task.FromResult(Get(key));
        public void Set(string key, byte[] value, DistributedCacheEntryOptions options) => _store[key] = value;
        public Task SetAsync(string key, byte[] value, DistributedCacheEntryOptions options, CancellationToken token = default)
        {
            Set(key, value, options);
            return Task.CompletedTask;
        }
        public void Refresh(string key) { }
        public Task RefreshAsync(string key, CancellationToken token = default) => Task.CompletedTask;
        public void Remove(string key) => _store.Remove(key);
        public Task RemoveAsync(string key, CancellationToken token = default) { Remove(key); return Task.CompletedTask; }
    }

    private sealed class FailingDistributedCache : IDistributedCache
    {
        public byte[]? Get(string key) => throw new TimeoutException("Redis connection timed out.");
        public Task<byte[]?> GetAsync(string key, CancellationToken token = default) => throw new TimeoutException("Redis connection timed out.");
        public void Set(string key, byte[] value, DistributedCacheEntryOptions options) => throw new TimeoutException("Redis connection timed out.");
        public Task SetAsync(string key, byte[] value, DistributedCacheEntryOptions options, CancellationToken token = default) => throw new TimeoutException("Redis connection timed out.");
        public void Refresh(string key) { }
        public Task RefreshAsync(string key, CancellationToken token = default) => Task.CompletedTask;
        public void Remove(string key) { }
        public Task RemoveAsync(string key, CancellationToken token = default) => Task.CompletedTask;
    }

    private sealed class FakeWebHostEnvironment : IWebHostEnvironment
    {
        public string WebRootPath { get; set; } = string.Empty;
        public Microsoft.Extensions.FileProviders.IFileProvider WebRootFileProvider { get; set; } = null!;
        public string ApplicationName { get; set; } = "EmployeeManagement";
        public Microsoft.Extensions.FileProviders.IFileProvider ContentRootFileProvider { get; set; } = null!;
        public string ContentRootPath { get; set; } = string.Empty;
        public string EnvironmentName { get; set; } = "Testing";
    }
}
