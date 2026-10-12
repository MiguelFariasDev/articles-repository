import type { Article, ArticleInput } from '../types'

const delay = () => new Promise((r) => setTimeout(r, 500))

const seed = (i: number, a: Partial<Article>): Article => ({
  id: crypto.randomUUID(),
  pdfS3Key: `articles/${i}.pdf`,
  resumoTraduzido: null,
  statusProcessamento: 'concluido',
  criadoEm: new Date(Date.now() - i * 86_400_000).toISOString(),
  atualizadoEm: null,
  titulo: '', autores: '', resumo: '', palavrasChave: '', area: 'Outras',
  ...a,
})

let db: Article[] = [
  seed(1, {
    titulo: 'Arquitetura de microsserviços em nuvem AWS',
    autores: 'Ana Souza, Carlos Lima',
    resumo: 'Análise comparativa de padrões de microsserviços implantados em EC2 com balanceamento por ALB.',
    resumoTraduzido: 'Comparative analysis of microservice patterns deployed on EC2 behind an ALB.',
    palavrasChave: 'aws, microsserviços, ec2', area: 'Computação',
  }),
  seed(2, {
    titulo: 'Impacto do exercício físico na saúde mental de universitários',
    autores: 'Beatriz Alves', resumo: 'Estudo transversal com 300 estudantes sobre atividade física e ansiedade.',
    palavrasChave: 'saúde mental, exercício', area: 'Saúde', statusProcessamento: 'processando',
  }),
  seed(3, {
    titulo: 'Otimização estrutural de pontes em concreto protendido',
    autores: 'Diego Ramos, Fernanda Costa', resumo: 'Modelo de otimização aplicado a vigas de pontes rodoviárias.',
    palavrasChave: 'estruturas, concreto', area: 'Engenharia', statusProcessamento: 'pendente',
  }),
  seed(4, {
    titulo: 'Letramento digital em comunidades ribeirinhas',
    autores: 'Gabriela Nunes', resumo: 'Pesquisa qualitativa sobre acesso e uso de tecnologia.',
    palavrasChave: 'educação, inclusão digital', area: 'Humanas', statusProcessamento: 'erro',
  }),
]

export const mockApi = {
  async list(area?: string) {
    await delay()
    return area ? db.filter((a) => a.area === area) : [...db]
  },
  async get(id: string) {
    await delay()
    const a = db.find((x) => x.id === id)
    if (!a) throw new Error('Artigo não encontrado')
    return { ...a }
  },
  async create(input: ArticleInput, pdf: File) {
    await delay()
    console.info('[mock] create', input, pdf.name)
    const a = seed(0, { ...input, statusProcessamento: 'pendente', criadoEm: new Date().toISOString() })
    db = [a, ...db]
    // simula o Worker avançando o status
    setTimeout(() => (a.statusProcessamento = 'processando'), 6000)
    setTimeout(() => {
      a.statusProcessamento = 'concluido'
      a.resumoTraduzido = `[EN] ${a.resumo}`
    }, 12000)
    return a
  },
  async update(id: string, input: ArticleInput, pdf?: File | null) {
    await delay()
    console.info('[mock] update', id, input, pdf?.name)
    db = db.map((a) => (a.id === id ? { ...a, ...input, atualizadoEm: new Date().toISOString() } : a))
  },
  async remove(id: string) {
    await delay()
    console.info('[mock] delete', id)
    db = db.filter((a) => a.id !== id)
  },
  async pdfUrl(id: string) {
    await delay()
    console.info('[mock] pdf', id)
    return 'data:application/pdf;base64,JVBERi0xLjQKMSAwIG9iajw8L1R5cGUvQ2F0YWxvZy9QYWdlcyAyIDAgUj4+ZW5kb2JqCjIgMCBvYmo8PC9UeXBlL1BhZ2VzL0tpZHNbMyAwIFJdL0NvdW50IDE+PmVuZG9iagozIDAgb2JqPDwvVHlwZS9QYWdlL1BhcmVudCAyIDAgUi9NZWRpYUJveFswIDAgMjAwIDIwMF0+PmVuZG9iagp0cmFpbGVyPDwvUm9vdCAxIDAgUj4+CiUlRU9G'
  },
}
