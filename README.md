# 📚 Articles Repository

![C#](https://img.shields.io/badge/C%23-239120?style=for-the-badge&logo=csharp&logoColor=white)
![.NET](https://img.shields.io/badge/.NET_10-512BD4?style=for-the-badge&logo=dotnet&logoColor=white)
![PostgreSQL](https://img.shields.io/badge/PostgreSQL-4169E1?style=for-the-badge&logo=postgresql&logoColor=white)
![Redis](https://img.shields.io/badge/Redis-DC382D?style=for-the-badge&logo=redis&logoColor=white)
![AWS](https://img.shields.io/badge/AWS-232F3E?style=for-the-badge&logo=amazonwebservices&logoColor=white)
![DynamoDB](https://img.shields.io/badge/DynamoDB-4053D6?style=for-the-badge&logo=amazondynamodb&logoColor=white)
![Docker](https://img.shields.io/badge/Docker-2496ED?style=for-the-badge&logo=docker&logoColor=white)
![TypeScript](https://img.shields.io/badge/TypeScript-3178C6?style=for-the-badge&logo=typescript&logoColor=white)
![React](https://img.shields.io/badge/React-20232A?style=for-the-badge&logo=react&logoColor=61DAFB)
![Vite](https://img.shields.io/badge/Vite-646CFF?style=for-the-badge&logo=vite&logoColor=white)

Repositório de Artigos Científicos e TCCs, uma aplicação distribuída na AWS desenvolvida como trabalho acadêmico.

## 📖 Sobre

Cada artigo cadastrado tem título, autores, resumo, palavras-chave, área e um **PDF obrigatório**. A aplicação permite cadastrar, consultar, buscar por área, atualizar e remover artigos.

- Buscas por área são **cacheadas** no Redis (ElastiCache).
- Toda ação CRUD é registrada em um **log no DynamoDB**.
- O upload do PDF publica um evento no **SNS**; um **Worker** consome a fila **SQS**, extrai o texto do PDF e traduz o resumo para inglês, sem bloquear a resposta HTTP.

> ✅ **Status:** backend completo com S3, Redis, DynamoDB, SNS/SQS e Worker, validado localmente (LocalStack). Próxima etapa: ALB + Auto Scaling.

## 🏗️ Arquitetura

```
Cliente ──► ALB ──► EC2 (ASG 1..3) ── API ─┬─► RDS (PostgreSQL)
                                            ├─► S3 (PDFs)  ◄── URL pré-assinada (download)
                                            ├─► ElastiCache (Redis) ◄─ cache de leituras
                                            ├─► DynamoDB (log de ações CRUD)
                                            └─► SNS ──► SQS ──► Worker ─┬─► S3 (baixa PDF)
                                                          └─► DLQ       ├─► PdfPig + tradução do resumo
                                                                        └─► RDS (status) + invalida cache
```

Detalhes, decisões e fluxos em [`docs/ARCHITECTURE.md`](docs/ARCHITECTURE.md).

## 🔌 Endpoints

| Método | Rota | Descrição |
|---|---|---|
| GET | `/health` | Health checks (RDS, Redis, S3, DynamoDB) — usado pelo ALB |
| GET | `/api/articles` | Lista (`area`, `search`, `page`, `pageSize`) — cacheada |
| GET | `/api/articles/{id}` | Detalhe — cacheado |
| GET | `/api/articles/area/{area}` | Lista por área — cacheada |
| GET | `/api/articles/search?q=` | Busca textual |
| GET | `/api/articles/{id}/download` | `{ "url": "..." }` pré-assinada (15 min) |
| POST | `/api/articles` | Cria (`multipart/form-data`: campos + `pdf` ≤ 10MB) → 201 |
| PUT | `/api/articles/{id}` | Atualiza (multipart com `pdf` opcional, ou JSON só de metadados) → 204 |
| DELETE | `/api/articles/{id}` | Remove artigo e PDF → 204 |
| GET | `/api/articles/_load?iterations=` | CPU-intensivo (demo do Auto Scaling) |

Áreas permitidas: Computação, Engenharia, Saúde, Humanas, Exatas, Outras.

## ☁️ Serviços AWS

| Serviço | Uso |
|---|---|
| EC2 | Hospedagem da aplicação |
| S3 | Armazenamento dos PDFs |
| RDS (PostgreSQL) | Dados dos artigos |
| ElastiCache (Redis) | Cache das buscas por área |
| DynamoDB | Log de ações CRUD |
| SNS + SQS (+ DLQ) | Processamento assíncrono do PDF |
| ALB + Auto Scaling | Balanceamento e escala (min 1, desired 1, max 3; CPU 70% / 25%) |

## 🧰 Stack

.NET 10, ASP.NET Core Web API, EF Core + Npgsql, StackExchange.Redis, AWSSDK, PdfPig, Worker Service, Swagger, React + TypeScript + Vite, Docker + LocalStack.

## 🗂️ Estrutura

```
src/
  ArticlesRepository.Domain/          # entidade
  ArticlesRepository.Application/     # DTOs, interfaces, ArticleService
  ArticlesRepository.Infrastructure/  # EF Core, Aws/, Cache/, Repositories/
  ArticlesRepository.Api/             # controllers, health checks
  ArticlesRepository.Worker/          # consumidor SQS + PdfProcessor
frontend/
infra/       # scripts AWS CLI (Parte 1) + user-data da EC2
scripts/     # localstack-init.sh, run-local.sh
docs/        # ARCHITECTURE.md
```

## ▶️ Como rodar localmente

Pré-requisitos: .NET SDK 10, Node.js 22+, Docker.

```bash
./scripts/run-local.sh   # docker compose (Postgres, Redis, LocalStack) + migration + API + Worker
```

- Swagger: http://localhost:5019/swagger · Health: http://localhost:5019/health
- Portas ocupadas (5432, 6379, 4566)? Crie um `.env` com `DB_PORT`, `REDIS_PORT`, `LOCALSTACK_PORT` (ver `.env.example`); o script já usa esses valores.
- Sem `DEEPL_API_KEY` o Worker usa um tradutor mock (`[EN] ...`).
- Frontend: `cd frontend && npm install && npm run dev` (http://localhost:5173).

Exemplo de upload:

```bash
curl -X POST http://localhost:5019/api/articles \
  -F "titulo=Teste" -F "autores=Autor" -F "resumo=Meu resumo" \
  -F "palavrasChave=teste,aws" -F "area=Computação" -F "pdf=@artigo.pdf;type=application/pdf"
```

## ☁️ Infra na AWS

Scripts idempotentes em [`infra/`](infra/README.md): `01` S3 → `02` RDS → `03` ElastiCache → `04` DynamoDB → `05` SNS/SQS → `06` IAM → `07` EC2 (`REPO_URL=... ./07-create-ec2.sh`) e `99-cleanup.sh`.

## 🔐 Configuração

Nunca versione credenciais. Localmente o SDK aponta para o LocalStack (`AWS:ServiceUrl`); na EC2 as credenciais vêm da IAM role.
