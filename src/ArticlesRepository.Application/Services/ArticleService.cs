using System.ComponentModel.DataAnnotations;
using ArticlesRepository.Application.DTOs;
using ArticlesRepository.Application.Interfaces;
using ArticlesRepository.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace ArticlesRepository.Application.Services;

public class ArticleService : IArticleService
{
    public static readonly IReadOnlyList<string> AllowedAreas =
        ["Computação", "Engenharia", "Saúde", "Humanas", "Exatas", "Outras"];

    private const string ListPrefix = "articles:list:";
    private static readonly TimeSpan ArticleTtl = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan ListTtl = TimeSpan.FromSeconds(60);
    private static readonly TimeSpan DownloadUrlTtl = TimeSpan.FromMinutes(15);

    private readonly IArticleRepository _repository;
    private readonly IS3StorageService _storage;
    private readonly ICacheService _cache;
    private readonly IArticleLogRepository _actionLog;
    private readonly IEventPublisher _events;
    private readonly IClientContext _client;
    private readonly ILogger<ArticleService> _logger;

    public ArticleService(
        IArticleRepository repository,
        IS3StorageService storage,
        ICacheService cache,
        IArticleLogRepository actionLog,
        IEventPublisher events,
        IClientContext client,
        ILogger<ArticleService> logger)
    {
        _repository = repository;
        _storage = storage;
        _cache = cache;
        _actionLog = actionLog;
        _events = events;
        _client = client;
        _logger = logger;
    }

    public async Task<IReadOnlyList<ArticleResponse>> GetAllAsync(ArticleQueryParameters q, CancellationToken ct = default)
    {
        string? area = null;
        if (!string.IsNullOrWhiteSpace(q.Area))
        {
            area = ValidateArea(q.Area);
        }

        var key = $"{ListPrefix}area={area}:search={q.Search?.Trim()}:page={q.Page}:size={q.PageSize}";
        var cached = await _cache.GetAsync<List<ArticleResponse>>(key, ct);
        if (cached is not null)
        {
            _logger.LogInformation("Cache HIT {CacheKey}", key);
            return cached;
        }

        _logger.LogInformation("Cache MISS {CacheKey} (consultando RDS)", key);
        var articles = await _repository.GetAllAsync(q with { Area = area }, ct);
        var result = articles.Select(ToResponse).ToList();
        await _cache.SetAsync(key, result, ListTtl, ct);
        return result;
    }

    public async Task<ArticleResponse?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var key = ArticleKey(id);
        var cached = await _cache.GetAsync<ArticleResponse>(key, ct);
        if (cached is not null)
        {
            _logger.LogInformation("Cache HIT {CacheKey}", key);
            await _actionLog.LogAsync("READ", new { id }, id, _client.IpAddress, ct);
            return cached;
        }

        _logger.LogInformation("Cache MISS {CacheKey} (consultando RDS)", key);
        var article = await _repository.GetByIdAsync(id, ct);
        if (article is null)
        {
            return null;
        }

        var response = ToResponse(article);
        await _cache.SetAsync(key, response, ArticleTtl, ct);
        await _actionLog.LogAsync("READ", new { id }, id, _client.IpAddress, ct);
        return response;
    }

    public async Task<IReadOnlyList<ArticleResponse>> GetByAreaAsync(string area, CancellationToken ct = default)
    {
        var normalized = ValidateArea(area);

        var key = $"{ListPrefix}area={normalized}:search=:page=1:size=all";
        var cached = await _cache.GetAsync<List<ArticleResponse>>(key, ct);
        if (cached is not null)
        {
            _logger.LogInformation("Cache HIT {CacheKey}", key);
            return cached;
        }

        _logger.LogInformation("Cache MISS {CacheKey} (consultando RDS)", key);
        var articles = await _repository.GetByAreaAsync(normalized, ct);
        var result = articles.Select(ToResponse).ToList();
        await _cache.SetAsync(key, result, ListTtl, ct);
        return result;
    }

    public async Task<ArticleResponse> CreateAsync(
        CreateArticleRequest req, Stream pdf, string fileName, string contentType, CancellationToken ct = default)
    {
        var area = ValidateArea(req.Area);

        // Fail-fast: o upload é essencial, exceções do S3 propagam.
        var pdfKey = await _storage.UploadAsync(pdf, fileName, contentType, ct);

        var article = new Article
        {
            Titulo = req.Titulo.Trim(),
            Autores = req.Autores.Trim(),
            Resumo = req.Resumo.Trim(),
            PalavrasChave = req.PalavrasChave.Trim(),
            Area = area,
            PdfS3Key = pdfKey,
            StatusProcessamento = "pendente"
        };

        try
        {
            await _repository.AddAsync(article, ct);
            await _repository.SaveChangesAsync(ct);
        }
        catch
        {
            await TryDeletePdfAsync(pdfKey);
            throw;
        }

        await _actionLog.LogAsync("CREATE", new { req.Titulo, req.Autores, req.Resumo, req.PalavrasChave, Area = area, PdfS3Key = pdfKey },
            article.Id, _client.IpAddress, ct);
        await InvalidateCacheAsync(article.Id, ct);
        await TryPublishAsync(() => _events.PublishArticleCreatedAsync(article.Id, pdfKey, ct), article.Id);

        _logger.LogInformation("Article {ArticleId} created", article.Id);
        return ToResponse(article);
    }

    public async Task<ArticleResponse?> UpdateAsync(
        Guid id, UpdateArticleRequest req, Stream? pdf = null, string? fileName = null, string? contentType = null,
        CancellationToken ct = default)
    {
        var article = await _repository.GetByIdAsync(id, ct);
        if (article is null)
        {
            return null;
        }

        var diff = new Dictionary<string, object?>();
        string? oldPdfKey = null;
        string? newPdfKey = null;

        if (req.Area is not null)
        {
            var area = ValidateArea(req.Area);
            Track(diff, "area", article.Area, area);
            article.Area = area;
        }

        Apply(diff, "titulo", req.Titulo, article.Titulo, v => article.Titulo = v);
        Apply(diff, "autores", req.Autores, article.Autores, v => article.Autores = v);
        Apply(diff, "resumo", req.Resumo, article.Resumo, v => article.Resumo = v);
        Apply(diff, "palavrasChave", req.PalavrasChave, article.PalavrasChave, v => article.PalavrasChave = v);

        if (pdf is not null)
        {
            newPdfKey = await _storage.UploadAsync(pdf, fileName ?? "article.pdf", contentType ?? "application/pdf", ct);
            oldPdfKey = article.PdfS3Key;
            Track(diff, "pdfS3Key", oldPdfKey, newPdfKey);
            article.PdfS3Key = newPdfKey;
        }

        var reprocess = newPdfKey is not null || (diff.ContainsKey("resumo") && article.PdfS3Key is not null);
        if (reprocess)
        {
            article.StatusProcessamento = "pendente";
            article.ResumoTraduzido = null;
        }

        try
        {
            _repository.Update(article);
            await _repository.SaveChangesAsync(ct);
        }
        catch
        {
            if (newPdfKey is not null)
            {
                await TryDeletePdfAsync(newPdfKey);
            }

            throw;
        }

        if (oldPdfKey is not null)
        {
            await TryDeletePdfAsync(oldPdfKey);
        }

        await _actionLog.LogAsync("UPDATE", diff, article.Id, _client.IpAddress, ct);
        await InvalidateCacheAsync(article.Id, ct);

        if (reprocess && article.PdfS3Key is not null)
        {
            await TryPublishAsync(() => _events.PublishArticleUpdatedAsync(article.Id, article.PdfS3Key, ct), article.Id);
        }

        _logger.LogInformation("Article {ArticleId} updated", article.Id);
        return ToResponse(article);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var article = await _repository.GetByIdAsync(id, ct);
        if (article is null)
        {
            return false;
        }

        var pdfKey = article.PdfS3Key;
        var titulo = article.Titulo;

        _repository.Remove(article);
        await _repository.SaveChangesAsync(ct);

        if (pdfKey is not null)
        {
            await TryDeletePdfAsync(pdfKey);
        }

        await _actionLog.LogAsync("DELETE", new { id, titulo }, id, _client.IpAddress, ct);
        await InvalidateCacheAsync(id, ct);

        _logger.LogInformation("Article {ArticleId} deleted", id);
        return true;
    }

    public async Task<string?> GetDownloadUrlAsync(Guid id, CancellationToken ct = default)
    {
        var article = await _repository.GetByIdAsync(id, ct);
        if (article?.PdfS3Key is null)
        {
            return null;
        }

        return await _storage.GetPresignedUrlAsync(article.PdfS3Key, DownloadUrlTtl, ct);
    }

    private static string ArticleKey(Guid id) => $"article:{id}";

    private async Task InvalidateCacheAsync(Guid id, CancellationToken ct)
    {
        await _cache.RemoveAsync(ArticleKey(id), ct);
        await _cache.RemoveByPrefixAsync(ListPrefix, ct);
    }

    /// <summary>Best-effort: falha ao remover do S3 não deve derrubar a operação principal.</summary>
    private async Task TryDeletePdfAsync(string key)
    {
        try
        {
            await _storage.DeleteAsync(key, CancellationToken.None);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Falha ao remover o PDF {PdfS3Key} do S3 (best-effort)", key);
        }
    }

    /// <summary>O artigo já foi salvo; falha ao publicar evento é logada e não desfaz a operação.</summary>
    private async Task TryPublishAsync(Func<Task> publish, Guid articleId)
    {
        try
        {
            await publish();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Falha ao publicar evento do artigo {ArticleId} no SNS", articleId);
        }
    }

    private static string ValidateArea(string area)
    {
        var match = AllowedAreas.FirstOrDefault(a => string.Equals(a, area.Trim(), StringComparison.OrdinalIgnoreCase));
        return match ?? throw new ValidationException(
            $"Área inválida: '{area}'. Áreas permitidas: {string.Join(", ", AllowedAreas)}.");
    }

    private static void Apply(Dictionary<string, object?> diff, string field, string? value, string current, Action<string> set)
    {
        if (value is null)
        {
            return;
        }

        var trimmed = value.Trim();
        if (trimmed.Length == 0)
        {
            throw new ValidationException("Campos informados não podem ser vazios.");
        }

        Track(diff, field, current, trimmed);
        set(trimmed);
    }

    private static void Track(Dictionary<string, object?> diff, string field, string? oldValue, string newValue)
    {
        if (!string.Equals(oldValue, newValue, StringComparison.Ordinal))
        {
            diff[field] = new { de = oldValue, para = newValue };
        }
    }

    private static ArticleResponse ToResponse(Article a) => new(
        a.Id, a.Titulo, a.Autores, a.Resumo, a.PalavrasChave, a.Area,
        a.PdfS3Key, a.ResumoTraduzido, a.StatusProcessamento, a.CriadoEm, a.AtualizadoEm);
}
