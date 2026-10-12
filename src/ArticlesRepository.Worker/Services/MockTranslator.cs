namespace ArticlesRepository.Worker.Services;

/// <summary>Tradutor usado localmente quando DEEPL_API_KEY não está configurada.</summary>
public class MockTranslator : ITranslator
{
    public Task<string> TranslateToEnglishAsync(string text, CancellationToken ct) =>
        Task.FromResult("[EN] " + text);
}
