using System.ComponentModel.DataAnnotations;

namespace EmployeeManagement.Application.DTOs;

/// <summary>Payload for creating a new employee.</summary>
public record CreateEmployeeRequest(
    [Required, MaxLength(100)] string FirstName,
    [Required, MaxLength(100)] string LastName,
    [Required, EmailAddress]   string Email,
    [Required, MaxLength(100)] string Department,
    [Range(0, double.MaxValue)] decimal Salary);
