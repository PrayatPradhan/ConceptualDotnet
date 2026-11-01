using System.Collections.Concurrent;
using EmployeeManagement.Domain.Entities;
using EmployeeManagement.Domain.Interfaces;

namespace EmployeeManagement.Tests.Fakes;

public sealed class FakeUserRepository : IUserRepository
{
    private readonly ConcurrentDictionary<Guid, User> _users = new();

    public Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _users.TryGetValue(id, out var user);
        return Task.FromResult(user);
    }

    public Task<User?> GetByUsernameOrEmailAsync(string identifier, CancellationToken cancellationToken = default)
    {
        var normalized = identifier.Trim().ToLowerInvariant();
        var user = _users.Values.FirstOrDefault(u =>
            u.Username.Equals(normalized, StringComparison.OrdinalIgnoreCase) ||
            u.Email.Equals(normalized, StringComparison.OrdinalIgnoreCase));

        return Task.FromResult(user);
    }

    public Task<bool> ExistsByUsernameOrEmailAsync(string username, string email, CancellationToken cancellationToken = default)
    {
        var normalizedUser = username.Trim().ToLowerInvariant();
        var normalizedEmail = email.Trim().ToLowerInvariant();

        var exists = _users.Values.Any(u =>
            u.Username.Equals(normalizedUser, StringComparison.OrdinalIgnoreCase) ||
            u.Email.Equals(normalizedEmail, StringComparison.OrdinalIgnoreCase));

        return Task.FromResult(exists);
    }

    public Task AddAsync(User user, CancellationToken cancellationToken = default)
    {
        _users[user.Id] = user;
        return Task.CompletedTask;
    }
}
