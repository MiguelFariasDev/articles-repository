export default function Loading() {
  return (
    <div className="flex justify-center py-12" role="status" aria-label="Carregando">
      <div className="h-8 w-8 animate-spin rounded-full border-4 border-blue-200 border-t-blue-600" />
    </div>
  )
}
