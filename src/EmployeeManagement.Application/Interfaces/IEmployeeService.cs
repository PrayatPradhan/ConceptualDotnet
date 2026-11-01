using EmployeeManagement.Application.Common;
using EmployeeManagement.Application.DTOs;

namespace EmployeeManagement.Application.Interfaces;

public interface IEmployeeService
{
    Task<PagedResult<EmployeeDto>> GetPagedEmployeesAsync(PaginationParams paginationParams, CancellationToken cancellationToken = default);
    Task<IEnumerable<EmployeeDto>> GetAllEmployeesAsync(CancellationToken cancellationToken = default);
    Task<EmployeeDto?> GetEmployeeByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<EmployeeDto> CreateEmployeeAsync(CreateEmployeeRequest request, CancellationToken cancellationToken = default);
    Task<EmployeeDto> UpdateEmployeeAsync(Guid id, UpdateEmployeeRequest request, CancellationToken cancellationToken = default);
    Task DeleteEmployeeAsync(Guid id, CancellationToken cancellationToken = default);
    Task<EmployeeDto> UploadPhotoAsync(Guid employeeId, Stream fileStream, string originalFileName, CancellationToken cancellationToken = default);
    Task<EmployeeDto> DeletePhotoAsync(Guid employeeId, CancellationToken cancellationToken = default);
}
