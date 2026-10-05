import { createContext, useContext } from 'react'

export type ToastType = 'success' | 'error' | 'info'

export interface ToastContextValue {
  notify: (type: ToastType, message: string) => void
}

export const ToastContext = createContext<ToastContextValue>({ notify: () => {} })

export const useToast = () => useContext(ToastContext)
