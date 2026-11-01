using EmployeeManagement.Application.Interfaces;
using EmployeeManagement.Domain.Constants;
using EmployeeManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EmployeeManagement.Infrastructure.Persistence;

public sealed class DatabaseInitializer
{
    private readonly ApplicationDbContext _context;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ILogger<DatabaseInitializer> _logger;

    public DatabaseInitializer(
        ApplicationDbContext context,
        IPasswordHasher passwordHasher,
        ILogger<DatabaseInitializer> logger)
    {
        _context = context;
        _passwordHasher = passwordHasher;
        _logger = logger;
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // Seed initial administrative accounts if empty
            if (!await _context.Users.AnyAsync(cancellationToken))
            {
                _logger.LogInformation("Seeding initial users into SQL Server database...");

                var defaultPassword = "Password123!";
                var hash = _passwordHasher.Hash(defaultPassword);

                var users = new[]
                {
                    User.Create("admin", "admin@company.com", hash, Roles.Admin),
                    User.Create("hrmanager", "hr@company.com", hash, Roles.HrManager),
                    User.Create("viewer", "viewer@company.com", hash, Roles.Viewer)
                };

                await _context.Users.AddRangeAsync(users, cancellationToken);
                await _context.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("Successfully seeded initial users.");
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Database connection could not be established or database is not reachable. Skipping initial seeding.");
        }
    }
}
