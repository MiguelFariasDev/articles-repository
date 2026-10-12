import { useEffect, useState } from 'react'
import { Link, useNavigate, useParams } from 'react-router-dom'
import { api } from '../api/client'
import Spinner from '../components/Spinner'
import StatusBadge from '../components/StatusBadge'
import { useToast } from '../toast'
import type { Article } from '../types'

const POLL_MS = 5000

export default function ArticleDetails() {
  const { id } = useParams()
  const toast = useToast()
  const navigate = useNavigate()
  const [article, setArticle] = useState<Article | null>(null)
  const [loading, setLoading] = useState(true)

  const active = article?.statusProcessamento === 'pendente' || article?.statusProcessamento === 'processando'

  useEffect(() => {
    if (!id) return
    api
      .get(id)
      .then(setArticle)
      .catch((e: Error) => toast('danger', e.message))
      .finally(() => setLoading(false))
  }, [id, toast])

  // polling enquanto o Worker processa o PDF
  useEffect(() => {
    if (!id || !active) return
    const timer = setInterval(() => {
      api.get(id).then(setArticle).catch(() => {})
    }, POLL_MS)
    return () => clearInterval(timer)
  }, [id, active])

  async function download() {
    if (!id) return
    try {
      window.open(await api.pdfUrl(id), '_blank', 'noopener')
    } catch (e) {
      toast('danger', (e as Error).message)
    }
  }

  if (loading) return <Spinner />
  if (!article) {
    return (
      <div className="alert alert-warning">
        Artigo não encontrado. <Link to="/">Voltar</Link>
      </div>
    )
  }

  return (
    <article>
      <div className="d-flex flex-wrap justify-content-between align-items-start gap-2 mb-3">
        <h1 className="h3 mb-0">{article.titulo}</h1>
        <StatusBadge status={article.statusProcessamento} />
      </div>

      <dl className="row">
        <dt className="col-sm-3">Autores</dt><dd className="col-sm-9">{article.autores}</dd>
        <dt className="col-sm-3">Área</dt><dd className="col-sm-9">{article.area}</dd>
        <dt className="col-sm-3">Palavras-chave</dt><dd className="col-sm-9">{article.palavrasChave}</dd>
        <dt className="col-sm-3">Criado em</dt><dd className="col-sm-9">{new Date(article.criadoEm).toLocaleString('pt-BR')}</dd>
        {article.atualizadoEm && (
          <><dt className="col-sm-3">Atualizado em</dt><dd className="col-sm-9">{new Date(article.atualizadoEm).toLocaleString('pt-BR')}</dd></>
        )}
      </dl>

      <h2 className="h5">Resumo</h2>
      <p style={{ whiteSpace: 'pre-wrap' }}>{article.resumo}</p>

      <h2 className="h5">Resumo (English)</h2>
      {article.resumoTraduzido ? (
        <p style={{ whiteSpace: 'pre-wrap' }}>{article.resumoTraduzido}</p>
      ) : active ? (
        <p className="text-muted">
          <span className="spinner-border spinner-border-sm me-2" aria-hidden="true" />
          Tradução em processamento… atualizando a cada 5s.
        </p>
      ) : (
        <p className="text-muted">Tradução indisponível.</p>
      )}

      <div className="d-flex gap-2 mt-4">
        <button className="btn btn-success" disabled={!article.pdfS3Key} onClick={download}>Baixar PDF</button>
        <Link className="btn btn-outline-secondary" to={`/articles/${article.id}/edit`}>Editar</Link>
        <button className="btn btn-outline-primary" onClick={() => navigate('/')}>Voltar</button>
      </div>
    </article>
  )
}
