# Frontend — Articles Repository

React 19 + TypeScript + Vite + Bootstrap 5 + React Router.

## Rodar

```bash
cd frontend
npm install
cp .env.example .env.development.local   # ajuste conforme necessário
npm run dev                               # http://localhost:5173
```

## Variáveis (`.env`)

| Variável | Padrão | Descrição |
|---|---|---|
| `VITE_API_URL` | `http://localhost:5019` | URL base da API |
| `VITE_USE_MOCK` | `false` | `true` usa dados fake (`src/api/mock.ts`) com latência de 500 ms |

## Endpoints consumidos

`GET /api/articles`, `GET /api/articles?area=`, `GET /api/articles/{id}`, `POST /api/articles` (JSON),
`PUT /api/articles/{id}`, `DELETE /api/articles/{id}`, `POST /api/articles/{id}/pdf` (multipart, campo `file`),
`GET /api/articles/{id}/pdf` (espera `{ "url": "..." }` presigned ou redirect ao PDF).
A busca textual é feita no cliente (título, autores, palavras-chave).

## CORS no backend

O `ArticlesRepository.Api` precisa aceitar `http://localhost:5173`:

```csharp
builder.Services.AddCors(o => o.AddPolicy("Frontend", p => p
    .WithOrigins("http://localhost:5173").AllowAnyHeader().AllowAnyMethod()));
app.UseCors("Frontend");
```
