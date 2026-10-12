namespace ArticlesRepository.Worker.Services;

public interface IPdfProcessor
{
    Task ProcessAsync(ArticleProcessEvent evt, CancellationToken ct);
}
