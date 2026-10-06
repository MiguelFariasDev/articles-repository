using ArticlesRepository.Application.Interfaces;

namespace ArticlesRepository.Api.Infrastructure;

public class HttpClientContext : IClientContext
{
    private readonly IHttpContextAccessor _accessor;

    public HttpClientContext(IHttpContextAccessor accessor)
    {
        _accessor = accessor;
    }

    public string? IpAddress => _accessor.HttpContext?.Connection.RemoteIpAddress?.ToString();
}
