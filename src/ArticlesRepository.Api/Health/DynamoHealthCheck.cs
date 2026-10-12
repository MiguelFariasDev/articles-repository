using Amazon.DynamoDBv2;
using ArticlesRepository.Infrastructure.Aws;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace ArticlesRepository.Api.Health;

public class DynamoHealthCheck : IHealthCheck
{
    private readonly IAmazonDynamoDB _dynamo;
    private readonly AwsOptions _options;

    public DynamoHealthCheck(IAmazonDynamoDB dynamo, IOptions<AwsOptions> options)
    {
        _dynamo = dynamo;
        _options = options.Value;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken ct = default)
    {
        try
        {
            // DescribeTable na tabela do projeto: a role da EC2 (ex.: Learner Lab) pode não ter ListTables.
            await _dynamo.DescribeTableAsync(_options.DynamoDbTable, ct);
            return HealthCheckResult.Healthy();
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("DynamoDB indisponível", ex);
        }
    }
}
