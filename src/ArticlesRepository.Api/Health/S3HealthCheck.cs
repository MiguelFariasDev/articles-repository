using Amazon.S3;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace ArticlesRepository.Api.Health;

public class S3HealthCheck : IHealthCheck
{
    private readonly IAmazonS3 _s3;

    public S3HealthCheck(IAmazonS3 s3)
    {
        _s3 = s3;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken ct = default)
    {
        try
        {
            await _s3.ListBucketsAsync(ct);
            return HealthCheckResult.Healthy();
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("S3 indisponível", ex);
        }
    }
}
