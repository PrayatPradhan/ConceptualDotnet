using System.ComponentModel.DataAnnotations;

namespace EmployeeManagement.Application.DTOs;

public record RegisterRequest(
    [Required, MinLength(3), MaxLength(50)] string Username,
    [Required, EmailAddress] string Email,
    [Required, MinLength(6)] string Password,
    [Required] string Role);

public record LoginRequest(
    [Required] string UsernameOrEmail,
    [Required] string Password);

public record AuthResponse(
    Guid Id,
    string Username,
    string Email,
    string Role,
    string Token);
