namespace ArticlesRepository.Domain.Entities;

public class Article
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Titulo { get; set; } = string.Empty;
    public string Autores { get; set; } = string.Empty;
    public string Resumo { get; set; } = string.Empty;
    public string PalavrasChave { get; set; } = string.Empty;
    public string Area { get; set; } = string.Empty;
    public string? PdfS3Key { get; set; }
    public string? ResumoTraduzido { get; set; }
    public string StatusProcessamento { get; set; } = "pendente";
    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
    public DateTime? AtualizadoEm { get; set; }
}
