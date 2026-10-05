import { useState, type ChangeEvent, type FormEvent } from 'react'
import { AREAS, type CreateArticleRequest } from '../types/article'

interface Props {
  initial?: CreateArticleRequest
  submitting: boolean
  onSubmit: (values: CreateArticleRequest) => void
  onCancel: () => void
}

const EMPTY: CreateArticleRequest = { titulo: '', autores: '', resumo: '', palavrasChave: '', area: '' }
const LABELS: Record<keyof CreateArticleRequest, string> = {
  titulo: 'Título', autores: 'Autores', resumo: 'Resumo', palavrasChave: 'Palavras-chave', area: 'Área',
}
const MAX: Partial<Record<keyof CreateArticleRequest, number>> = { titulo: 300, autores: 500, palavrasChave: 300 }

type Errors = Partial<Record<keyof CreateArticleRequest, string>>

export default function ArticleForm({ initial = EMPTY, submitting, onSubmit, onCancel }: Props) {
  const [values, setValues] = useState(initial)
  const [errors, setErrors] = useState<Errors>({})

  const onChange = (e: ChangeEvent<HTMLInputElement | HTMLTextAreaElement | HTMLSelectElement>) =>
    setValues((v) => ({ ...v, [e.target.name]: e.target.value }))

  function handleSubmit(e: FormEvent) {
    e.preventDefault()
    const found: Errors = {}
    for (const key of Object.keys(LABELS) as (keyof CreateArticleRequest)[]) {
      const value = values[key].trim()
      if (!value) found[key] = `${LABELS[key]} é obrigatório.`
      else if (MAX[key] && value.length > MAX[key]!) found[key] = `Máximo de ${MAX[key]} caracteres.`
    }
    setErrors(found)
    if (Object.keys(found).length) return
    onSubmit({
      titulo: values.titulo.trim(), autores: values.autores.trim(), resumo: values.resumo.trim(),
      palavrasChave: values.palavrasChave.trim(), area: values.area,
    })
  }

  const input = (name: keyof CreateArticleRequest) =>
    `w-full rounded border px-3 py-2 ${errors[name] ? 'border-red-500' : 'border-gray-300'}`
  const error = (name: keyof CreateArticleRequest) =>
    errors[name] && <p className="mt-1 text-sm text-red-600">{errors[name]}</p>

  return (
    <form onSubmit={handleSubmit} noValidate className="max-w-3xl space-y-4 rounded border bg-white p-6">
      <div>
        <label htmlFor="titulo" className="mb-1 block text-sm font-medium">Título</label>
        <input id="titulo" name="titulo" value={values.titulo} onChange={onChange} className={input('titulo')} />
        {error('titulo')}
      </div>
      <div>
        <label htmlFor="autores" className="mb-1 block text-sm font-medium">Autores</label>
        <input id="autores" name="autores" value={values.autores} onChange={onChange} className={input('autores')} />
        {error('autores')}
      </div>
      <div>
        <label htmlFor="resumo" className="mb-1 block text-sm font-medium">Resumo</label>
        <textarea id="resumo" name="resumo" rows={6} value={values.resumo} onChange={onChange} className={input('resumo')} />
        {error('resumo')}
      </div>
      <div>
        <label htmlFor="palavrasChave" className="mb-1 block text-sm font-medium">Palavras-chave</label>
        <input id="palavrasChave" name="palavrasChave" value={values.palavrasChave} onChange={onChange} className={input('palavrasChave')} />
        {error('palavrasChave')}
      </div>
      <div>
        <label htmlFor="area" className="mb-1 block text-sm font-medium">Área</label>
        <select id="area" name="area" value={values.area} onChange={onChange} className={input('area')}>
          <option value="">Selecione…</option>
          {AREAS.map((a) => <option key={a}>{a}</option>)}
        </select>
        {error('area')}
      </div>
      <div title="Disponível em breve">
        <label htmlFor="pdf" className="mb-1 block text-sm font-medium text-gray-500">PDF</label>
        <input id="pdf" type="file" disabled className="w-full cursor-not-allowed rounded border border-gray-200 bg-gray-100 px-3 py-2 text-gray-400" />
        <p className="mt-1 text-xs text-gray-500">Disponível em breve.</p>
      </div>
      <div className="flex gap-2">
        <button type="submit" disabled={submitting} className="rounded bg-blue-600 px-4 py-2 text-white hover:bg-blue-700 disabled:opacity-60">
          {submitting ? 'Salvando…' : 'Salvar'}
        </button>
        <button type="button" onClick={onCancel} disabled={submitting} className="rounded border px-4 py-2 hover:bg-gray-100">
          Cancelar
        </button>
      </div>
    </form>
  )
}
