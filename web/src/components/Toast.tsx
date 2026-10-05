import { useCallback, useState, type ReactNode } from 'react'
import { ToastContext, type ToastType } from '../hooks/useToast'

interface Item {
  id: number
  type: ToastType
  message: string
}

const STYLES: Record<ToastType, string> = {
  success: 'bg-green-600',
  error: 'bg-red-600',
  info: 'bg-blue-600',
}

export default function ToastProvider({ children }: { children: ReactNode }) {
  const [items, setItems] = useState<Item[]>([])

  const notify = useCallback((type: ToastType, message: string) => {
    const id = Date.now() + Math.random()
    setItems((list) => [...list, { id, type, message }])
    setTimeout(() => setItems((list) => list.filter((i) => i.id !== id)), 3000)
  }, [])

  return (
    <ToastContext value={{ notify }}>
      {children}
      <div className="fixed right-4 top-4 z-50 flex flex-col gap-2" aria-live="polite">
        {items.map((i) => (
          <div key={i.id} className={`rounded px-4 py-3 text-sm text-white shadow-lg ${STYLES[i.type]}`}>
            {i.message}
          </div>
        ))}
      </div>
    </ToastContext>
  )
}
