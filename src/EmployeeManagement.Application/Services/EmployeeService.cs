using EmployeeManagement.Application.Common;
using EmployeeManagement.Application.DTOs;
using EmployeeManagement.Application.Interfaces;
using EmployeeManagement.Domain.Entities;
using EmployeeManagement.Domain.Interfaces;

namespace EmployeeManagement.Application.Services;

/// <summary>
/// Orchestrates employee use-cases including photo management.
/// Implements the Cache-Aside pattern via ICacheService and file persistence via IFileStorageService.
/// </summary>
public sealed class EmployeeService : IEmployeeService
{
    private readonly IEmployeeRepository _repository;
    private readonly ICacheService _cache;
    private readonly IFileStorageService _fileStorage;

    private static readonly TimeSpan EntityCacheDuration = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan ListCacheDuration = TimeSpan.FromMinutes(2);

    public EmployeeService(
        IEmployeeRepository repository, 
        ICacheService cache,
        IFileStorageService fileStorage)
    {
        _repository = repository;
        _cache = cache;
        _fileStorage = fileStorage;
    }

    public async Task<PagedResult<EmployeeDto>> GetPagedEmployeesAsync(
        PaginationParams paginationParams, 
        CancellationToken cancellationToken = default)
    {
        var cacheKey = $"employees:paged:{paginationParams.Page}:{paginationParams.PageSize}";

        var cachedResult = await _cache.GetAsync<PagedResult<EmployeeDto>>(cacheKey, cancellationToken);
        if (cachedResult is not null)
        {
            return cachedResult;
        }

        var (employees, totalCount) = await _repository.GetPagedAsync(
            paginationParams.Page,
            paginationParams.PageSize,
            cancellationToken);

        var dtos = employees.Select(MapToDto).ToList();

        var result = new PagedResult<EmployeeDto>(
            dtos,
            paginationParams.Page,
            paginationParams.PageSize,
            totalCount);

        await _cache.SetAsync(cacheKey, result, ListCacheDuration, cancellationToken);

        return result;
    }

    public async Task<IEnumerable<EmployeeDto>> GetAllEmployeesAsync(CancellationToken cancellationToken = default)
    {
        const string cacheKey = "employees:all";

        var cached = await _cache.GetAsync<List<EmployeeDto>>(cacheKey, cancellationToken);
        if (cached is not null)
        {
            return cached;
        }

        var employees = await _repository.GetAllAsync(cancellationToken);
        var dtos = employees.Select(MapToDto).ToList();

        await _cache.SetAsync(cacheKey, dtos, ListCacheDuration, cancellationToken);

        return dtos;
    }

    public async Task<EmployeeDto?> GetEmployeeByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var cacheKey = $"employees:{id}";

        var cached = await _cache.GetAsync<EmployeeDto>(cacheKey, cancellationToken);
        if (cached is not null)
        {
            return cached;
        }

        var employee = await _repository.GetByIdAsync(id, cancellationToken);
        if (employee is null)
        {
            return null;
        }

        var dto = MapToDto(employee);
        await _cache.SetAsync(cacheKey, dto, EntityCacheDuration, cancellationToken);

        return dto;
    }

    public async Task<EmployeeDto> CreateEmployeeAsync(CreateEmployeeRequest request, CancellationToken cancellationToken = default)
    {
        var emailTaken = await _repository.ExistsByEmailAsync(request.Email, cancellationToken);
        if (emailTaken)
            throw new InvalidOperationException($"An employee with email '{request.Email}' already exists.");

        var employee = Employee.Create(
            request.FirstName,
            request.LastName,
            request.Email,
            request.Department,
            request.Salary);

        await _repository.AddAsync(employee, cancellationToken);
        await InvalidateCollectionCachesAsync(cancellationToken);

        return MapToDto(employee);
    }

    public async Task<EmployeeDto> UpdateEmployeeAsync(Guid id, UpdateEmployeeRequest request, CancellationToken cancellationToken = default)
    {
        var employee = await _repository.GetByIdAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException($"Employee with id '{id}' was not found.");

        if (!employee.Email.Equals(request.Email, StringComparison.OrdinalIgnoreCase))
        {
            var emailTaken = await _repository.ExistsByEmailAsync(request.Email, cancellationToken);
            if (emailTaken)
                throw new InvalidOperationException($"An employee with email '{request.Email}' already exists.");
        }

        employee.Update(
            request.FirstName,
            request.LastName,
            request.Email,
            request.Department,
            request.Salary);

        await _repository.UpdateAsync(employee, cancellationToken);

        await _cache.RemoveAsync($"employees:{id}", cancellationToken);
        await InvalidateCollectionCachesAsync(cancellationToken);

        return MapToDto(employee);
    }

    public async Task DeleteEmployeeAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var employee = await _repository.GetByIdAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException($"Employee with id '{id}' was not found.");

        // Delete associated photo file if one exists
        if (!string.IsNullOrWhiteSpace(employee.ProfilePictureUrl))
        {
            await _fileStorage.DeleteFileAsync(employee.ProfilePictureUrl, cancellationToken);
        }

        await _repository.DeleteAsync(id, cancellationToken);

        await _cache.RemoveAsync($"employees:{id}", cancellationToken);
        await InvalidateCollectionCachesAsync(cancellationToken);
    }

    public async Task<EmployeeDto> UploadPhotoAsync(
        Guid employeeId, 
        Stream fileStream, 
        string originalFileName, 
        CancellationToken cancellationToken = default)
    {
        var employee = await _repository.GetByIdAsync(employeeId, cancellationToken)
            ?? throw new KeyNotFoundException($"Employee with id '{employeeId}' was not found.");

        // Clean up old photo file if replacing an existing one
        if (!string.IsNullOrWhiteSpace(employee.ProfilePictureUrl))
        {
            await _fileStorage.DeleteFileAsync(employee.ProfilePictureUrl, cancellationToken);
        }

        // Save new file to storage
        var relativeUrl = await _fileStorage.SaveFileAsync(
            fileStream, 
            originalFileName, 
            subFolder: "employees", 
            cancellationToken);

        employee.UpdateProfilePicture(relativeUrl);
        await _repository.UpdateAsync(employee, cancellationToken);

        // Invalidate cache
        await _cache.RemoveAsync($"employees:{employeeId}", cancellationToken);
        await InvalidateCollectionCachesAsync(cancellationToken);

        return MapToDto(employee);
    }

    public async Task<EmployeeDto> DeletePhotoAsync(Guid employeeId, CancellationToken cancellationToken = default)
    {
        var employee = await _repository.GetByIdAsync(employeeId, cancellationToken)
            ?? throw new KeyNotFoundException($"Employee with id '{employeeId}' was not found.");

        if (!string.IsNullOrWhiteSpace(employee.ProfilePictureUrl))
        {
            await _fileStorage.DeleteFileAsync(employee.ProfilePictureUrl, cancellationToken);
            employee.UpdateProfilePicture(null);
            await _repository.UpdateAsync(employee, cancellationToken);

            await _cache.RemoveAsync($"employees:{employeeId}", cancellationToken);
            await InvalidateCollectionCachesAsync(cancellationToken);
        }

        return MapToDto(employee);
    }

    private async Task InvalidateCollectionCachesAsync(CancellationToken cancellationToken)
    {
        await _cache.RemoveAsync("employees:all", cancellationToken);
    }

    private static EmployeeDto MapToDto(Employee e) => new(
        e.Id,
        e.FirstName,
        e.LastName,
        e.FullName,
        e.Email,
        e.Department,
        e.Salary,
        e.HiredDate,
        e.ProfilePictureUrl);
}
