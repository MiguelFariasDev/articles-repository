using Amazon.DynamoDBv2;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace ArticlesRepository.Api.Health;

public class DynamoHealthCheck : IHealthCheck
{
    private readonly IAmazonDynamoDB _dynamo;

    public DynamoHealthCheck(IAmazonDynamoDB dynamo)
    {
        _dynamo = dynamo;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken ct = default)
    {
        try
        {
            await _dynamo.ListTablesAsync(ct);
            return HealthCheckResult.Healthy();
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("DynamoDB indisponível", ex);
        }
    }
}
