export const formatDate = (iso: string) => new Date(iso).toLocaleDateString('pt-BR')

export const formatDateTime = (iso: string) => new Date(iso).toLocaleString('pt-BR')

export const truncate = (text: string, max = 80) => (text.length > max ? `${text.slice(0, max - 1)}…` : text)
