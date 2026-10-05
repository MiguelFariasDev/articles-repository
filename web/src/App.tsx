import { BrowserRouter, Route, Routes } from 'react-router-dom'
import Layout from './components/Layout'
import ToastProvider from './components/Toast'
import AboutPage from './pages/AboutPage'
import ArticleDetailsPage from './pages/ArticleDetailsPage'
import ArticleFormPage from './pages/ArticleFormPage'
import HomePage from './pages/HomePage'

export default function App() {
  return (
    <ToastProvider>
      <BrowserRouter>
        <Layout>
          <Routes>
            <Route path="/" element={<HomePage />} />
            <Route path="/articles/new" element={<ArticleFormPage />} />
            <Route path="/articles/:id" element={<ArticleDetailsPage />} />
            <Route path="/articles/:id/edit" element={<ArticleFormPage />} />
            <Route path="/about" element={<AboutPage />} />
            <Route path="*" element={<p>Página não encontrada.</p>} />
          </Routes>
        </Layout>
      </BrowserRouter>
    </ToastProvider>
  )
}
