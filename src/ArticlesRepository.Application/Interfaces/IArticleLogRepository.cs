namespace ArticlesRepository.Application.Interfaces;

public interface IArticleLogRepository
{
    Task LogAsync(string acao, object? dados, Guid? articleId, string? ip, CancellationToken ct);
}
