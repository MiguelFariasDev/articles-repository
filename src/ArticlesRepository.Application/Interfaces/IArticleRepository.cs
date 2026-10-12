using ArticlesRepository.Application.DTOs;
using ArticlesRepository.Domain.Entities;

namespace ArticlesRepository.Application.Interfaces;

public interface IArticleRepository
{
    Task<IReadOnlyList<Article>> GetAllAsync(ArticleQueryParameters query, CancellationToken ct = default);
    Task<Article?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<Article>> GetByAreaAsync(string area, CancellationToken ct = default);
    Task AddAsync(Article article, CancellationToken ct = default);
    void Update(Article article);
    void Remove(Article article);
    Task SaveChangesAsync(CancellationToken ct = default);
}
