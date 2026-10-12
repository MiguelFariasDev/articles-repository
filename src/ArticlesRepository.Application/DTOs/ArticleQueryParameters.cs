using System.ComponentModel.DataAnnotations;

namespace ArticlesRepository.Application.DTOs;

public record ArticleQueryParameters(
    string? Area = null,
    string? Search = null,
    [Range(1, int.MaxValue)] int Page = 1,
    [Range(1, 100)] int PageSize = 20);
