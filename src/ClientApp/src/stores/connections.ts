import { defineStore } from 'pinia'
import { ref } from 'vue'
import type { Connection } from '@/types'

export const useConnectionsStore = defineStore('connections', () => {
  const connections = ref<Connection[]>([])
  const loading = ref(false)

  async function fetchConnections() {
    loading.value = true
    try {
      // TODO: Replace with actual API call
      connections.value = []
    } finally {
      loading.value = false
    }
  }

  return { connections, loading, fetchConnections }
})
