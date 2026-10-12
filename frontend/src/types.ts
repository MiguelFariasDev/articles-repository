export const AREAS = ['Computação', 'Engenharia', 'Saúde', 'Humanas', 'Exatas', 'Outras'] as const

export type ProcessingStatus = 'pendente' | 'processando' | 'concluido' | 'erro'

export interface Article {
  id: string
  titulo: string
  autores: string
  resumo: string
  palavrasChave: string
  area: string
  pdfS3Key: string | null
  resumoTraduzido: string | null
  statusProcessamento: ProcessingStatus
  criadoEm: string
  atualizadoEm: string | null
}

export type ArticleInput = Pick<Article, 'titulo' | 'autores' | 'resumo' | 'palavrasChave' | 'area'>
