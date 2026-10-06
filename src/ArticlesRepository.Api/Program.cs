using ArticlesRepository.Api.Health;
using ArticlesRepository.Api.Infrastructure;
using ArticlesRepository.Application.Interfaces;
using ArticlesRepository.Application.Services;
using ArticlesRepository.Infrastructure.Aws;
using ArticlesRepository.Infrastructure.Cache;
using ArticlesRepository.Infrastructure.Data;
using ArticlesRepository.Infrastructure.Repositories;
using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddJsonConsole();

var connectionString = builder.Configuration.GetConnectionString("Rds")
    ?? throw new InvalidOperationException("ConnectionStrings:Rds não configurada.");
var redisConnection = builder.Configuration["Redis:Connection"] ?? "localhost:6379";

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<AppDbContext>(options => options.UseNpgsql(connectionString));
builder.Services.AddAwsServices(builder.Configuration);
builder.Services.AddRedisCache(builder.Configuration);

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IClientContext, HttpClientContext>();
builder.Services.AddScoped<IArticleRepository, ArticleRepository>();
builder.Services.AddScoped<IS3StorageService, S3StorageService>();
builder.Services.AddScoped<IArticleLogRepository, DynamoLogRepository>();
builder.Services.AddScoped<IEventPublisher, SnsEventPublisher>();
builder.Services.AddScoped<IArticleService, ArticleService>();

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

builder.Services.AddHealthChecks()
    .AddNpgSql(connectionString, name: "rds")
    .AddRedis(redisConnection, name: "redis")
    .AddCheck<S3HealthCheck>("s3")
    .AddCheck<DynamoHealthCheck>("dynamodb");

builder.Services.AddCors(o => o.AddPolicy("Frontend", p => p
    .WithOrigins("http://localhost:5173", "http://localhost:3000")
    .AllowAnyHeader()
    .AllowAnyMethod()));

var app = builder.Build();

// Em produção (EC2) as migrations são aplicadas no boot; o lock do EF evita corrida entre instâncias do ASG.
if (app.Configuration.GetValue<bool>("Database:MigrateOnStartup"))
{
    using var scope = app.Services.CreateScope();
    await scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.MigrateAsync();
}

app.UseExceptionHandler();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseCors("Frontend");

app.UseAuthorization();

app.MapControllers();

// Usado pelo ALB — não remover.
app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});

app.Run();
