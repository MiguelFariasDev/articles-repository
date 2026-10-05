import type { Article } from '../types/article'
import { formatDate, truncate } from '../utils/format'
import StatusBadge from './StatusBadge'

interface Props {
  articles: Article[]
  onView: (article: Article) => void
  onEdit: (article: Article) => void
  onDelete: (article: Article) => void
}

export default function ArticleTable({ articles, onView, onEdit, onDelete }: Props) {
  if (articles.length === 0) {
    return (
      <div className="rounded border border-dashed bg-white py-12 text-center text-gray-500">
        Nenhum artigo encontrado.
      </div>
    )
  }

  return (
    <div className="overflow-x-auto rounded border bg-white">
      <table className="min-w-full divide-y divide-gray-200 text-sm">
        <thead className="bg-gray-100 text-left text-xs font-semibold uppercase text-gray-600">
          <tr>
            <th className="px-4 py-3">Título</th>
            <th className="px-4 py-3">Autores</th>
            <th className="px-4 py-3">Área</th>
            <th className="px-4 py-3">Status</th>
            <th className="px-4 py-3">Data</th>
            <th className="px-4 py-3 text-right">Ações</th>
          </tr>
        </thead>
        <tbody className="divide-y divide-gray-100">
          {articles.map((a) => (
            <tr key={a.id} className="hover:bg-gray-50">
              <td className="px-4 py-3 font-medium">{truncate(a.titulo, 70)}</td>
              <td className="px-4 py-3">{truncate(a.autores, 40)}</td>
              <td className="px-4 py-3">{a.area}</td>
              <td className="px-4 py-3"><StatusBadge status={a.statusProcessamento} /></td>
              <td className="whitespace-nowrap px-4 py-3">{formatDate(a.criadoEm)}</td>
              <td className="whitespace-nowrap px-4 py-3 text-right">
                <button className="mr-3 text-blue-600 hover:underline" onClick={() => onView(a)}>Ver</button>
                <button className="mr-3 text-gray-700 hover:underline" onClick={() => onEdit(a)}>Editar</button>
                <button className="text-red-600 hover:underline" onClick={() => onDelete(a)}>Excluir</button>
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  )
}
