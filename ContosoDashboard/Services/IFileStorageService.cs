namespace ContosoDashboard.Services;

public interface IFileStorageService
{
    Task<string> UploadAsync(Stream fileStream, string originalFileName, string contentType, string logicalPath);
    Task DeleteAsync(string storedFilePath);
    Task<Stream> DownloadAsync(string storedFilePath);
    Task<string> GetUrlAsync(string storedFilePath, TimeSpan expiration);
}
