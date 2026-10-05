import type { ReactNode } from 'react'
import { NavLink } from 'react-router-dom'

const link = ({ isActive }: { isActive: boolean }) =>
  `rounded px-3 py-1.5 text-sm font-medium ${isActive ? 'bg-white text-blue-700' : 'text-blue-50 hover:bg-blue-500'}`

export default function Layout({ children }: { children: ReactNode }) {
  return (
    <div className="flex min-h-screen flex-col bg-gray-50 text-gray-900">
      <header className="bg-blue-600 text-white">
        <div className="mx-auto flex max-w-6xl flex-wrap items-center justify-between gap-2 px-4 py-3">
          <div className="flex items-center gap-2 text-lg font-semibold">
            <span aria-hidden="true">📚</span> Articles Repository
          </div>
          <nav className="flex gap-1">
            <NavLink to="/" end className={link}>Home</NavLink>
            <NavLink to="/articles/new" className={link}>Novo Artigo</NavLink>
            <NavLink to="/about" className={link}>Sobre</NavLink>
          </nav>
        </div>
      </header>
      <main className="mx-auto w-full max-w-6xl flex-1 px-4 py-6">{children}</main>
      <footer className="border-t bg-white py-4 text-center text-sm text-gray-500">
        Trabalho acadêmico — Serviços AWS 2025
      </footer>
    </div>
  )
}
