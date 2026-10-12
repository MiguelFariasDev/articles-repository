using ArticlesRepository.Application.DTOs;
using ArticlesRepository.Application.Interfaces;
using ArticlesRepository.Domain.Entities;
using ArticlesRepository.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ArticlesRepository.Infrastructure.Repositories;

public class ArticleRepository : IArticleRepository
{
    private readonly AppDbContext _db;

    public ArticleRepository(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<Article>> GetAllAsync(ArticleQueryParameters query, CancellationToken ct = default)
    {
        IQueryable<Article> articles = _db.Articles.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.Area))
        {
            var area = query.Area.Trim();
            articles = articles.Where(a => a.Area == area);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var pattern = $"%{query.Search.Trim()}%";
            articles = articles.Where(a =>
                EF.Functions.ILike(a.Titulo, pattern) ||
                EF.Functions.ILike(a.Autores, pattern) ||
                EF.Functions.ILike(a.PalavrasChave, pattern) ||
                EF.Functions.ILike(a.Resumo, pattern));
        }

        var page = Math.Max(query.Page, 1);
        var pageSize = Math.Clamp(query.PageSize, 1, 100);

        return await articles
            .OrderByDescending(a => a.CriadoEm)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);
    }

    public Task<Article?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        _db.Articles.FirstOrDefaultAsync(a => a.Id == id, ct);

    public async Task<IReadOnlyList<Article>> GetByAreaAsync(string area, CancellationToken ct = default) =>
        await _db.Articles.AsNoTracking()
            .Where(a => a.Area == area)
            .OrderByDescending(a => a.CriadoEm)
            .ToListAsync(ct);

    public async Task AddAsync(Article article, CancellationToken ct = default) =>
        await _db.Articles.AddAsync(article, ct);

    public void Update(Article article) => _db.Articles.Update(article);

    public void Remove(Article article) => _db.Articles.Remove(article);

    public Task SaveChangesAsync(CancellationToken ct = default) => _db.SaveChangesAsync(ct);
}
