using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace EmployeeManagement.Infrastructure.Health;

/// <summary>
/// Verifies that the file storage directory (uploads) is writable and readable.
/// </summary>
public sealed class FileStorageHealthCheck : IHealthCheck
{
    private readonly IWebHostEnvironment _environment;

    public FileStorageHealthCheck(IWebHostEnvironment environment)
    {
        _environment = environment;
    }

    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            var webRoot = _environment.WebRootPath ?? Path.Combine(_environment.ContentRootPath, "wwwroot");
            var uploadsDir = Path.Combine(webRoot, "uploads");

            if (!Directory.Exists(uploadsDir))
            {
                Directory.CreateDirectory(uploadsDir);
            }

            // Write and delete a small temp file to verify write permissions
            var probePath = Path.Combine(uploadsDir, $".probe_{Guid.NewGuid():N}.tmp");
            File.WriteAllText(probePath, "probe");
            File.Delete(probePath);

            return Task.FromResult(HealthCheckResult.Healthy("File storage directory is accessible and writable."));
        }
        catch (Exception ex)
        {
            return Task.FromResult(HealthCheckResult.Unhealthy("File storage directory is not accessible or writable.", ex));
        }
    }
}
