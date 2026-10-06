using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Security.Cryptography;
using ArticlesRepository.Application.DTOs;
using ArticlesRepository.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ArticlesRepository.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ArticlesController : ControllerBase
{
    private const long MaxPdfBytes = 10 * 1024 * 1024;
    private const long MaxRequestBytes = MaxPdfBytes + 1024 * 1024; // margem para os campos do formulário

    private readonly IArticleService _service;

    public ArticlesController(IArticleService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ArticleResponse>>> GetAll(
        [FromQuery] ArticleQueryParameters query, CancellationToken ct)
    {
        try
        {
            return Ok(await _service.GetAllAsync(query, ct));
        }
        catch (ValidationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ArticleResponse>> GetById(Guid id, CancellationToken ct)
    {
        var article = await _service.GetByIdAsync(id, ct);
        return article is null ? NotFound() : Ok(article);
    }

    [HttpGet("{id:guid}/download")]
    public async Task<IActionResult> Download(Guid id, CancellationToken ct)
    {
        var url = await _service.GetDownloadUrlAsync(id, ct);
        return url is null ? NotFound() : Ok(new { url });
    }

    [HttpGet("area/{area}")]
    public async Task<ActionResult<IReadOnlyList<ArticleResponse>>> GetByArea(string area, CancellationToken ct)
    {
        try
        {
            return Ok(await _service.GetByAreaAsync(area, ct));
        }
        catch (ValidationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet("search")]
    public async Task<ActionResult<IReadOnlyList<ArticleResponse>>> Search(
        [FromQuery] string q, CancellationToken ct, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        return Ok(await _service.GetAllAsync(new ArticleQueryParameters(Search: q, Page: page, PageSize: pageSize), ct));
    }

    [HttpPost]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(MaxRequestBytes)]
    [ProducesResponseType(typeof(ArticleResponse), StatusCodes.Status201Created)]
    public async Task<IActionResult> Create([FromForm] CreateArticleRequest request, IFormFile? pdf, CancellationToken ct)
    {
        var error = await ValidatePdfAsync(pdf, required: true, ct);
        if (error is not null)
        {
            return error;
        }

        try
        {
            await using var stream = pdf!.OpenReadStream();
            var created = await _service.CreateAsync(request, stream, pdf.FileName, "application/pdf", ct);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }
        catch (ValidationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>Atualiza metadados e, opcionalmente, substitui o PDF (multipart/form-data).</summary>
    [HttpPut("{id:guid}")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(MaxRequestBytes)]
    public async Task<IActionResult> Update(Guid id, [FromForm] UpdateArticleRequest request, IFormFile? pdf, CancellationToken ct)
    {
        var error = await ValidatePdfAsync(pdf, required: false, ct);
        if (error is not null)
        {
            return error;
        }

        try
        {
            if (pdf is null)
            {
                return await UpdateResult(id, request, null, ct);
            }

            await using var stream = pdf.OpenReadStream();
            return await UpdateResult(id, request, (stream, pdf.FileName), ct);
        }
        catch (ValidationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>Atualização somente dos metadados via JSON (mantida por compatibilidade).</summary>
    [HttpPut("{id:guid}")]
    [Consumes("application/json")]
    [ApiExplorerSettings(IgnoreApi = true)]
    public async Task<IActionResult> UpdateJson(Guid id, [FromBody] UpdateArticleRequest request, CancellationToken ct)
    {
        try
        {
            return await UpdateResult(id, request, null, ct);
        }
        catch (ValidationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct) =>
        await _service.DeleteAsync(id, ct) ? NoContent() : NotFound();

    /// <summary>Endpoint CPU-intensivo usado na demonstração do Auto Scaling.</summary>
    [AllowAnonymous]
    [HttpGet("_load")]
    public IActionResult Load([FromQuery] int iterations = 1_000_000)
    {
        iterations = Math.Clamp(iterations, 1, 50_000_000);

        var stopwatch = Stopwatch.StartNew();
        var buffer = new byte[32];
        for (var i = 0; i < iterations; i++)
        {
            buffer = SHA256.HashData(buffer);
        }

        stopwatch.Stop();
        return Ok(new { elapsedMs = stopwatch.ElapsedMilliseconds, iterations });
    }

    private async Task<IActionResult> UpdateResult(
        Guid id, UpdateArticleRequest request, (Stream Stream, string FileName)? pdf, CancellationToken ct)
    {
        var updated = await _service.UpdateAsync(id, request, pdf?.Stream, pdf?.FileName, "application/pdf", ct);
        return updated is null ? NotFound() : NoContent();
    }

    private async Task<IActionResult?> ValidatePdfAsync(IFormFile? pdf, bool required, CancellationToken ct)
    {
        if (pdf is null || pdf.Length == 0)
        {
            return required ? BadRequest(new { error = "O arquivo PDF ('pdf') é obrigatório." }) : null;
        }

        if (pdf.Length > MaxPdfBytes)
        {
            return StatusCode(StatusCodes.Status413PayloadTooLarge, new { error = "O PDF excede o limite de 10MB." });
        }

        if (!string.Equals(pdf.ContentType, "application/pdf", StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(new { error = "O arquivo deve ser do tipo application/pdf." });
        }

        // Confere a assinatura "%PDF" para não aceitar qualquer arquivo com Content-Type forjado.
        var header = new byte[4];
        await using var stream = pdf.OpenReadStream();
        var read = await stream.ReadAtLeastAsync(header, header.Length, throwOnEndOfStream: false, ct);
        if (read < 4 || header[0] != '%' || header[1] != 'P' || header[2] != 'D' || header[3] != 'F')
        {
            return BadRequest(new { error = "O arquivo enviado não é um PDF válido." });
        }

        return null;
    }
}
