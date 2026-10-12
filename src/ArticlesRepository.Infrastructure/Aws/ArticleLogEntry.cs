using Amazon.DynamoDBv2.DataModel;

namespace ArticlesRepository.Infrastructure.Aws;

[DynamoDBTable("articles-repository-logs")]
public class ArticleLogEntry
{
    [DynamoDBHashKey] public string Id { get; set; } = Guid.NewGuid().ToString();
    [DynamoDBRangeKey] public string Timestamp { get; set; } = DateTime.UtcNow.ToString("o");
    [DynamoDBProperty] public string Acao { get; set; } = "";       // CREATE|READ|UPDATE|DELETE
    [DynamoDBProperty] public string? ArticleId { get; set; }
    [DynamoDBProperty] public string DadosManipulados { get; set; } = ""; // JSON
    [DynamoDBProperty] public string? UsuarioIp { get; set; }
    [DynamoDBProperty] public long Ttl { get; set; } // Unix timestamp; expira em 90 dias
}
