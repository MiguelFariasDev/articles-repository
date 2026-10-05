# Arquitetura

Repositório de Artigos Científicos/TCCs distribuído na AWS. A API recebe metadados + PDF, o Worker processa o PDF de forma assíncrona.

```
                        ┌──────────────────────────────── EC2 (ASG 1..3) ───────────────────────────────┐
Cliente ──► ALB ──────► │  API (ASP.NET Core :5000)                          Worker (BackgroundService) │
                        └─────┬───────┬───────┬────────┬────────┬─────────────────▲──────┬──────┬───────┘
                              │       │       │        │        │ publica          │      │      │
                              ▼       ▼       ▼        ▼        ▼                  │      │      ▼
                            RDS     S3     Redis   DynamoDB    SNS ──► SQS ────────┘      │   Redis (invalida cache)
                         (Postgres) (PDFs) (cache) (log CRUD)   tópico  fila (+DLQ)        ├──► S3 (baixa PDF)
                                                                                           └──► RDS (status + tradução)
```

## Camadas (Clean Architecture)

| Projeto | Papel |
|---|---|
| `Domain` | Entidade `Article` pura, sem dependências |
| `Application` | DTOs, interfaces (`IArticleService`, `IS3StorageService`, `ICacheService`, `IArticleLogRepository`, `IEventPublisher`) e `ArticleService` (orquestração) |
| `Infrastructure` | EF Core/Npgsql, S3, Redis, DynamoDB, SNS, `AwsClientFactory` |
| `Api` | Controllers, health checks, exception handler global |
| `Worker` | Consumidor SQS + `PdfProcessor` |

## Decisões de design

- **SNS + SQS (fan-out + fila):** o upload não pode esperar a extração/tradução. O SNS desacopla quem publica de quem consome (novos consumidores = novas subscriptions) e a SQS dá buffer, retry e **DLQ** (após 3 recebimentos falhos). O Worker escala/falha independentemente da API.
- **DynamoDB para o log de ações:** escrita append-only, sem joins, alto volume e schema flexível (`DadosManipulados` em JSON). Mantém o RDS livre de tabelas de auditoria e o **TTL de 90 dias** expira os registros sem custo.
- **Cache (Redis/ElastiCache):** leituras (por id e listagens/área) dominam o uso; TTLs curtos (5 min por artigo, 60 s por listagem) mais invalidação em toda escrita mantêm consistência aceitável. Cache é **fail-safe**: erro do Redis vira warning e a requisição cai no RDS.
- **S3 para PDFs, chave no RDS:** binários grandes fora do banco; download por **URL pré-assinada** (15 min), sem trafegar o arquivo pela API.
- **Falhas:** S3 é fail-fast (o upload é essencial); cache e DynamoDB são fail-safe; falha ao publicar no SNS é logada (o artigo permanece `pendente`); falha no Worker relança a exceção → mensagem volta e, ao estourar `maxReceiveCount`, vai para a DLQ.
- **Configuração:** `IOptions<AwsOptions>`; `AWS:ServiceUrl` preenchido ⇒ LocalStack (credenciais dummy, path-style S3). Em produção, credenciais vêm da IAM role.

## Fluxos

### Upload (POST /api/articles, multipart)
```
Cliente ─► API: valida PDF (application/pdf, %PDF, ≤10MB)
API ─► S3: PutObject articles/{guid}-{arquivo}.pdf        (falhou? → erro, nada é gravado)
API ─► RDS: INSERT article (status=pendente, pdf_s3_key)  (falhou? → remove PDF do S3)
API ─► DynamoDB: log CREATE                                (best-effort)
API ─► Redis: invalida article:{id} e articles:list:*      (best-effort)
API ─► SNS: article.created {articleId, pdfS3Key, occurredAt}
API ─► Cliente: 201 Created (não espera o Worker)
```

### Processamento assíncrono
```
SNS ─► SQS ─► Worker (long poll 20s, visibility 300s)
Worker: status=processando → baixa PDF do S3 → extrai texto (PdfPig)
        → traduz o resumo para EN (DeepL se DEEPL_API_KEY, senão mock "[EN] ...")
        → resumo_traduzido + status=concluido → invalida cache → DeleteMessage
Erro:   status=erro → relança → mensagem não é deletada → (3x) → DLQ
```

### Leitura com cache
```
GET → Redis? ── hit ─► resposta
            └─ miss ─► RDS ─► grava no Redis (TTL) ─► resposta
```
Chaves: `article:{id}` (5 min) e `articles:list:area=..:search=..:page=..:size=..` (60 s).

### Log de ações
Todo CREATE/READ(by id)/UPDATE(diff)/DELETE grava `ArticleLogEntry` (`Id`, `Timestamp`, `Acao`, `ArticleId`, `DadosManipulados`, `UsuarioIp`, `Ttl`). Falha ao logar apenas gera warning.

## Observabilidade e operação

- Logs estruturados em JSON (`AddJsonConsole`) na API e no Worker.
- `GET /health` (usado pelo ALB): RDS, Redis, S3 e DynamoDB.
- `GET /api/articles/_load?iterations=N`: SHA256 em loop para gerar CPU na demo do Auto Scaling (CPU 70% escala / 25% reduz).
