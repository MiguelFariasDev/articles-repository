import { NavLink, Outlet } from 'react-router-dom'

export default function Layout() {
  return (
    <div className="d-flex flex-column min-vh-100">
      <header className="app-header text-white py-3">
        <div className="container d-flex align-items-center gap-2">
          <span className="fs-3" aria-hidden="true">📚</span>
          <span className="fs-4 fw-semibold">Articles Repository</span>
        </div>
      </header>
      <nav className="navbar navbar-expand bg-body-tertiary border-bottom">
        <div className="container">
          <ul className="navbar-nav flex-row gap-3">
            <li className="nav-item"><NavLink className="nav-link" to="/" end>Home</NavLink></li>
            <li className="nav-item"><NavLink className="nav-link" to="/articles/new">Novo Artigo</NavLink></li>
            <li className="nav-item"><NavLink className="nav-link" to="/about">Sobre</NavLink></li>
          </ul>
        </div>
      </nav>
      <main className="container py-4 flex-grow-1">
        <Outlet />
      </main>
      <footer className="text-center text-muted border-top py-3 small">
        Trabalho acadêmico — Serviços AWS 2025
      </footer>
    </div>
  )
}
