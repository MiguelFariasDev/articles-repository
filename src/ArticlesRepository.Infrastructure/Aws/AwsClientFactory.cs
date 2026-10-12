using Amazon;
using Amazon.DynamoDBv2;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.SimpleNotificationService;
using Amazon.SQS;
using Microsoft.Extensions.Options;

namespace ArticlesRepository.Infrastructure.Aws;

/// <summary>Único lugar onde os clientes AWS são instanciados. Em produção usa a cadeia padrão de credenciais (IAM role).</summary>
public class AwsClientFactory
{
    private readonly AwsOptions _options;

    public AwsClientFactory(IOptions<AwsOptions> options)
    {
        _options = options.Value;
    }

    public IAmazonS3 CreateS3()
    {
        var config = Configure(new AmazonS3Config { ForcePathStyle = _options.UseLocalStack });
        if (_options.UseLocalStack)
        {
            config.RequestChecksumCalculation = RequestChecksumCalculation.WHEN_REQUIRED;
            config.ResponseChecksumValidation = ResponseChecksumValidation.WHEN_REQUIRED;
        }

        return _options.UseLocalStack ? new AmazonS3Client(LocalCredentials(), config) : new AmazonS3Client(config);
    }

    public IAmazonSimpleNotificationService CreateSns()
    {
        var config = Configure(new AmazonSimpleNotificationServiceConfig());
        return _options.UseLocalStack
            ? new AmazonSimpleNotificationServiceClient(LocalCredentials(), config)
            : new AmazonSimpleNotificationServiceClient(config);
    }

    public IAmazonSQS CreateSqs()
    {
        var config = Configure(new AmazonSQSConfig());
        return _options.UseLocalStack ? new AmazonSQSClient(LocalCredentials(), config) : new AmazonSQSClient(config);
    }

    public IAmazonDynamoDB CreateDynamoDb()
    {
        var config = Configure(new AmazonDynamoDBConfig());
        return _options.UseLocalStack ? new AmazonDynamoDBClient(LocalCredentials(), config) : new AmazonDynamoDBClient(config);
    }

    private T Configure<T>(T config) where T : ClientConfig
    {
        if (_options.UseLocalStack)
        {
            config.ServiceURL = _options.ServiceUrl;
            config.AuthenticationRegion = _options.Region;
        }
        else
        {
            config.RegionEndpoint = RegionEndpoint.GetBySystemName(_options.Region);
        }

        return config;
    }

    private static BasicAWSCredentials LocalCredentials() => new("test", "test");
}
