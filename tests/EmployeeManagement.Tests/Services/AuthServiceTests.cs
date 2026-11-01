using EmployeeManagement.Application.DTOs;
using EmployeeManagement.Application.Services;
using EmployeeManagement.Domain.Constants;
using EmployeeManagement.Domain.Entities;
using EmployeeManagement.Infrastructure.Authentication;
using EmployeeManagement.Tests.Fakes;
using Microsoft.Extensions.Options;
using Xunit;

namespace EmployeeManagement.Tests.Services;

public class AuthServiceTests
{
    private readonly AuthService _authService;
    private readonly FakeUserRepository _repo;
    private readonly PasswordHasher _hasher;

    public AuthServiceTests()
    {
        _hasher = new PasswordHasher();
        _repo = new FakeUserRepository();

        // Seed admin user
        var admin = User.Create("admin", "admin@company.com", _hasher.Hash("Password123!"), Roles.Admin);
        _repo.AddAsync(admin).Wait();

        var jwtSettings = new JwtSettings
        {
            Secret = "Super_Secret_Testing_Key_At_Least_32_Bytes_Long!",
            Issuer = "TestIssuer",
            Audience = "TestAudience",
            ExpiryMinutes = 60
        };
        var tokenGenerator = new JwtTokenGenerator(Options.Create(jwtSettings));

        _authService = new AuthService(_repo, _hasher, tokenGenerator);
    }

    [Fact]
    public async Task LoginAsync_WithPreSeededAdmin_ShouldSucceed()
    {
        var request = new LoginRequest("admin", "Password123!");
        var response = await _authService.LoginAsync(request);

        Assert.NotNull(response);
        Assert.Equal("admin", response.Username);
        Assert.Equal(Roles.Admin, response.Role);
        Assert.False(string.IsNullOrWhiteSpace(response.Token));
    }

    [Fact]
    public async Task LoginAsync_WithWrongPassword_ShouldThrowUnauthorized()
    {
        var request = new LoginRequest("admin", "WrongPass!");

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _authService.LoginAsync(request));
    }

    [Fact]
    public async Task RegisterAsync_WithValidData_ShouldCreateUserAndReturnToken()
    {
        var request = new RegisterRequest("newuser", "newuser@test.com", "Password123!", Roles.Viewer);

        var response = await _authService.RegisterAsync(request);

        Assert.NotNull(response);
        Assert.Equal("newuser", response.Username);
        Assert.Equal(Roles.Viewer, response.Role);
        Assert.False(string.IsNullOrWhiteSpace(response.Token));
    }

    [Fact]
    public async Task RegisterAsync_WithDuplicateUsername_ShouldThrowInvalidOperation()
    {
        var request = new RegisterRequest("admin", "different@test.com", "Password123!", Roles.Viewer);

        await Assert.ThrowsAsync<InvalidOperationException>(() => _authService.RegisterAsync(request));
    }

    [Fact]
    public async Task RegisterAsync_WithInvalidRole_ShouldThrowArgumentException()
    {
        var request = new RegisterRequest("john", "john@test.com", "Password123!", "SuperHero");

        await Assert.ThrowsAsync<ArgumentException>(() => _authService.RegisterAsync(request));
    }
}
