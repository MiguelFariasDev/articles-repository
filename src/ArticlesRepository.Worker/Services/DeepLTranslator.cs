using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;

namespace ArticlesRepository.Worker.Services;

public class DeepLTranslator : ITranslator
{
    private readonly HttpClient _http;

    public DeepLTranslator(HttpClient http, IConfiguration configuration)
    {
        var apiKey = configuration["DEEPL_API_KEY"] ?? throw new InvalidOperationException("DEEPL_API_KEY não configurada.");

        // Chaves do plano gratuito terminam com ":fx" e usam outro host.
        _http = http;
        _http.BaseAddress = new Uri(apiKey.EndsWith(":fx", StringComparison.Ordinal)
            ? "https://api-free.deepl.com/"
            : "https://api.deepl.com/");
        _http.DefaultRequestHeaders.Authorization = new("DeepL-Auth-Key", apiKey);
    }

    public async Task<string> TranslateToEnglishAsync(string text, CancellationToken ct)
    {
        using var response = await _http.PostAsJsonAsync("v2/translate", new { text = new[] { text }, target_lang = "EN-US" }, ct);
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadFromJsonAsync<DeepLResponse>(ct);
        return body?.Translations.FirstOrDefault()?.Text
            ?? throw new InvalidOperationException("Resposta do DeepL sem tradução.");
    }

    private record DeepLResponse([property: JsonPropertyName("translations")] List<DeepLTranslation> Translations);

    private record DeepLTranslation([property: JsonPropertyName("text")] string Text);
}
