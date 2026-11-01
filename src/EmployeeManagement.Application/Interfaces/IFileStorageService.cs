namespace EmployeeManagement.Application.Interfaces;

/// <summary>
/// Abstraction for storing and deleting uploaded files.
/// In local environments, stores files to disk.
/// In cloud environments, this can be swapped with Azure Blob or AWS S3 without touching Application/API code.
/// </summary>
public interface IFileStorageService
{
    Task<string> SaveFileAsync(
        Stream stream, 
        string originalFileName, 
        string subFolder, 
        CancellationToken cancellationToken = default);

    Task DeleteFileAsync(string relativePath, CancellationToken cancellationToken = default);
}
