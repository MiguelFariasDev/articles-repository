using ArticlesRepository.Application.Interfaces;
using ArticlesRepository.Infrastructure.Aws;
using ArticlesRepository.Infrastructure.Cache;
using ArticlesRepository.Infrastructure.Data;
using ArticlesRepository.Infrastructure.Repositories;
using ArticlesRepository.Worker.Services;
using ArticlesRepository.Worker.Workers;
using Microsoft.EntityFrameworkCore;

var builder = Host.CreateApplicationBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddJsonConsole();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Rds")));
builder.Services.AddAwsServices(builder.Configuration);
builder.Services.AddRedisCache(builder.Configuration);

builder.Services.AddScoped<IArticleRepository, ArticleRepository>();
builder.Services.AddScoped<IS3StorageService, S3StorageService>();
builder.Services.AddScoped<IPdfProcessor, PdfProcessor>();

// Com DEEPL_API_KEY usa o DeepL; sem chave (ex.: local) usa o tradutor mock.
if (!string.IsNullOrWhiteSpace(builder.Configuration["DEEPL_API_KEY"]))
{
    builder.Services.AddHttpClient<ITranslator, DeepLTranslator>();
}
else
{
    builder.Services.AddSingleton<ITranslator, MockTranslator>();
}

builder.Services.AddHostedService<PdfProcessingWorker>();

var host = builder.Build();
host.Run();
