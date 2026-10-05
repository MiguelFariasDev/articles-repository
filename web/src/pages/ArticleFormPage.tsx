import { useEffect, useState } from 'react'
import { useNavigate, useParams } from 'react-router-dom'
import { create, getById, update } from '../api/articles'
import ArticleForm from '../components/ArticleForm'
import Loading from '../components/Loading'
import { useToast } from '../hooks/useToast'
import type { CreateArticleRequest } from '../types/article'

export default function ArticleFormPage() {
  const { id } = useParams()
  const isEdit = Boolean(id)
  const navigate = useNavigate()
  const { notify } = useToast()
  const [initial, setInitial] = useState<CreateArticleRequest | undefined>()
  const [loading, setLoading] = useState(isEdit)
  const [submitting, setSubmitting] = useState(false)

  useEffect(() => {
    if (!id) return
    getById(id)
      .then((a) =>
        setInitial({ titulo: a.titulo, autores: a.autores, resumo: a.resumo, palavrasChave: a.palavrasChave, area: a.area }),
      )
      .catch((e: Error) => notify('error', e.message))
      .finally(() => setLoading(false))
  }, [id, notify])

  async function handleSubmit(values: CreateArticleRequest) {
    setSubmitting(true)
    try {
      if (id) await update(id, values)
      else await create(values)
      notify('success', isEdit ? 'Artigo atualizado com sucesso.' : 'Artigo cadastrado com sucesso.')
      navigate('/')
    } catch (e) {
      notify('error', (e as Error).message)
    } finally {
      setSubmitting(false)
    }
  }

  if (loading) return <Loading />
  if (isEdit && !initial) return <p className="text-gray-600">Artigo não encontrado.</p>

  return (
    <>
      <h1 className="mb-4 text-2xl font-bold">{isEdit ? 'Editar artigo' : 'Novo artigo'}</h1>
      <ArticleForm initial={initial} submitting={submitting} onSubmit={handleSubmit} onCancel={() => navigate('/')} />
    </>
  )
}
