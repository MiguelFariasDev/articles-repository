import { useEffect, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { remove } from '../api/articles'
import ArticleTable from '../components/ArticleTable'
import Loading from '../components/Loading'
import { useArticles } from '../hooks/useArticles'
import { useToast } from '../hooks/useToast'
import { AREAS, type Article } from '../types/article'

const PAGE_SIZE = 5

export default function HomePage() {
  const navigate = useNavigate()
  const { notify } = useToast()
  const [area, setArea] = useState('')
  const [search, setSearch] = useState('')
  const [page, setPage] = useState(1)
  const { articles, setArticles, loading, error } = useArticles({ area, search })

  useEffect(() => {
    if (error) notify('error', error)
  }, [error, notify])

  const pages = Math.max(1, Math.ceil(articles.length / PAGE_SIZE))
  const current = Math.min(page, pages)
  const visible = articles.slice((current - 1) * PAGE_SIZE, current * PAGE_SIZE)

  async function handleDelete(article: Article) {
    if (!window.confirm(`Excluir "${article.titulo}"?`)) return
    try {
      await remove(article.id)
      setArticles((list) => list.filter((a) => a.id !== article.id))
      notify('success', 'Artigo excluído com sucesso.')
    } catch (e) {
      notify('error', (e as Error).message)
    }
  }

  return (
    <>
      <h1 className="mb-4 text-2xl font-bold">Repositório de Artigos</h1>

      <div className="mb-4 flex flex-col gap-2 sm:flex-row">
        <select
          aria-label="Filtrar por área"
          value={area}
          onChange={(e) => { setArea(e.target.value); setPage(1) }}
          className="rounded border border-gray-300 bg-white px-3 py-2"
        >
          <option value="">Todas as áreas</option>
          {AREAS.map((a) => <option key={a}>{a}</option>)}
        </select>
        <input
          type="search"
          aria-label="Buscar"
          placeholder="Buscar por título, autores ou palavras-chave"
          value={search}
          onChange={(e) => { setSearch(e.target.value); setPage(1) }}
          className="flex-1 rounded border border-gray-300 px-3 py-2"
        />
        <button onClick={() => navigate('/articles/new')} className="rounded bg-blue-600 px-4 py-2 text-white hover:bg-blue-700">
          Novo Artigo
        </button>
      </div>

      {loading ? (
        <Loading />
      ) : (
        <>
          <ArticleTable
            articles={visible}
            onView={(a) => navigate(`/articles/${a.id}`)}
            onEdit={(a) => navigate(`/articles/${a.id}/edit`)}
            onDelete={handleDelete}
          />
          {pages > 1 && (
            <div className="mt-4 flex items-center justify-center gap-3 text-sm">
              <button disabled={current === 1} onClick={() => setPage(current - 1)} className="rounded border bg-white px-3 py-1 disabled:opacity-50">Anterior</button>
              <span>Página {current} de {pages}</span>
              <button disabled={current === pages} onClick={() => setPage(current + 1)} className="rounded border bg-white px-3 py-1 disabled:opacity-50">Próxima</button>
            </div>
          )}
        </>
      )}
    </>
  )
}
