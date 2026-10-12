using ArticlesRepository.Application.Interfaces;
using UglyToad.PdfPig;

namespace ArticlesRepository.Worker.Services;

public class PdfProcessor : IPdfProcessor
{
    private readonly IArticleRepository _repository;
    private readonly IS3StorageService _storage;
    private readonly ITranslator _translator;
    private readonly ICacheService _cache;
    private readonly ILogger<PdfProcessor> _logger;

    public PdfProcessor(
        IArticleRepository repository,
        IS3StorageService storage,
        ITranslator translator,
        ICacheService cache,
        ILogger<PdfProcessor> logger)
    {
        _repository = repository;
        _storage = storage;
        _translator = translator;
        _cache = cache;
        _logger = logger;
    }

    public async Task ProcessAsync(ArticleProcessEvent evt, CancellationToken ct)
    {
        var article = await _repository.GetByIdAsync(evt.ArticleId, ct);
        if (article is null)
        {
            _logger.LogWarning("Artigo {ArticleId} não existe mais; evento descartado", evt.ArticleId);
            return;
        }

        if (article.PdfS3Key != evt.PdfS3Key)
        {
            _logger.LogWarning("Evento obsoleto para o artigo {ArticleId} (PDF já foi substituído); descartado", evt.ArticleId);
            return;
        }

        try
        {
            article.StatusProcessamento = "processando";
            await SaveAsync(article, ct);

            await using var pdf = await _storage.DownloadAsync(evt.PdfS3Key, ct);
            var text = ExtractText(pdf);
            _logger.LogInformation("Texto extraído do PDF do artigo {ArticleId}: {Characters} caracteres", article.Id, text.Length);

            article.ResumoTraduzido = await _translator.TranslateToEnglishAsync(article.Resumo, ct);
            article.StatusProcessamento = "concluido";
            await SaveAsync(article, ct);

            _logger.LogInformation("Artigo {ArticleId} processado com sucesso", article.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Falha ao processar o artigo {ArticleId}", article.Id);
            await MarkAsErrorAsync(article);
            throw; // relança: a mensagem não é deletada e, após maxReceiveCount, vai para a DLQ
        }
        finally
        {
            await _cache.RemoveAsync($"article:{article.Id}", CancellationToken.None);
            await _cache.RemoveByPrefixAsync("articles:list:", CancellationToken.None);
        }
    }

    private async Task SaveAsync(Domain.Entities.Article article, CancellationToken ct)
    {
        _repository.Update(article);
        await _repository.SaveChangesAsync(ct);
    }

    private async Task MarkAsErrorAsync(Domain.Entities.Article article)
    {
        try
        {
            article.StatusProcessamento = "erro";
            await SaveAsync(article, CancellationToken.None);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Não foi possível marcar o artigo {ArticleId} com status 'erro'", article.Id);
        }
    }

    private static string ExtractText(Stream pdf)
    {
        using var document = PdfDocument.Open(pdf);
        return string.Join(Environment.NewLine, document.GetPages().Select(p => p.Text));
    }
}
