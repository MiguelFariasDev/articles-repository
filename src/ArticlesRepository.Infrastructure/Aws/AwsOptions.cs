namespace ArticlesRepository.Infrastructure.Aws;

public class AwsOptions
{
    public const string SectionName = "AWS";

    public string Region { get; set; } = "us-east-1";
    public string? ServiceUrl { get; set; }     // LocalStack em dev
    public string S3Bucket { get; set; } = "";
    public string SnsTopicArn { get; set; } = "";
    public string SqsQueueUrl { get; set; } = "";
    public string DynamoDbTable { get; set; } = "";
    public bool UseLocalStack => !string.IsNullOrWhiteSpace(ServiceUrl);
}
