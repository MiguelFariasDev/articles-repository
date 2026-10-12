import type { ProcessingStatus } from '../types'

const STYLES: Record<ProcessingStatus, string> = {
  pendente: 'bg-secondary',
  processando: 'bg-warning text-dark',
  concluido: 'bg-success',
  erro: 'bg-danger',
}

export default function StatusBadge({ status }: { status: ProcessingStatus }) {
  return <span className={`badge ${STYLES[status] ?? 'bg-secondary'}`}>{status}</span>
}
