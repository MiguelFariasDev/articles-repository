import { useEffect, useState, type ReactNode } from 'react'
import { Link, useNavigate, useParams } from 'react-router-dom'
import { getById, getDownloadUrl } from '../api/articles'
import Loading from '../components/Loading'
import StatusBadge from '../components/StatusBadge'
import { useToast } from '../hooks/useToast'
import type { Article } from '../types/article'
import { formatDateTime } from '../utils/format'

const POLL_MS = 5000

function Card({ title, children }: { title: string; children: ReactNode }) {
  return (
    <section className="rounded border bg-white p-4">
      <h2 className="mb-2 text-sm font-semibold uppercase text-gray-500">{title}</h2>
      <div className="whitespace-pre-wrap">{children}</div>
    </section>
  )
}

export default function ArticleDetailsPage() {
  const { id } = useParams()
  const navigate = useNavigate()
  const { notify } = useToast()
  const [article, setArticle] = useState<Article | null>(null)
  const [loading, setLoading] = useState(true)

  const polling = article?.statusProcessamento === 'pendente' || article?.statusProcessamento === 'processando'

  useEffect(() => {
    if (!id) return
    getById(id)
      .then(setArticle)
      .catch((e: Error) => notify('error', e.message))
      .finally(() => setLoading(false))
  }, [id, notify])

  // polling enquanto o Worker processa; para em "concluido" ou "erro"
  useEffect(() => {
    if (!id || !polling) return
    const timer = setInterval(() => {
      getById(id).then(setArticle).catch(() => {})
    }, POLL_MS)
    return () => clearInterval(timer)
  }, [id, polling])

  async function download() {
    if (!id) return
    try {
      window.open(await getDownloadUrl(id), '_blank', 'noopener')
    } catch (e) {
      notify('error', (e as Error).message)
    }
  }

  if (loading) return <Loading />
  if (!article) {
    return <p className="text-gray-600">Artigo não encontrado. <Link to="/" className="text-blue-600 underline">Voltar</Link></p>
  }

  return (
    <div className="space-y-4">
      <div className="flex flex-wrap items-start justify-between gap-2">
        <h1 className="text-2xl font-bold">{article.titulo}</h1>
        <StatusBadge status={article.statusProcessamento} />
      </div>

      <div className="grid gap-4 sm:grid-cols-2">
        <Card title="Autores">{article.autores}</Card>
        <Card title="Área">{article.area}</Card>
        <Card title="Palavras-chave">{article.palavrasChave}</Card>
        <Card title="Datas">
          Criado em {formatDateTime(article.criadoEm)}
          {article.atualizadoEm && `\nAtualizado em ${formatDateTime(article.atualizadoEm)}`}
        </Card>
      </div>

      <Card title="Resumo">{article.resumo}</Card>

      {polling && (
        <p className="text-sm text-yellow-700">Processando o PDF… atualizando a cada 5s.</p>
      )}
      {article.statusProcessamento === 'concluido' && (
        <Card title="Resumo Traduzido">{article.resumoTraduzido ?? 'Tradução indisponível.'}</Card>
      )}

      <div className="flex flex-wrap gap-2">
        <button onClick={download} disabled={!article.pdfS3Key} className="rounded bg-green-600 px-4 py-2 text-white hover:bg-green-700 disabled:opacity-50">
          Baixar PDF
        </button>
        <Link to={`/articles/${article.id}/edit`} className="rounded border bg-white px-4 py-2 hover:bg-gray-100">Editar</Link>
        <button onClick={() => navigate('/')} className="rounded border bg-white px-4 py-2 hover:bg-gray-100">Voltar</button>
      </div>
    </div>
  )
}
