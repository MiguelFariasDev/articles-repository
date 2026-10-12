using System.Text.Json;
using Amazon.SQS;
using Amazon.SQS.Model;
using ArticlesRepository.Infrastructure.Aws;
using ArticlesRepository.Worker.Services;
using Microsoft.Extensions.Options;

namespace ArticlesRepository.Worker.Workers;

public class PdfProcessingWorker : BackgroundService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly IAmazonSQS _sqs;
    private readonly AwsOptions _options;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<PdfProcessingWorker> _logger;

    public PdfProcessingWorker(
        IAmazonSQS sqs,
        IOptions<AwsOptions> options,
        IServiceScopeFactory scopeFactory,
        ILogger<PdfProcessingWorker> logger)
    {
        _sqs = sqs;
        _options = options.Value;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        _logger.LogInformation("Worker iniciado; consumindo {QueueUrl}", _options.SqsQueueUrl);

        while (!ct.IsCancellationRequested)
        {
            try
            {
                var response = await _sqs.ReceiveMessageAsync(new ReceiveMessageRequest
                {
                    QueueUrl = _options.SqsQueueUrl,
                    WaitTimeSeconds = 20,
                    MaxNumberOfMessages = 5,
                    VisibilityTimeout = 300
                }, ct);

                foreach (var message in response.Messages ?? [])
                {
                    await HandleMessageAsync(message, ct);
                }
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao consultar a fila SQS; nova tentativa em 5s");
                await Task.Delay(TimeSpan.FromSeconds(5), ct);
            }
        }

        _logger.LogInformation("Worker encerrado");
    }

    private async Task HandleMessageAsync(Message message, CancellationToken ct)
    {
        try
        {
            var evt = ParseSnsEnvelope(message.Body);

            await using (var scope = _scopeFactory.CreateAsyncScope())
            {
                await scope.ServiceProvider.GetRequiredService<IPdfProcessor>().ProcessAsync(evt, ct);
            }

            await _sqs.DeleteMessageAsync(_options.SqsQueueUrl, message.ReceiptHandle, ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Não deleta: a mensagem volta após o VisibilityTimeout e, ao exceder maxReceiveCount, vai para a DLQ.
            _logger.LogError(ex, "Falha ao processar a mensagem {MessageId}; será reentregue/DLQ", message.MessageId);
        }
    }

    /// <summary>O SNS envelopa o JSON original no campo "Message"; sem envelope (raw delivery) usa o corpo direto.</summary>
    internal static ArticleProcessEvent ParseSnsEnvelope(string body)
    {
        using var document = JsonDocument.Parse(body);
        var payload = document.RootElement.TryGetProperty("Message", out var inner) && inner.ValueKind == JsonValueKind.String
            ? inner.GetString()!
            : body;

        return JsonSerializer.Deserialize<ArticleProcessEvent>(payload, JsonOptions)
            ?? throw new InvalidOperationException("Mensagem SQS sem conteúdo.");
    }
}
