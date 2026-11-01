using System.Text;
using EmployeeManagement.Application.DTOs;
using EmployeeManagement.Application.Services;
using EmployeeManagement.Domain.Entities;
using EmployeeManagement.Domain.Interfaces;
using EmployeeManagement.Tests.Fakes;
using Xunit;

namespace EmployeeManagement.Tests.Services;

public class EmployeePhotoTests
{
    [Fact]
    public async Task UploadPhotoAsync_ShouldSaveFile_AndUpdateEmployeeProfilePictureUrl()
    {
        var fakeRepo = new FakeEmployeeRepository();
        var fakeCache = new FakeCacheService();
        var fakeStorage = new FakeFileStorageService();

        var emp = Employee.Create("Bob", "Miller", "bob@company.com", "Engineering", 90000);
        await fakeRepo.AddAsync(emp);

        var service = new EmployeeService(fakeRepo, fakeCache, fakeStorage);

        var photoBytes = Encoding.UTF8.GetBytes("fake-image-binary-data");
        using var stream = new MemoryStream(photoBytes);

        var result = await service.UploadPhotoAsync(emp.Id, stream, "bob_profile.png");

        Assert.NotNull(result);
        Assert.NotNull(result.ProfilePictureUrl);
        Assert.Contains(".png", result.ProfilePictureUrl);
        Assert.Single(fakeStorage.Files);
    }

    [Fact]
    public async Task UploadPhotoAsync_WhenReplacingExistingPhoto_ShouldDeleteOldFile()
    {
        var fakeRepo = new FakeEmployeeRepository();
        var fakeCache = new FakeCacheService();
        var fakeStorage = new FakeFileStorageService();

        var emp = Employee.Create("Bob", "Miller", "bob@company.com", "Engineering", 90000);
        await fakeRepo.AddAsync(emp);

        var service = new EmployeeService(fakeRepo, fakeCache, fakeStorage);

        // Upload first photo
        using var stream1 = new MemoryStream(Encoding.UTF8.GetBytes("image1"));
        var firstResult = await service.UploadPhotoAsync(emp.Id, stream1, "photo1.png");
        var firstPhotoPath = firstResult.ProfilePictureUrl!;

        Assert.True(fakeStorage.Files.ContainsKey(firstPhotoPath));

        // Upload replacement photo
        using var stream2 = new MemoryStream(Encoding.UTF8.GetBytes("image2"));
        var secondResult = await service.UploadPhotoAsync(emp.Id, stream2, "photo2.jpg");

        // Old file must be cleaned up from storage
        Assert.False(fakeStorage.Files.ContainsKey(firstPhotoPath));
        // New file must exist
        Assert.True(fakeStorage.Files.ContainsKey(secondResult.ProfilePictureUrl!));
        Assert.Single(fakeStorage.Files);
    }

    [Fact]
    public async Task DeletePhotoAsync_ShouldRemoveFile_AndClearProfilePictureUrl()
    {
        var fakeRepo = new FakeEmployeeRepository();
        var fakeCache = new FakeCacheService();
        var fakeStorage = new FakeFileStorageService();

        var emp = Employee.Create("Bob", "Miller", "bob@company.com", "Engineering", 90000);
        await fakeRepo.AddAsync(emp);

        var service = new EmployeeService(fakeRepo, fakeCache, fakeStorage);

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes("test-photo"));
        var uploaded = await service.UploadPhotoAsync(emp.Id, stream, "avatar.png");
        var photoPath = uploaded.ProfilePictureUrl!;

        Assert.True(fakeStorage.Files.ContainsKey(photoPath));

        // Delete photo
        var deletedResult = await service.DeletePhotoAsync(emp.Id);

        Assert.Null(deletedResult.ProfilePictureUrl);
        Assert.False(fakeStorage.Files.ContainsKey(photoPath));
        Assert.Empty(fakeStorage.Files);
    }

    private sealed class FakeEmployeeRepository : IEmployeeRepository
    {
        private readonly List<Employee> _employees = new();

        public Task<IEnumerable<Employee>> GetAllAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IEnumerable<Employee>>(_employees);

        public Task<(IReadOnlyList<Employee> Items, int TotalCount)> GetPagedAsync(int page, int pageSize, CancellationToken cancellationToken = default)
            => Task.FromResult<(IReadOnlyList<Employee>, int)>((_employees, _employees.Count));

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
