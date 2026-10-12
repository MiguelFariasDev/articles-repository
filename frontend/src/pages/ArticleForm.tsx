import { useEffect, useState, type ChangeEvent, type FormEvent } from 'react'
import { useNavigate, useParams } from 'react-router-dom'
import { api } from '../api/client'
import Spinner from '../components/Spinner'
import { useToast } from '../toast'
import { AREAS, type ArticleInput } from '../types'

const MAX_PDF_BYTES = 10 * 1024 * 1024
const EMPTY: ArticleInput = { titulo: '', autores: '', resumo: '', palavrasChave: '', area: '' }
const LIMITS: Partial<Record<keyof ArticleInput, number>> = { titulo: 300, autores: 500, palavrasChave: 300 }
const LABELS: Record<keyof ArticleInput, string> = {
  titulo: 'Título', autores: 'Autores', resumo: 'Resumo', palavrasChave: 'Palavras-chave', area: 'Área',
}

type Errors = Partial<Record<keyof ArticleInput | 'pdf', string>>

function validatePdf(file: File | null): string | undefined {
  if (!file) return undefined
  if (file.type !== 'application/pdf' && !file.name.toLowerCase().endsWith('.pdf')) return 'Apenas arquivos PDF são aceitos.'
  if (file.size > MAX_PDF_BYTES) return 'O PDF deve ter no máximo 10 MB.'
  return undefined
}

export default function ArticleForm() {
  const { id } = useParams()
  const isEdit = Boolean(id)
  const toast = useToast()
  const navigate = useNavigate()
  const [form, setForm] = useState<ArticleInput>(EMPTY)
  const [pdf, setPdf] = useState<File | null>(null)
  const [errors, setErrors] = useState<Errors>({})
  const [loading, setLoading] = useState(isEdit)
  const [saving, setSaving] = useState(false)

  useEffect(() => {
    if (!id) return
    api
      .get(id)
      .then((a) => setForm({ titulo: a.titulo, autores: a.autores, resumo: a.resumo, palavrasChave: a.palavrasChave, area: a.area }))
      .catch((e: Error) => toast('danger', e.message))
      .finally(() => setLoading(false))
  }, [id, toast])

  function validate(): Errors {
    const e: Errors = {}
    for (const key of Object.keys(LABELS) as (keyof ArticleInput)[]) {
      const value = form[key].trim()
      if (!value) e[key] = `${LABELS[key]} é obrigatório.`
      else if (LIMITS[key] && value.length > LIMITS[key]!) e[key] = `${LABELS[key]} deve ter no máximo ${LIMITS[key]} caracteres.`
    }
    if (!isEdit && !pdf) e.pdf = 'O PDF é obrigatório.'
    else if (validatePdf(pdf)) e.pdf = validatePdf(pdf)
    return e
  }

  function onField(e: ChangeEvent<HTMLInputElement | HTMLTextAreaElement | HTMLSelectElement>) {
    setForm((f) => ({ ...f, [e.target.name]: e.target.value }))
  }

  function onFile(e: ChangeEvent<HTMLInputElement>) {
    const file = e.target.files?.[0] ?? null
    const error = validatePdf(file)
    if (error) e.target.value = ''
    setPdf(error ? null : file)
    setErrors((prev) => ({ ...prev, pdf: error }))
  }

  async function onSubmit(e: FormEvent) {
    e.preventDefault()
    const found = validate()
    setErrors(found)
    if (Object.keys(found).length) return
    setSaving(true)
    try {
      const input: ArticleInput = {
        titulo: form.titulo.trim(), autores: form.autores.trim(), resumo: form.resumo.trim(),
        palavrasChave: form.palavrasChave.trim(), area: form.area,
      }
      if (isEdit) await api.update(id!, input, pdf)
      else await api.create(input, pdf!)
      toast('success', isEdit ? 'Artigo atualizado com sucesso.' : 'Artigo cadastrado com sucesso.')
      navigate('/')
    } catch (err) {
      toast('danger', (err as Error).message)
    } finally {
      setSaving(false)
    }
  }

  if (loading) return <Spinner />

  const field = (name: keyof ArticleInput) => ({
    id: name, name, value: form[name], onChange: onField,
    className: `form-control${errors[name] ? ' is-invalid' : ''}`,
  })

  return (
    <form onSubmit={onSubmit} noValidate style={{ maxWidth: 760 }}>
      <h1 className="h3 mb-4">{isEdit ? 'Editar artigo' : 'Novo artigo'}</h1>

      <div className="mb-3">
        <label htmlFor="titulo" className="form-label">Título</label>
        <input {...field('titulo')} />
        <div className="invalid-feedback">{errors.titulo}</div>
      </div>
      <div className="mb-3">
        <label htmlFor="autores" className="form-label">Autores</label>
        <input {...field('autores')} />
        <div className="invalid-feedback">{errors.autores}</div>
      </div>
      <div className="mb-3">
        <label htmlFor="resumo" className="form-label">Resumo</label>
        <textarea rows={6} {...field('resumo')} />
        <div className="invalid-feedback">{errors.resumo}</div>
      </div>
      <div className="mb-3">
        <label htmlFor="palavrasChave" className="form-label">Palavras-chave</label>
        <input {...field('palavrasChave')} />
        <div className="invalid-feedback">{errors.palavrasChave}</div>
      </div>
      <div className="mb-3">
        <label htmlFor="area" className="form-label">Área</label>
        <select {...field('area')} className={`form-select${errors.area ? ' is-invalid' : ''}`}>
          <option value="">Selecione…</option>
          {AREAS.map((a) => <option key={a}>{a}</option>)}
        </select>
        <div className="invalid-feedback">{errors.area}</div>
      </div>
      <div className="mb-4">
        <label htmlFor="pdf" className="form-label">PDF {isEdit ? '(opcional — substitui o atual)' : ''}</label>
        <input id="pdf" type="file" accept=".pdf,application/pdf" onChange={onFile} className={`form-control${errors.pdf ? ' is-invalid' : ''}`} />
        <div className="invalid-feedback">{errors.pdf}</div>
        <div className="form-text">Somente PDF, até 10 MB.</div>
      </div>

      <div className="d-flex gap-2">
        <button type="submit" className="btn btn-primary" disabled={saving}>
          {saving && <span className="spinner-border spinner-border-sm me-2" aria-hidden="true" />}
          Salvar
        </button>
        <button type="button" className="btn btn-outline-secondary" onClick={() => navigate('/')} disabled={saving}>Cancelar</button>
      </div>
    </form>
  )
}
