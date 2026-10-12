namespace ArticlesRepository.Application.Interfaces;

/// <summary>Dados do cliente da requisição atual (implementado na Api).</summary>
public interface IClientContext
{
    string? IpAddress { get; }
}
