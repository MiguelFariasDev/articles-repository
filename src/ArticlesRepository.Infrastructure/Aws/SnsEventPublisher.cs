using System.Text.Json;
using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using ArticlesRepository.Application.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ArticlesRepository.Infrastructure.Aws;

public class SnsEventPublisher : IEventPublisher
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly IAmazonSimpleNotificationService _sns;
    private readonly AwsOptions _options;
    private readonly ILogger<SnsEventPublisher> _logger;

    public SnsEventPublisher(IAmazonSimpleNotificationService sns, IOptions<AwsOptions> options, ILogger<SnsEventPublisher> logger)
    {
        _sns = sns;
        _options = options.Value;
        _logger = logger;
    }

    public Task PublishArticleCreatedAsync(Guid articleId, string pdfS3Key, CancellationToken ct) =>
        PublishAsync("article.created", articleId, pdfS3Key, ct);

    public Task PublishArticleUpdatedAsync(Guid articleId, string pdfS3Key, CancellationToken ct) =>
        PublishAsync("article.updated", articleId, pdfS3Key, ct);

    private async Task PublishAsync(string eventType, Guid articleId, string pdfS3Key, CancellationToken ct)
    {
        var body = JsonSerializer.Serialize(new { articleId, pdfS3Key, occurredAt = DateTime.UtcNow }, JsonOptions);

        var request = new PublishRequest
        {
            TopicArn = _options.SnsTopicArn,
            Message = body,
            MessageAttributes = new Dictionary<string, MessageAttributeValue>
            {
                ["eventType"] = new MessageAttributeValue { DataType = "String", StringValue = eventType }
            }
        };

        var response = await _sns.PublishAsync(request, ct);
        _logger.LogInformation("Evento {EventType} publicado no SNS para o artigo {ArticleId} (MessageId {MessageId})",
            eventType, articleId, response.MessageId);
    }
}
