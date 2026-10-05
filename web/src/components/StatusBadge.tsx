import type { StatusProcessamento } from '../types/article'

const STYLES: Record<StatusProcessamento, string> = {
  pendente: 'bg-gray-200 text-gray-800',
  processando: 'bg-yellow-200 text-yellow-800',
  concluido: 'bg-green-200 text-green-800',
  erro: 'bg-red-200 text-red-800',
}

export default function StatusBadge({ status }: { status: StatusProcessamento }) {
  return (
    <span className={`inline-block rounded-full px-2.5 py-0.5 text-xs font-medium ${STYLES[status] ?? STYLES.pendente}`}>
      {status}
    </span>
  )
}
