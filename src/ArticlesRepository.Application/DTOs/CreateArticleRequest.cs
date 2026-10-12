using System.ComponentModel.DataAnnotations;

namespace ArticlesRepository.Application.DTOs;

public record CreateArticleRequest(
    [Required, MaxLength(300)] string Titulo,
    [Required, MaxLength(500)] string Autores,
    [Required] string Resumo,
    [Required, MaxLength(300)] string PalavrasChave,
    [Required, MaxLength(100)] string Area);
