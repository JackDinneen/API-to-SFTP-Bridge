import { defineStore } from 'pinia'
import { ref } from 'vue'
import type { SyncRun } from '@/types'
import { useApi } from '@/composables/useApi'

export const useSyncStore = defineStore('sync', () => {
  const syncHistory = ref<SyncRun[]>([])
  const latestSync = ref<SyncRun | null>(null)
  const loading = ref(false)
  const error = ref<string | null>(null)

  const api = useApi()

  async function fetchSyncHistory(connectionId: string) {
    loading.value = true
    error.value = null
    try {
      const result = await api.getAsync<SyncRun[]>(
        `/sync/${connectionId}/history`,
      )
      if (result.success && result.data) {
        syncHistory.value = result.data
      } else {
        error.value = result.message ?? 'Failed to fetch sync history'
      }
    } catch (err: unknown) {
      error.value = err instanceof Error ? err.message : 'Network error'
    } finally {
      loading.value = false
    }
  }

  async function fetchLatestSync(connectionId: string) {
    error.value = null
    try {
      const result = await api.getAsync<SyncRun>(
        `/sync/${connectionId}/latest`,
      )
      if (result.success && result.data) {
        latestSync.value = result.data
      } else {
        latestSync.value = null
      }
    } catch (err: unknown) {
      error.value = err instanceof Error ? err.message : 'Network error'
    }
  }

  function $reset() {
    syncHistory.value = []
    latestSync.value = null
    loading.value = false
    error.value = null
  }

  return {
    syncHistory,
    latestSync,
    loading,
    error,
    fetchSyncHistory,
    fetchLatestSync,
    $reset,
  }
})
