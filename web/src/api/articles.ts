import type {
  Article,
  ArticleQueryParameters,
  CreateArticleRequest,
  UpdateArticleRequest,
} from '../types/article'
import { apiClient } from './client'
import { mockArticles } from './mock'

const USE_MOCK = import.meta.env.VITE_USE_MOCK === 'true'

// Estado em memória do modo mock
let mockDb: Article[] = [...mockArticles]

const latency = () => new Promise<void>((r) => setTimeout(r, 300 + Math.random() * 500))

const matches = (a: Article, q: string) =>
  [a.titulo, a.autores, a.palavrasChave].some((f) => f.toLowerCase().includes(q.trim().toLowerCase()))

const findMock = (id: string) => {
  const article = mockDb.find((a) => a.id === id)
  if (!article) throw new Error('Artigo não encontrado.')
  return article
}

export async function getAll(params?: ArticleQueryParameters): Promise<Article[]> {
  if (USE_MOCK) {
    await latency()
    return mockDb.filter((a) => (!params?.area || a.area === params.area) && (!params?.search || matches(a, params.search)))
  }
  return (await apiClient.get<Article[]>('/api/articles', { params })).data
}

export async function getById(id: string): Promise<Article> {
  if (USE_MOCK) {
    await latency()
    return { ...findMock(id) }
  }
  return (await apiClient.get<Article>(`/api/articles/${id}`)).data
}

export async function getByArea(area: string): Promise<Article[]> {
  if (USE_MOCK) {
    await latency()
    return mockDb.filter((a) => a.area === area)
  }
  return (await apiClient.get<Article[]>(`/api/articles/area/${encodeURIComponent(area)}`)).data
}

export async function search(q: string): Promise<Article[]> {
  if (USE_MOCK) {
    await latency()
    return mockDb.filter((a) => matches(a, q))
  }
  return (await apiClient.get<Article[]>('/api/articles/search', { params: { q } })).data
}

export async function create(req: CreateArticleRequest): Promise<Article> {
  if (USE_MOCK) {
    await latency()
    console.info('[mock] create', req)
    const article: Article = {
      ...req,
      id: crypto.randomUUID(),
      pdfS3Key: null,
      resumoTraduzido: null,
      statusProcessamento: 'pendente',
      criadoEm: new Date().toISOString(),
      atualizadoEm: null,
    }
    mockDb = [article, ...mockDb]
    return article
  }
  return (await apiClient.post<Article>('/api/articles', req)).data
}

export async function update(id: string, req: UpdateArticleRequest): Promise<Article> {
  if (USE_MOCK) {
    await latency()
    console.info('[mock] update', id, req)
    const updated = { ...findMock(id), ...req, atualizadoEm: new Date().toISOString() }
    mockDb = mockDb.map((a) => (a.id === id ? updated : a))
    return updated
  }
  return (await apiClient.put<Article>(`/api/articles/${id}`, req)).data
}

export async function remove(id: string): Promise<void> {
  if (USE_MOCK) {
    await latency()
    console.info('[mock] delete', id)
    mockDb = mockDb.filter((a) => a.id !== id)
    return
  }
  await apiClient.delete(`/api/articles/${id}`)
}

// A API pode devolver a URL como string pura ou como { url }
export async function getDownloadUrl(id: string): Promise<string> {
  if (USE_MOCK) {
    await latency()
    console.info('[mock] download', id)
    return 'https://www.w3.org/WAI/ER/tests/xhtml/testfiles/resources/pdf/dummy.pdf'
  }
  const { data } = await apiClient.get<string | { url: string }>(`/api/articles/${id}/download`)
  return typeof data === 'string' ? data : data.url
}
