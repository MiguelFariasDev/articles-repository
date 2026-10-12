using System.ComponentModel.DataAnnotations;

namespace ArticlesRepository.Application.DTOs;

public record UpdateArticleRequest(
    [MaxLength(300)] string? Titulo = null,
    [MaxLength(500)] string? Autores = null,
    string? Resumo = null,
    [MaxLength(300)] string? PalavrasChave = null,
    [MaxLength(100)] string? Area = null);
