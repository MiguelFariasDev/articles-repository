export const AREAS = ['Computação', 'Engenharia', 'Saúde', 'Humanas', 'Exatas', 'Outras'] as const

export type StatusProcessamento = 'pendente' | 'processando' | 'concluido' | 'erro'

export interface Article {
  id: string
  titulo: string
  autores: string
  resumo: string
  palavrasChave: string
  area: string
  pdfS3Key: string | null
  resumoTraduzido: string | null
  statusProcessamento: StatusProcessamento
  criadoEm: string
  atualizadoEm: string | null
}

export interface CreateArticleRequest {
  titulo: string
  autores: string
  resumo: string
  palavrasChave: string
  area: string
}

export interface UpdateArticleRequest {
  titulo?: string
  autores?: string
  resumo?: string
  palavrasChave?: string
  area?: string
}

export interface ArticleQueryParameters {
  area?: string
  search?: string
  page?: number
  pageSize?: number
}
