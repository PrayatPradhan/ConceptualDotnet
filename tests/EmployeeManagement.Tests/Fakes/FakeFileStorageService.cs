using EmployeeManagement.Application.Interfaces;

namespace EmployeeManagement.Tests.Fakes;

public sealed class FakeFileStorageService : IFileStorageService
{
    public readonly Dictionary<string, byte[]> Files = new();

    public async Task<string> SaveFileAsync(
        Stream stream, 
        string originalFileName, 
        string subFolder, 
        CancellationToken cancellationToken = default)
    {
        using var memoryStream = new MemoryStream();
        await stream.CopyToAsync(memoryStream, cancellationToken);

        var path = $"/uploads/{subFolder}/{Guid.NewGuid():N}_{originalFileName}";
        Files[path] = memoryStream.ToArray();

        return path;
    }

    public Task DeleteFileAsync(string relativePath, CancellationToken cancellationToken = default)
    {
        Files.Remove(relativePath);
        return Task.CompletedTask;
    }
}
