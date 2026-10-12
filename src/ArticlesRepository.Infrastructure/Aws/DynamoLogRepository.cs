using System.Text.Json;
using Amazon.DynamoDBv2.DataModel;
using ArticlesRepository.Application.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ArticlesRepository.Infrastructure.Aws;

public class DynamoLogRepository : IArticleLogRepository
{
    private static readonly TimeSpan Retention = TimeSpan.FromDays(90);
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly IDynamoDBContext _context;
    private readonly AwsOptions _options;
    private readonly ILogger<DynamoLogRepository> _logger;

    public DynamoLogRepository(IDynamoDBContext context, IOptions<AwsOptions> options, ILogger<DynamoLogRepository> logger)
    {
        _context = context;
        _options = options.Value;
        _logger = logger;
    }

    /// <summary>Falha em logar nunca derruba a operação principal.</summary>
    public async Task LogAsync(string acao, object? dados, Guid? articleId, string? ip, CancellationToken ct)
    {
        try
        {
            var now = DateTime.UtcNow;
            var entry = new ArticleLogEntry
            {
                Timestamp = now.ToString("o"),
                Acao = acao,
                ArticleId = articleId?.ToString(),
                DadosManipulados = JsonSerializer.Serialize(dados, JsonOptions),
                UsuarioIp = ip,
                Ttl = new DateTimeOffset(now.Add(Retention)).ToUnixTimeSeconds()
            };

            var config = new SaveConfig();
            if (!string.IsNullOrWhiteSpace(_options.DynamoDbTable))
            {
                config.OverrideTableName = _options.DynamoDbTable;
            }

            await _context.SaveAsync(entry, config, ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Falha ao gravar log {Acao} do artigo {ArticleId} no DynamoDB", acao, articleId);
        }
    }
}
