using Amazon.S3;
using Amazon.S3.Model;
using ArticlesRepository.Application.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ArticlesRepository.Infrastructure.Aws;

public class S3StorageService : IS3StorageService
{
    private readonly IAmazonS3 _s3;
    private readonly AwsOptions _options;
    private readonly ILogger<S3StorageService> _logger;

    public S3StorageService(IAmazonS3 s3, IOptions<AwsOptions> options, ILogger<S3StorageService> logger)
    {
        _s3 = s3;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<string> UploadAsync(Stream stream, string fileName, string contentType, CancellationToken ct)
    {
        var key = $"articles/{Guid.NewGuid()}-{Sanitize(fileName)}";
        try
        {
            var request = new PutObjectRequest
            {
                BucketName = _options.S3Bucket,
                Key = key,
                InputStream = stream,
                ContentType = contentType,
                AutoCloseStream = false
            };

            await _s3.PutObjectAsync(request, ct);
            _logger.LogInformation("PDF enviado ao S3: {Bucket}/{Key}", _options.S3Bucket, key);
            return key;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Falha no upload para o S3: {Bucket}/{Key}", _options.S3Bucket, key);
            throw;
        }
    }

    public async Task<Stream> DownloadAsync(string key, CancellationToken ct)
    {
        try
        {
            using var response = await _s3.GetObjectAsync(_options.S3Bucket, key, ct);
            var buffer = new MemoryStream();
            await response.ResponseStream.CopyToAsync(buffer, ct);
            buffer.Position = 0;
            return buffer;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Falha no download do S3: {Bucket}/{Key}", _options.S3Bucket, key);
            throw;
        }
    }

    public async Task DeleteAsync(string key, CancellationToken ct)
    {
        try
        {
            await _s3.DeleteObjectAsync(_options.S3Bucket, key, ct);
            _logger.LogInformation("PDF removido do S3: {Bucket}/{Key}", _options.S3Bucket, key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Falha ao remover do S3: {Bucket}/{Key}", _options.S3Bucket, key);
            throw;
        }
    }

    public async Task<string> GetPresignedUrlAsync(string key, TimeSpan expiresIn, CancellationToken ct)
    {
        try
        {
            var request = new GetPreSignedUrlRequest
            {
                BucketName = _options.S3Bucket,
                Key = key,
                Verb = HttpVerb.GET,
                // LocalStack em http:// precisa de URL http; na AWS real o padrão é https.
                Protocol = _options.ServiceUrl?.StartsWith("http://", StringComparison.OrdinalIgnoreCase) == true ? Protocol.HTTP : Protocol.HTTPS,
                Expires = DateTime.UtcNow.Add(expiresIn)
            };

            return await _s3.GetPreSignedURLAsync(request);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Falha ao gerar URL pré-assinada: {Bucket}/{Key}", _options.S3Bucket, key);
            throw;
        }
    }

    private static string Sanitize(string fileName)
    {
        var name = Path.GetFileName(fileName);
        var clean = new string(name.Select(c => char.IsLetterOrDigit(c) || c is '.' or '-' or '_' ? c : '_').ToArray());
        return string.IsNullOrWhiteSpace(clean) ? "article.pdf" : clean;
    }
}
