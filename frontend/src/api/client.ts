import type { Article, ArticleInput } from '../types'
import { mockApi } from './mock'

const BASE = import.meta.env.VITE_API_URL ?? 'http://localhost:5019'
const USE_MOCK = import.meta.env.VITE_USE_MOCK === 'true'

async function request<T>(path: string, init?: RequestInit): Promise<T> {
  let res: Response
  try {
    res = await fetch(`${BASE}${path}`, init)
  } catch {
    throw new Error('Não foi possível conectar à API. Verifique se o backend está no ar.')
  }
  if (!res.ok) {
    const text = await res.text().catch(() => '')
    let message = text
    try {
      const body = JSON.parse(text) as { error?: string; detail?: string; title?: string }
      message = body.error ?? body.detail ?? body.title ?? text
    } catch {
      // resposta não-JSON: usa o texto cru
    }
    throw new Error(message || `Erro ${res.status} ao chamar a API`)
  }
  return res.status === 204 ? (undefined as T) : ((await res.json()) as T)
}

function toForm(input: ArticleInput, pdf?: File | null) {
  const form = new FormData()
  form.append('titulo', input.titulo)
  form.append('autores', input.autores)
  form.append('resumo', input.resumo)
  form.append('palavrasChave', input.palavrasChave)
  form.append('area', input.area)
  if (pdf) form.append('pdf', pdf)
  return form
}

const realApi = {
  list: (area?: string) =>
    request<Article[]>(`/api/articles${area ? `?area=${encodeURIComponent(area)}` : ''}`),
  get: (id: string) => request<Article>(`/api/articles/${id}`),
  // O PDF vai junto no multipart; a API grava no S3 e publica o evento SNS
  create: (input: ArticleInput, pdf: File) =>
    request<Article>('/api/articles', { method: 'POST', body: toForm(input, pdf) }),
  update: (id: string, input: ArticleInput, pdf?: File | null) =>
    request<void>(`/api/articles/${id}`, { method: 'PUT', body: toForm(input, pdf) }),
  remove: (id: string) => request<void>(`/api/articles/${id}`, { method: 'DELETE' }),
  // GET /download responde { url } (URL pré-assinada do S3)
  async pdfUrl(id: string) {
    const data = await request<{ url: string }>(`/api/articles/${id}/download`)
    return data.url
  },
}

export const api = USE_MOCK ? mockApi : realApi
