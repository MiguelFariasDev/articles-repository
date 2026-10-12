namespace ArticlesRepository.Application.Interfaces;

public interface IS3StorageService
{
    Task<string> UploadAsync(Stream stream, string fileName, string contentType, CancellationToken ct);
    Task<Stream> DownloadAsync(string key, CancellationToken ct);
    Task DeleteAsync(string key, CancellationToken ct);
    Task<string> GetPresignedUrlAsync(string key, TimeSpan expiresIn, CancellationToken ct);
}
