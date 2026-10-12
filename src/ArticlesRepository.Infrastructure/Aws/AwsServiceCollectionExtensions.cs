using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Amazon.S3;
using Amazon.SimpleNotificationService;
using Amazon.SQS;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ArticlesRepository.Infrastructure.Aws;

public static class AwsServiceCollectionExtensions
{
    public static IServiceCollection AddAwsServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<AwsOptions>(configuration.GetSection(AwsOptions.SectionName));

        services.AddSingleton<AwsClientFactory>();
        services.AddSingleton<IAmazonS3>(sp => sp.GetRequiredService<AwsClientFactory>().CreateS3());
        services.AddSingleton<IAmazonSimpleNotificationService>(sp => sp.GetRequiredService<AwsClientFactory>().CreateSns());
        services.AddSingleton<IAmazonSQS>(sp => sp.GetRequiredService<AwsClientFactory>().CreateSqs());
        services.AddSingleton<IAmazonDynamoDB>(sp => sp.GetRequiredService<AwsClientFactory>().CreateDynamoDb());

        services.AddScoped<IDynamoDBContext>(sp =>
            new DynamoDBContextBuilder()
                .WithDynamoDBClient(() => sp.GetRequiredService<IAmazonDynamoDB>())
                .Build());

        return services;
    }
}
