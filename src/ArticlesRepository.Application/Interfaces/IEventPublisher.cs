namespace ArticlesRepository.Application.Interfaces;

public interface IEventPublisher
{
    Task PublishArticleCreatedAsync(Guid articleId, string pdfS3Key, CancellationToken ct);
    Task PublishArticleUpdatedAsync(Guid articleId, string pdfS3Key, CancellationToken ct);
}
