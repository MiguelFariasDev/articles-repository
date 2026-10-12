using Amazon.S3;
using Amazon.S3.Model;
using ArticlesRepository.Infrastructure.Aws;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace ArticlesRepository.Api.Health;

public class S3HealthCheck : IHealthCheck
{
    private readonly IAmazonS3 _s3;
    private readonly AwsOptions _options;

    public S3HealthCheck(IAmazonS3 s3, IOptions<AwsOptions> options)
    {
        _s3 = s3;
        _options = options.Value;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken ct = default)
    {
        try
        {
            // Consulta só o bucket do projeto (não exige s3:ListAllMyBuckets).
            await _s3.ListObjectsV2Async(new ListObjectsV2Request { BucketName = _options.S3Bucket, MaxKeys = 1 }, ct);
            return HealthCheckResult.Healthy();
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("S3 indisponível", ex);
        }
    }
}
