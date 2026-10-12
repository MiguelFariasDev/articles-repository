using ArticlesRepository.Application.DTOs;

namespace ArticlesRepository.Application.Interfaces;

public interface IArticleService
{
    Task<IReadOnlyList<ArticleResponse>> GetAllAsync(ArticleQueryParameters q, CancellationToken ct = default);
    Task<ArticleResponse?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<ArticleResponse>> GetByAreaAsync(string area, CancellationToken ct = default);
    Task<ArticleResponse> CreateAsync(CreateArticleRequest req, Stream pdf, string fileName, string contentType, CancellationToken ct = default);
    Task<ArticleResponse?> UpdateAsync(Guid id, UpdateArticleRequest req, Stream? pdf = null, string? fileName = null, string? contentType = null, CancellationToken ct = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken ct = default);

    /// <summary>URL pré-assinada (15 min) do PDF; null se o artigo não existe ou não tem PDF.</summary>
    Task<string?> GetDownloadUrlAsync(Guid id, CancellationToken ct = default);
}
