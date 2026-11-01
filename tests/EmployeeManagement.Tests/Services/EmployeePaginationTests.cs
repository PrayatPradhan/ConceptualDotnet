using EmployeeManagement.Application.Common;
using EmployeeManagement.Application.DTOs;
using EmployeeManagement.Application.Services;
using EmployeeManagement.Domain.Entities;
using EmployeeManagement.Domain.Interfaces;
using EmployeeManagement.Tests.Fakes;
using Xunit;

namespace EmployeeManagement.Tests.Services;

public class EmployeePaginationTests
{
    [Fact]
    public void PaginationParams_DefaultValues_ShouldBePage1AndPageSize10()
    {
        var parameters = new PaginationParams();

        Assert.Equal(1, parameters.Page);
        Assert.Equal(10, parameters.PageSize);
    }

    [Fact]
    public void PaginationParams_InvalidInputs_ShouldFallbackToDefaults()
    {
        var parameters = new PaginationParams { Page = -5, PageSize = -20 };

        Assert.Equal(1, parameters.Page);
        Assert.Equal(10, parameters.PageSize);
    }

    [Fact]
    public void PaginationParams_ExcessivePageSize_ShouldBeClampedTo100()
    {
        var parameters = new PaginationParams { PageSize = 5000 };

        Assert.Equal(100, parameters.PageSize);
    }

    [Theory]
    [InlineData(25, 10, 1, 3, false, true)]
    [InlineData(25, 10, 2, 3, true, true)]
    [InlineData(25, 10, 3, 3, true, false)]
    [InlineData(10, 10, 1, 1, false, false)]
    public void PagedResult_Metadata_CalculatesCorrectly(
        int totalCount, int pageSize, int page, int expectedTotalPages, bool expectedHasPrev, bool expectedHasNext)
    {
        var items = new List<string>();
        var result = new PagedResult<string>(items, page, pageSize, totalCount);

        Assert.Equal(expectedTotalPages, result.TotalPages);
        Assert.Equal(expectedHasPrev, result.HasPreviousPage);
        Assert.Equal(expectedHasNext, result.HasNextPage);
    }

    [Fact]
    public async Task GetPagedEmployeesAsync_ShouldReturnCorrectPagedResultAndPopulateCache()
    {
        var fakeRepo = new FakeEmployeeRepository();
        var fakeCache = new FakeCacheService();
        var fakeStorage = new FakeFileStorageService();

        for (int i = 1; i <= 15; i++)
        {
            var emp = Employee.Create($"First{i}", $"Last{i}", $"emp{i}@company.com", "IT", 50000 + (i * 1000));
            await fakeRepo.AddAsync(emp);
        }

        var service = new EmployeeService(fakeRepo, fakeCache, fakeStorage);
        var pagination = new PaginationParams { Page = 2, PageSize = 5 };

        // 1. Initial call (Cache Miss -> DB Read -> Cache Populate)
        var result = await service.GetPagedEmployeesAsync(pagination);

        Assert.Equal(5, result.Items.Count);
        Assert.Equal(2, result.Page);
        Assert.Equal(5, result.PageSize);
        Assert.Equal(15, result.TotalCount);

        // Verify cache was populated
        Assert.True(fakeCache.ContainsKey("employees:paged:2:5"));

        // 2. Second call should return from cache without re-querying repo
        var cachedResult = await service.GetPagedEmployeesAsync(pagination);
        Assert.NotNull(cachedResult);
        Assert.Equal(5, cachedResult.Items.Count);
    }

    [Fact]
    public async Task GetEmployeeByIdAsync_ShouldCacheEntity_AndInvalidateOnUpdate()
    {
        var fakeRepo = new FakeEmployeeRepository();
        var fakeCache = new FakeCacheService();
        var fakeStorage = new FakeFileStorageService();

        var emp = Employee.Create("Alice", "Smith", "alice@company.com", "Finance", 70000);
        await fakeRepo.AddAsync(emp);

        var service = new EmployeeService(fakeRepo, fakeCache, fakeStorage);

        // 1. Query employee -> caches item
        var dto = await service.GetEmployeeByIdAsync(emp.Id);
        Assert.NotNull(dto);
        Assert.True(fakeCache.ContainsKey($"employees:{emp.Id}"));

        // 2. Update employee -> cache is evicted
        var updateRequest = new UpdateEmployeeRequest("Alice", "Johnson", "alice@company.com", "Finance", 75000);
        await service.UpdateEmployeeAsync(emp.Id, updateRequest);

        // Key should have been evicted from cache
        Assert.False(fakeCache.ContainsKey($"employees:{emp.Id}"));
    }

    private sealed class FakeEmployeeRepository : IEmployeeRepository
    {
        private readonly List<Employee> _employees = new();

        public Task<IEnumerable<Employee>> GetAllAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IEnumerable<Employee>>(_employees);

        public Task<(IReadOnlyList<Employee> Items, int TotalCount)> GetPagedAsync(int page, int pageSize, CancellationToken cancellationToken = default)
        {
            var total = _employees.Count;
            var items = _employees
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            return Task.FromResult< (IReadOnlyList<Employee>, int) >((items, total));
        }

        public Task<Employee?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
            => Task.FromResult(_employees.FirstOrDefault(e => e.Id == id));

        public Task AddAsync(Employee employee, CancellationToken cancellationToken = default)
        {
            _employees.Add(employee);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(Employee employee, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task DeleteAsync(Guid id, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<bool> ExistsByEmailAsync(string email, CancellationToken cancellationToken = default)
            => Task.FromResult(_employees.Any(e => e.Email.Equals(email, StringComparison.OrdinalIgnoreCase)));
    }
}
