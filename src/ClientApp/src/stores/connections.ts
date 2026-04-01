import { defineStore } from 'pinia'
import { ref } from 'vue'
import type { Connection } from '@/types'
import { useApi } from '@/composables/useApi'

export const useConnectionsStore = defineStore('connections', () => {
  const connections = ref<Connection[]>([])
  const currentConnection = ref<Connection | null>(null)
  const loading = ref(false)
  const error = ref<string | null>(null)

  const api = useApi()

  async function fetchConnections() {
    loading.value = true
    error.value = null
    try {
      const result = await api.getAsync<Connection[]>('/connections')
      if (result.success && result.data) {
        connections.value = result.data
      } else {
        error.value = result.message ?? 'Failed to fetch connections'
      }
    } catch (err: unknown) {
      error.value = err instanceof Error ? err.message : 'Network error'
    } finally {
      loading.value = false
    }
  }

  async function fetchConnection(id: string) {
    loading.value = true
    error.value = null
    try {
      const result = await api.getAsync<Connection>(`/connections/${id}`)
      if (result.success && result.data) {
        currentConnection.value = result.data
      } else {
        error.value = result.message ?? 'Failed to fetch connection'
      }
    } catch (err: unknown) {
      error.value = err instanceof Error ? err.message : 'Network error'
    } finally {
      loading.value = false
    }
  }

  async function createConnection(data: Partial<Connection>) {
    loading.value = true
    error.value = null
    try {
      const result = await api.postAsync<Connection>('/connections', data)
      if (result.success && result.data) {
        connections.value.push(result.data)
        return result.data
      } else {
        error.value = result.message ?? 'Failed to create connection'
        return null
      }
    } catch (err: unknown) {
      error.value = err instanceof Error ? err.message : 'Network error'
      return null
    } finally {
      loading.value = false
    }
  }

  async function updateConnection(id: string, data: Partial<Connection>) {
    loading.value = true
    error.value = null
    try {
      const result = await api.putAsync<Connection>(`/connections/${id}`, data)
      if (result.success && result.data) {
        const index = connections.value.findIndex((c) => c.id === id)
        if (index !== -1) {
          connections.value[index] = result.data
        }
        if (currentConnection.value?.id === id) {
          currentConnection.value = result.data
        }
        return result.data
      } else {
        error.value = result.message ?? 'Failed to update connection'
        return null
      }
    } catch (err: unknown) {
      error.value = err instanceof Error ? err.message : 'Network error'
      return null
    } finally {
      loading.value = false
    }
  }

  async function deleteConnection(id: string) {
    loading.value = true
    error.value = null
    try {
      const result = await api.deleteAsync<void>(`/connections/${id}`)
      if (result.success) {
        connections.value = connections.value.filter((c) => c.id !== id)
        if (currentConnection.value?.id === id) {
          currentConnection.value = null
        }
        return true
      } else {
        error.value = result.message ?? 'Failed to delete connection'
        return false
      }
    } catch (err: unknown) {
      error.value = err instanceof Error ? err.message : 'Network error'
      return false
    } finally {
      loading.value = false
    }
  }

  async function triggerSync(id: string) {
    error.value = null
    try {
      const result = await api.postAsync<void>(`/connections/${id}/sync`)
      if (!result.success) {
        error.value = result.message ?? 'Failed to trigger sync'
        return false
      }
      return true
    } catch (err: unknown) {
      error.value = err instanceof Error ? err.message : 'Network error'
      return false
    }
  }

  async function testConnection(id: string) {
    error.value = null
    try {
      const result = await api.postAsync<{ ok: boolean }>(
        `/connections/${id}/test`,
      )
      if (result.success && result.data) {
        return result.data.ok
      }
      error.value = result.message ?? 'Connection test failed'
      return false
    } catch (err: unknown) {
      error.value = err instanceof Error ? err.message : 'Network error'
      return false
    }
  }

  return {
    connections,
    currentConnection,
    loading,
    error,
    fetchConnections,
    fetchConnection,
    createConnection,
    updateConnection,
    deleteConnection,
    triggerSync,
    testConnection,
  }
})
