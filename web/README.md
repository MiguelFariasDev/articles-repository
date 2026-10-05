# Web — Articles Repository

React + TypeScript + Vite + TailwindCSS 3 + axios + react-router-dom.

## Rodar

```bash
cd web
npm install
cp .env.example .env
npm run dev      # http://localhost:5173
npm run build
```

## Mock x API real

Em `web/.env`:

| Variável | Padrão | Descrição |
|---|---|---|
| `VITE_API_BASE_URL` | `http://localhost:5000` | URL base da API |
| `VITE_USE_MOCK` | `true` | `true` = dados fake em memória (latência 300–800 ms); `false` = API real |

Reinicie o `npm run dev` após alterar o `.env`.

## Endpoints usados

`GET /api/articles` (com `area`/`search` como query), `GET /api/articles/{id}`, `GET /api/articles/area/{area}`,
`GET /api/articles/search?q=`, `POST /api/articles` (JSON), `PUT /api/articles/{id}`, `DELETE /api/articles/{id}`,
`GET /api/articles/{id}/download` (string ou `{ "url": "..." }`).

O upload de PDF aparece desabilitado no formulário ("Disponível em breve") até o backend aceitar multipart.

## CORS

Para rodar integrado ao backend, o `ArticlesRepository.Api` precisa aceitar `http://localhost:5173`
(policy `Frontend` no `Program.cs`).
