namespace EmployeeManagement.Application.DTOs;

/// <summary>Read model returned to callers.</summary>
public record EmployeeDto(
    Guid Id,
    string FirstName,
    string LastName,
    string FullName,
    string Email,
    string Department,
    decimal Salary,
    DateTime HiredDate,
    string? ProfilePictureUrl);
