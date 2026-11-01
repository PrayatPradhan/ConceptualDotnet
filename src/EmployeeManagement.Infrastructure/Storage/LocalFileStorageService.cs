using EmployeeManagement.Application.Interfaces;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;

namespace EmployeeManagement.Infrastructure.Storage;

public sealed class LocalFileStorageService : IFileStorageService
{
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<LocalFileStorageService> _logger;

    public LocalFileStorageService(IWebHostEnvironment environment, ILogger<LocalFileStorageService> logger)
    {
        _environment = environment;
        _logger = logger;
    }

    public async Task<string> SaveFileAsync(
        Stream stream, 
        string originalFileName, 
        string subFolder, 
        CancellationToken cancellationToken = default)
    {
        var extension = Path.GetExtension(originalFileName).ToLowerInvariant();
        var uniqueFileName = $"{Guid.NewGuid():N}{extension}";

        // Root uploads folder under WebRootPath (wwwroot)
        var webRoot = _environment.WebRootPath ?? Path.Combine(_environment.ContentRootPath, "wwwroot");
        var targetDirectory = Path.Combine(webRoot, "uploads", subFolder);

        if (!Directory.Exists(targetDirectory))
        {
            Directory.CreateDirectory(targetDirectory);
        }

        var fullFilePath = Path.Combine(targetDirectory, uniqueFileName);

        await using var outputStream = new FileStream(fullFilePath, FileMode.Create);
        await stream.CopyToAsync(outputStream, cancellationToken);

        _logger.LogInformation("Saved photo to: {Path}", fullFilePath);

        // Return forward-slash relative web URL for frontend consumption
        return $"/uploads/{subFolder}/{uniqueFileName}";
    }

    public Task DeleteFileAsync(string relativePath, CancellationToken cancellationToken = default)
    {
        try
        {
            var webRoot = _environment.WebRootPath ?? Path.Combine(_environment.ContentRootPath, "wwwroot");
            var normalizedRelative = relativePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
            var fullPath = Path.Combine(webRoot, normalizedRelative);

            if (File.Exists(fullPath))
            {
                File.Delete(fullPath);
                _logger.LogInformation("Deleted old photo file: {Path}", fullPath);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to delete file at path: {RelativePath}", relativePath);
        }

        return Task.CompletedTask;
    }
}
