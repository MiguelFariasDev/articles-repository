# CLAUDE.md — Articles Repository

Contexto permanente do projeto. Atualize sempre que uma decisão mudar.

## Visão geral
Repositório de Artigos Científicos/TCCs. Trabalho acadêmico de faculdade que exige uma aplicação distribuída na AWS.
- Cada artigo tem: título, autores, resumo, palavras-chave, área e um PDF obrigatório.
- Buscas por área usam cache (ElastiCache/Redis).
- Toda ação CRUD é logada no DynamoDB.
- Upload do PDF publica evento no SNS; o Worker consome a SQS, extrai o texto do PDF e traduz o resumo para inglês.

Idioma do código: inglês. Docs/commits/UI: português (pt-BR).

## Serviços AWS obrigatórios
1. **EC2** — hospedagem da aplicação
2. **S3** — armazenamento dos PDFs
3. **RDS** — PostgreSQL
4. **ElastiCache** — Redis (cache)
5. **DynamoDB** — log de ações CRUD
6. **SNS + SQS** — processamento assíncrono

Parte 2: **Application Load Balancer (ALB)** + **Auto Scaling Group** (min=1, desired=1, max=3; CPU 70% escala / 25% reduz).

## Stack
- **.NET 10** (`net10.0`), C# — **não é .NET 8**
- ASP.NET Core Web API + Swagger (Swashbuckle)
- EF Core 10 + Npgsql (RDS PostgreSQL)
- StackExchange.Redis, AWSSDK (S3, SNS, SQS, DynamoDBv2)
- Worker Service (`BackgroundService`) como consumidor SQS
- Frontend: React 19 + TypeScript + Vite (`frontend/`)
- Docker Compose + LocalStack no ambiente local

## Arquitetura
```
Cliente ──► ALB ──► EC2 (ASG 1..3) ── API ─┬─► RDS (PostgreSQL)
                                            ├─► S3 (PDFs)
                                            ├─► ElastiCache (Redis)
                                            ├─► DynamoDB (logs CRUD)
                                            └─► SNS ──► SQS ──► Worker
                                                                 │ extrai texto do PDF (S3)
                                                                 └ traduz resumo → EN (RDS)
```

## Estrutura de pastas
```
ArticlesRepository.sln
Directory.Build.props
docker-compose.yml   .env.example
src/
  ArticlesRepository.Domain/          Entities, ValueObjects, Interfaces
  ArticlesRepository.Application/     Services, DTOs, Interfaces
  ArticlesRepository.Infrastructure/  Data, Aws, Cache
  ArticlesRepository.Api/             Controllers (Program.cs)
  ArticlesRepository.Worker/          Workers, Services
frontend/                             SPA React
infra/                                scripts AWS CLI
scripts/                              scripts auxiliares
```
Referências: Api→Application; Worker→Application; Application→Domain; Infrastructure→Application+Domain.

## Convenções de código
- PascalCase para classes/métodos, `_camelCase` para campos privados.
- `async/await` em toda I/O; nunca `.Result` / `.Wait()`.
- Injeção de dependência para todos os serviços AWS.
- Nullable habilitado.
- Logging com `ILogger<T>` (nunca `Console.WriteLine`).
- Interface para todo serviço de infraestrutura.

## Tratamento de erros
- Falha no cache (Redis) **nunca** derruba a requisição (cair para o banco).
- Falha no log DynamoDB **não** impede o CRUD.
- Falha no SNS/SQS em produção deve ir para **DLQ**.

## Nunca
- Nunca commitar credenciais (use `.env`, user-secrets, placeholders).
- Nunca usar `new AmazonS3Client()` diretamente — injetar via DI.
- Nunca bloquear a resposta HTTP esperando o Worker.
- Nunca remover o `/health` (usado pelo ALB).
- Nunca implementar fora do escopo pedido no prompt da etapa.

## Endpoints planejados
- `GET /health`
- `GET /api/articles` · `GET /api/articles/{id}` · `GET /api/articles?area=...` (cacheado)
- `POST /api/articles` · `PUT /api/articles/{id}` · `DELETE /api/articles/{id}`
- `POST /api/articles/{id}/pdf` (upload → S3 + evento SNS) · `GET /api/articles/{id}/pdf`

## Comandos úteis
```bash
dotnet build
dotnet run --project src/ArticlesRepository.Api        # http://localhost:5019
dotnet run --project src/ArticlesRepository.Worker
dotnet ef migrations add <Nome> -p src/ArticlesRepository.Infrastructure -s src/ArticlesRepository.Api
dotnet ef database update -p src/ArticlesRepository.Infrastructure -s src/ArticlesRepository.Api
docker compose up -d
cd frontend && npm install && npm run dev               # http://localhost:5173
```

## Modelo de Dados
Entidade `Article` (Domain, pura, sem atributos EF) → tabela `articles` (mapeamento por Fluent API em `Infrastructure/Data/AppDbContext.cs`, colunas em snake_case via `HasColumnName`).

| Propriedade | Coluna | Tipo / regra |
|---|---|---|
| Id | id | uuid, PK |
| Titulo | titulo | varchar(300), obrigatório |
| Autores | autores | varchar(500), obrigatório |
| Resumo | resumo | text, obrigatório |
| PalavrasChave | palavras_chave | varchar(300), obrigatório |
| Area | area | varchar(100), obrigatório, indexado |
| PdfS3Key | pdf_s3_key | varchar(500), null até o upload |
| ResumoTraduzido | resumo_traduzido | text, preenchido pelo Worker |
| StatusProcessamento | status_processamento | varchar(30), `pendente`\|`processando`\|`concluido`\|`erro` (default `pendente`) |
| CriadoEm | criado_em | timestamptz |
| AtualizadoEm | atualizado_em | timestamptz, null até o 1º update (preenchido no `SaveChangesAsync`) |

Migrations em `src/ArticlesRepository.Infrastructure/Data/Migrations`. O `AppDbContextFactory` (design-time) lê `DB_CONNECTION` ou usa o Postgres local.

## Integração AWS
- **Config**: `AwsOptions` (seção `AWS`), `AwsClientFactory` (único lugar com `new Amazon*Client`), `AddAwsServices` (S3/SNS/SQS/DynamoDB singletons + `IDynamoDBContext` scoped). `AWS:ServiceUrl` preenchido ⇒ LocalStack (credenciais dummy, `ForcePathStyle`); vazio ⇒ AWS real via IAM role.
- **S3** (`S3StorageService`): key `articles/{guid}-{arquivo}`; upload fail-fast; download por URL pré-assinada (15 min).
- **Redis** (`RedisCacheService`, `AddRedisCache`): fail-safe; `article:{id}` 5 min, `articles:list:...` 60 s; invalidação por prefixo (SCAN).
- **DynamoDB** (`DynamoLogRepository`): log CREATE/READ/UPDATE/DELETE, TTL 90 dias, nunca derruba a operação.
- **SNS** (`SnsEventPublisher`): `article.created` / `article.updated` (atributo `eventType`).
- **Worker** (`PdfProcessingWorker` + `PdfProcessor`): consome SQS (long poll), extrai texto (PdfPig), traduz resumo (DeepL com `DEEPL_API_KEY`, senão mock), atualiza status; erro ⇒ não deleta a mensagem ⇒ DLQ após 3 tentativas.

### Fluxos
- **Create**: valida PDF → S3 → RDS (se falhar, remove o PDF) → log Dynamo → invalida cache → SNS `article.created` → 201.
- **Update**: novo PDF (opcional) → S3, apaga o antigo (best-effort), status volta a `pendente` (também se o resumo mudar) → log com diff → invalida cache → SNS `article.updated`.
- **Delete**: RDS → apaga PDF (best-effort) → log → invalida cache.
- Falha ao publicar no SNS é logada e não desfaz o artigo (fica `pendente`).

### Variáveis de ambiente / config
`ConnectionStrings__Rds`, `Redis__Connection`, `AWS__Region`, `AWS__ServiceUrl`, `AWS__S3Bucket`, `AWS__SnsTopicArn`, `AWS__SqsQueueUrl`, `AWS__DynamoDbTable`, `DEEPL_API_KEY` (opcional), `Database__MigrateOnStartup` (true na EC2), `DB_CONNECTION` (só `dotnet ef`). Portas do host no `.env`: `DB_PORT`, `REDIS_PORT`, `LOCALSTACK_PORT`.

### Rodar local com LocalStack
`./scripts/run-local.sh` (sobe Docker, espera healthchecks, migration, API e Worker; logs em `.run/`). O `scripts/localstack-init.sh` cria bucket, tópico, fila+DLQ, subscription e tabela. A imagem é `localstack/localstack:3` (a `latest` exige token). Se 5432/4566 estiverem ocupadas, ajuste `DB_PORT`/`LOCALSTACK_PORT` no `.env` (o script deriva `ConnectionStrings__Rds`, `AWS__ServiceUrl` e `AWS__SqsQueueUrl`). URLs: Swagger `http://localhost:5019/swagger`, health `http://localhost:5019/health`. Em `curl`, percent-encode acentos na query (`area=Computa%C3%A7%C3%A3o`).

### Infra AWS
Scripts em `infra/` (ver `infra/README.md`), detalhes de design em `docs/ARCHITECTURE.md`. Os scripts de infra só foram validados com `bash -n`.

## Estado atual
Etapas 1–3B: estrutura, domínio, migration, CRUD.
Etapa 4: integração AWS completa (S3, Redis, DynamoDB, SNS/SQS, Worker), health checks, `/api/articles/_load`, exception handler global, LocalStack, scripts de infra e user-data. Validada localmente (fluxo completo). 
Etapa 5: ALB + Auto Scaling em `infra/08-create-alb.sh` e `infra/09-create-asg.sh` (sg-alb, target group `/health`, launch template, ASG 1..3, alarmes CPU 70%/25%; `sg-ec2:5000` só do ALB). Só validada com `bash -n`. Próximo: validar na AWS real / docs finais.
Decisões: Worker usa `Microsoft.NET.Sdk.Worker`; provider EF `Npgsql.EntityFrameworkCore.PostgreSQL`; `PUT` aceita multipart (documentado no Swagger) e JSON (compat.).
