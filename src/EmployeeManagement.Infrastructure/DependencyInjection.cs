using EmployeeManagement.Application.Interfaces;
using EmployeeManagement.Application.Services;
using EmployeeManagement.Domain.Interfaces;
using EmployeeManagement.Infrastructure.Authentication;
using EmployeeManagement.Infrastructure.Caching;
using EmployeeManagement.Infrastructure.Persistence;
using EmployeeManagement.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EmployeeManagement.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // 1. Database Context (SQL Server)
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' was not found.");

        services.AddDbContext<ApplicationDbContext>(options =>
        {
            options.UseSqlServer(connectionString, sqlOptions =>
            {
                sqlOptions.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName);
                sqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 3,
                    maxRetryDelay: TimeSpan.FromSeconds(5),
                    errorNumbersToAdd: null);
            });
        });

        // 2. Distributed Caching (Redis)
        var redisConnectionString = configuration.GetConnectionString("Redis") ?? "localhost:6379,abortConnect=false";
        services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = redisConnectionString;
            options.InstanceName = "EmployeeManagement:"; // Namespaces all keys in Redis
        });
        services.AddSingleton<ICacheService, RedisCacheService>();

        // 3. JWT configuration options
        services.Configure<JwtSettings>(configuration.GetSection(JwtSettings.SectionName));

        // 4. Security & Storage utilities
        services.AddSingleton<IPasswordHasher, PasswordHasher>();
        services.AddSingleton<IJwtTokenGenerator, JwtTokenGenerator>();
        services.AddScoped<IFileStorageService, EmployeeManagement.Infrastructure.Storage.LocalFileStorageService>();

        // 5. Persistence Repositories & Initializer (Scoped lifecycle for DbContext)
        services.AddScoped<IEmployeeRepository, SqlEmployeeRepository>();
        services.AddScoped<IUserRepository, SqlUserRepository>();
        services.AddScoped<DatabaseInitializer>();

        // 6. Application use-case services
        services.AddScoped<IEmployeeService, EmployeeService>();
        services.AddScoped<IAuthService, AuthService>();

        // 7. Health Checks (Liveness and Readiness Probes)
        services.AddHealthChecks()
            .AddCheck("self", () => Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy("API process is running."), tags: ["live"])
            .AddCheck<EmployeeManagement.Infrastructure.Health.SqlDatabaseHealthCheck>("sqlserver", tags: ["ready", "db"])
            .AddCheck<EmployeeManagement.Infrastructure.Health.RedisHealthCheck>("redis", tags: ["ready", "cache"])
            .AddCheck<EmployeeManagement.Infrastructure.Health.FileStorageHealthCheck>("filestorage", tags: ["ready", "storage"]);

        return services;
    }
}
