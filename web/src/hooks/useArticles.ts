import { useCallback, useEffect, useState } from 'react'
import { getAll } from '../api/articles'
import type { Article, ArticleQueryParameters } from '../types/article'

export function useArticles(params: ArticleQueryParameters) {
  const { area, search } = params
  const [articles, setArticles] = useState<Article[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [reloadKey, setReloadKey] = useState(0)

  useEffect(() => {
    let cancelled = false
    // debounce da busca textual
    const timer = setTimeout(() => {
      setLoading(true)
      getAll({ area: area || undefined, search: search?.trim() || undefined })
        .then((data) => {
          if (cancelled) return
          setArticles(data)
          setError(null)
        })
        .catch((e: Error) => !cancelled && setError(e.message))
        .finally(() => !cancelled && setLoading(false))
    }, 250)
    return () => {
      cancelled = true
      clearTimeout(timer)
    }
  }, [area, search, reloadKey])

  const reload = useCallback(() => setReloadKey((k) => k + 1), [])

  return { articles, setArticles, loading, error, reload }
}
