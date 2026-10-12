import { BrowserRouter, Route, Routes } from 'react-router-dom'
import Layout from './components/Layout'
import About from './pages/About'
import ArticleDetails from './pages/ArticleDetails'
import ArticleForm from './pages/ArticleForm'
import Home from './pages/Home'
import { ToastProvider } from './toast'

export default function App() {
  return (
    <ToastProvider>
      <BrowserRouter>
        <Routes>
          <Route element={<Layout />}>
            <Route path="/" element={<Home />} />
            <Route path="/articles/new" element={<ArticleForm />} />
            <Route path="/articles/:id" element={<ArticleDetails />} />
            <Route path="/articles/:id/edit" element={<ArticleForm />} />
            <Route path="/about" element={<About />} />
            <Route path="*" element={<div className="alert alert-warning">Página não encontrada.</div>} />
          </Route>
        </Routes>
      </BrowserRouter>
    </ToastProvider>
  )
}
