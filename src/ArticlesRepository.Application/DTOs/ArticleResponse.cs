namespace ArticlesRepository.Application.DTOs;

public record ArticleResponse(
    Guid Id,
    string Titulo,
    string Autores,
    string Resumo,
    string PalavrasChave,
    string Area,
    string? PdfS3Key,
    string? ResumoTraduzido,
    string StatusProcessamento,
    DateTime CriadoEm,
    DateTime? AtualizadoEm);
