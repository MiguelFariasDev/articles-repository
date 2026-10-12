import { createContext, useCallback, useContext, useState, type ReactNode } from 'react'

type Kind = 'success' | 'danger'
interface Toast { id: number; kind: Kind; message: string }

const ToastContext = createContext<(kind: Kind, message: string) => void>(() => {})

export const useToast = () => useContext(ToastContext)

export function ToastProvider({ children }: { children: ReactNode }) {
  const [toasts, setToasts] = useState<Toast[]>([])

  const push = useCallback((kind: Kind, message: string) => {
    const id = Date.now() + Math.random()
    setToasts((t) => [...t, { id, kind, message }])
    setTimeout(() => setToasts((t) => t.filter((x) => x.id !== id)), 4000)
  }, [])

  return (
    <ToastContext value={push}>
      {children}
      <div className="toast-container position-fixed top-0 end-0 p-3" role="status" aria-live="polite">
        {toasts.map((t) => (
          <div key={t.id} className={`toast show text-bg-${t.kind} border-0 mb-2`}>
            <div className="toast-body">{t.message}</div>
          </div>
        ))}
      </div>
    </ToastContext>
  )
}
