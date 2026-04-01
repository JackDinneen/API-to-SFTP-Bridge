import { ref } from 'vue'
import type { ApiResponse } from '@/types'

const API_BASE_URL = import.meta.env.VITE_API_BASE_URL ?? '/api'

interface UseApiReturn<T> {
  data: ReturnType<typeof ref<T | null>>
  error: ReturnType<typeof ref<string | null>>
  loading: ReturnType<typeof ref<boolean>>
}

function getAuthHeaders(): Record<string, string> {
  const token = localStorage.getItem('auth_token')
  if (token) {
    return { Authorization: `Bearer ${token}` }
  }
  return {}
}

async function request<T>(
  method: string,
  url: string,
  body?: unknown,
): Promise<ApiResponse<T>> {
  const headers: Record<string, string> = {
    'Content-Type': 'application/json',
    Accept: 'application/json',
    ...getAuthHeaders(),
  }

  const options: RequestInit = {
    method,
    headers,
  }

  if (body !== undefined) {
    options.body = JSON.stringify(body)
  }

  const response = await fetch(`${API_BASE_URL}${url}`, options)

  if (!response.ok) {
    const errorText = await response.text().catch(() => 'Unknown error')
    return {
      success: false,
      message: `HTTP ${response.status}: ${errorText}`,
      errors: [errorText],
    }
  }

  const result = (await response.json()) as ApiResponse<T>
  return result
}

export function useApi() {
  function get<T>(url: string): UseApiReturn<T> {
    const data = ref<T | null>(null) as ReturnType<typeof ref<T | null>>
    const error = ref<string | null>(null)
    const loading = ref(false)

    loading.value = true
    error.value = null

    request<T>('GET', url)
      .then((result) => {
        if (result.success && result.data !== undefined) {
          data.value = result.data
        } else {
          error.value = result.message ?? 'Request failed'
        }
      })
      .catch((err: unknown) => {
        error.value = err instanceof Error ? err.message : 'Network error'
      })
      .finally(() => {
        loading.value = false
      })

    return { data, error, loading }
  }

  async function getAsync<T>(url: string): Promise<ApiResponse<T>> {
    return request<T>('GET', url)
  }

  async function postAsync<T>(
    url: string,
    body?: unknown,
  ): Promise<ApiResponse<T>> {
    return request<T>('POST', url, body)
  }

  async function putAsync<T>(
    url: string,
    body?: unknown,
  ): Promise<ApiResponse<T>> {
    return request<T>('PUT', url, body)
  }

  async function deleteAsync<T>(url: string): Promise<ApiResponse<T>> {
    return request<T>('DELETE', url)
  }

  return { get, getAsync, postAsync, putAsync, deleteAsync }
}
