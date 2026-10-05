import axios from 'axios'

export const apiClient = axios.create({
  baseURL: import.meta.env.VITE_API_BASE_URL ?? 'http://localhost:5000',
})

apiClient.interceptors.response.use(
  (response) => response,
  (error: unknown) => {
    if (!axios.isAxiosError(error)) return Promise.reject(error)
    if (!error.response) {
      return Promise.reject(new Error('Não foi possível conectar à API. Verifique se o backend está no ar.'))
    }
    const data = error.response.data as { title?: string; detail?: string; message?: string } | string | undefined
    const detail = typeof data === 'string' ? data : (data?.detail ?? data?.title ?? data?.message)
    return Promise.reject(new Error(detail || `Erro ${error.response.status} ao chamar a API.`))
  },
)
