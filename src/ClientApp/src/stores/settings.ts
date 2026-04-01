import { defineStore } from 'pinia'
import { ref } from 'vue'
import type { ReferenceDataItem, UserProfile } from '@/types'
import { useApi } from '@/composables/useApi'

export const useSettingsStore = defineStore('settings', () => {
  const referenceData = ref<ReferenceDataItem[]>([])
  const userProfile = ref<UserProfile | null>(null)
  const loading = ref(false)
  const error = ref<string | null>(null)

  const api = useApi()

  async function fetchReferenceData() {
    loading.value = true
    error.value = null
    try {
      const result = await api.getAsync<ReferenceDataItem[]>('/reference-data')
      if (result.success && result.data) {
        referenceData.value = result.data
      } else {
        error.value = result.message ?? 'Failed to fetch reference data'
      }
    } catch (err: unknown) {
      error.value = err instanceof Error ? err.message : 'Network error'
    } finally {
      loading.value = false
    }
  }

  async function uploadReferenceData(file: File): Promise<number | null> {
    loading.value = true
    error.value = null
    try {
      const formData = new FormData()
      formData.append('file', file)

      const baseUrl = import.meta.env.VITE_API_BASE_URL ?? '/api'
      const response = await fetch(`${baseUrl}/reference-data/upload`, {
        method: 'POST',
        body: formData,
      })

      const result = await response.json()
      if (result.success) {
        await fetchReferenceData()
        return result.data as number
      } else {
        error.value = result.message ?? 'Upload failed'
        return null
      }
    } catch (err: unknown) {
      error.value = err instanceof Error ? err.message : 'Upload error'
      return null
    } finally {
      loading.value = false
    }
  }

  async function deleteReferenceData(id: string) {
    error.value = null
    try {
      const result = await api.deleteAsync<boolean>(`/reference-data/${id}`)
      if (result.success) {
        referenceData.value = referenceData.value.filter((r) => r.id !== id)
        return true
      } else {
        error.value = result.message ?? 'Failed to delete'
        return false
      }
    } catch (err: unknown) {
      error.value = err instanceof Error ? err.message : 'Network error'
      return false
    }
  }

  async function fetchUserProfile() {
    try {
      const result = await api.getAsync<UserProfile>('/auth/me')
      if (result.success && result.data) {
        userProfile.value = result.data
      }
    } catch {
      // Silently fail — not critical
    }
  }

  return {
    referenceData,
    userProfile,
    loading,
    error,
    fetchReferenceData,
    uploadReferenceData,
    deleteReferenceData,
    fetchUserProfile,
  }
})
