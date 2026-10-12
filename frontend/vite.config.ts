import react from '@vitejs/plugin-react'
import { defineConfig, loadEnv } from 'vite'

// https://vite.dev/config/
export default defineConfig(({ mode }) => {
  const env = loadEnv(mode, process.cwd(), '')
  // Em dev o navegador fala só com o próprio Vite (mesma origem); ele encaminha /api e /health
  // para o backend, evitando CORS/mixed content/HTTPS-only. Alvo: API_PROXY_TARGET (ex.: o DNS do ALB).
  const target = env.API_PROXY_TARGET || 'http://localhost:5019'
  return {
    plugins: [react()],
    server: {
      proxy: {
        '/api': { target, changeOrigin: true },
        '/health': { target, changeOrigin: true },
      },
    },
  }
})
