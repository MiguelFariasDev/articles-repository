import { useEffect, useMemo, useState } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { api } from '../api/client'
import Spinner from '../components/Spinner'
import StatusBadge from '../components/StatusBadge'
import { useToast } from '../toast'
import { AREAS, type Article } from '../types'

export default function Home() {
  const toast = useToast()
  const navigate = useNavigate()
  const [articles, setArticles] = useState<Article[]>([])
  const [loading, setLoading] = useState(true)
  const [area, setArea] = useState('')
  const [query, setQuery] = useState('')
  const [toDelete, setToDelete] = useState<Article | null>(null)

  useEffect(() => {
    let cancelled = false
    setLoading(true)
    api
      .list(area || undefined)
      .then((data) => !cancelled && setArticles(data))
      .catch((e: Error) => toast('danger', e.message))
      .finally(() => !cancelled && setLoading(false))
    return () => {
      cancelled = true
    }
  }, [area, toast])

  const filtered = useMemo(() => {
    const q = query.trim().toLowerCase()
    if (!q) return articles
    return articles.filter((a) =>
      [a.titulo, a.autores, a.palavrasChave].some((f) => f.toLowerCase().includes(q)),
    )
  }, [articles, query])

  async function download(a: Article) {
    try {
      window.open(await api.pdfUrl(a.id), '_blank', 'noopener')
    } catch (e) {
      toast('danger', (e as Error).message)
    }
  }

  async function confirmDelete() {
    if (!toDelete) return
    try {
      await api.remove(toDelete.id)
      setArticles((list) => list.filter((a) => a.id !== toDelete.id))
      toast('success', 'Artigo excluído com sucesso.')
    } catch (e) {
      toast('danger', (e as Error).message)
    } finally {
      setToDelete(null)
    }
  }

  return (
    <>
      <div className="d-flex flex-wrap justify-content-between align-items-center gap-2 mb-3">
        <h1 className="h3 mb-0">Artigos</h1>
        <button className="btn btn-primary" onClick={() => navigate('/articles/new')}>Novo Artigo</button>
      </div>

      <div className="row g-2 mb-3">
        <div className="col-12 col-md-4">
          <select className="form-select" aria-label="Filtrar por área" value={area} onChange={(e) => setArea(e.target.value)}>
            <option value="">Todas as áreas</option>
            {AREAS.map((a) => <option key={a}>{a}</option>)}
          </select>
        </div>
        <div className="col-12 col-md-8">
          <input
            className="form-control"
            type="search"
            placeholder="Buscar por título, autores ou palavras-chave"
            aria-label="Buscar"
            value={query}
            onChange={(e) => setQuery(e.target.value)}
          />
        </div>
      </div>

      {loading ? (
        <Spinner />
      ) : filtered.length === 0 ? (
        <div className="alert alert-info">Nenhum artigo encontrado.</div>
      ) : (
        <div className="table-responsive">
          <table className="table table-hover align-middle">
            <thead>
              <tr><th>Título</th><th>Autores</th><th>Área</th><th>Status</th><th>Criado em</th><th className="text-end">Ações</th></tr>
            </thead>
            <tbody>
              {filtered.map((a) => (
                <tr key={a.id}>
                  <td><Link to={`/articles/${a.id}`}>{a.titulo}</Link></td>
                  <td>{a.autores}</td>
                  <td>{a.area}</td>
                  <td><StatusBadge status={a.statusProcessamento} /></td>
                  <td>{new Date(a.criadoEm).toLocaleDateString('pt-BR')}</td>
                  <td className="text-end text-nowrap">
                    <Link className="btn btn-sm btn-outline-primary me-1" to={`/articles/${a.id}`}>Ver</Link>
                    <Link className="btn btn-sm btn-outline-secondary me-1" to={`/articles/${a.id}/edit`}>Editar</Link>
                    <button className="btn btn-sm btn-outline-success me-1" disabled={!a.pdfS3Key} onClick={() => download(a)}>PDF</button>
                    <button className="btn btn-sm btn-outline-danger" onClick={() => setToDelete(a)}>Excluir</button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}

      {toDelete && (
        <>
          <div className="modal d-block" tabIndex={-1} role="dialog" aria-modal="true">
            <div className="modal-dialog modal-dialog-centered">
              <div className="modal-content">
                <div className="modal-header"><h2 className="modal-title h5">Excluir artigo</h2></div>
                <div className="modal-body">Deseja realmente excluir “{toDelete.titulo}”? Esta ação não pode ser desfeita.</div>
                <div className="modal-footer">
                  <button className="btn btn-secondary" onClick={() => setToDelete(null)}>Cancelar</button>
                  <button className="btn btn-danger" onClick={confirmDelete}>Excluir</button>
                </div>
              </div>
            </div>
          </div>
          <div className="modal-backdrop show" />
        </>
      )}
    </>
  )
}
