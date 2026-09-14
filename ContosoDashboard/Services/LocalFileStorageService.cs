namespace ContosoDashboard.Services;

public class LocalFileStorageService : IFileStorageService
{
    private readonly string _rootPath;

    public LocalFileStorageService()
    {
        _rootPath = Path.Combine(Directory.GetCurrentDirectory(), "AppData", "uploads");
        Directory.CreateDirectory(_rootPath);
    }

    public async Task<string> UploadAsync(Stream fileStream, string originalFileName, string contentType, string logicalPath)
    {
        if (string.IsNullOrWhiteSpace(logicalPath))
            throw new InvalidOperationException("A storage path is required.");

        var relativePath = logicalPath.Replace('/', Path.DirectorySeparatorChar).Replace('\\', Path.DirectorySeparatorChar);
        var fullPath = Path.Combine(_rootPath, relativePath);
        var directory = Path.GetDirectoryName(fullPath);

        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        await using var output = new FileStream(fullPath, FileMode.Create, FileAccess.Write, FileShare.None);
        await fileStream.CopyToAsync(output);

        return logicalPath.Replace('\\', '/');
    }

    public Task DeleteAsync(string storedFilePath)
    {
        if (string.IsNullOrWhiteSpace(storedFilePath))
            return Task.CompletedTask;

        var normalized = storedFilePath.Replace('/', Path.DirectorySeparatorChar).Replace('\\', Path.DirectorySeparatorChar);
        var fullPath = Path.Combine(_rootPath, normalized);

        if (File.Exists(fullPath))
        {
            File.Delete(fullPath);
        }

        return Task.CompletedTask;
    }

    public async Task<Stream> DownloadAsync(string storedFilePath)
    {
        var normalized = storedFilePath.Replace('/', Path.DirectorySeparatorChar).Replace('\\', Path.DirectorySeparatorChar);
        var fullPath = Path.Combine(_rootPath, normalized);

        if (!File.Exists(fullPath))
            throw new FileNotFoundException("The requested document file was not found.");

        return await Task.FromResult<Stream>(new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read));
    }

    public Task<string> GetUrlAsync(string storedFilePath, TimeSpan expiration)
    {
        return Task.FromResult($"/documents/download?path={Uri.EscapeDataString(storedFilePath)}");
    }
}
