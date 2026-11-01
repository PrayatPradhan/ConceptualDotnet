using EmployeeManagement.Infrastructure.Persistence;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace EmployeeManagement.Infrastructure.Health;

/// <summary>
/// Verifies connectivity to Microsoft SQL Server.
/// If SQL Server cannot be contacted, reports Unhealthy (HTTP 503).
/// </summary>
public sealed class SqlDatabaseHealthCheck : IHealthCheck
{
    private readonly ApplicationDbContext _context;

    public SqlDatabaseHealthCheck(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            var canConnect = await _context.Database.CanConnectAsync(cancellationToken);

            return canConnect
                ? HealthCheckResult.Healthy("SQL Server database connection is active and responsive.")
                : HealthCheckResult.Unhealthy("SQL Server database connection failed.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Exception occurred while pinging SQL Server database.", ex);
        }
    }
}
