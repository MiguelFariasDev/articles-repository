namespace ArticlesRepository.Worker.Services;

public record ArticleProcessEvent(Guid ArticleId, string PdfS3Key);
