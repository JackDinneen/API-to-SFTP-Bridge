<script setup lang="ts">
import { onMounted, computed } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useConnectionsStore } from '@/stores/connections'
import { useSyncStore } from '@/stores/sync'
import { ConnectionStatus } from '@/types'
import StatusBadge from '@/components/shared/StatusBadge.vue'
import HealthIndicator from '@/components/shared/HealthIndicator.vue'
import SyncHistoryTable from '@/components/dashboard/SyncHistoryTable.vue'
import LoadingSpinner from '@/components/shared/LoadingSpinner.vue'

const route = useRoute()
const router = useRouter()
const connectionsStore = useConnectionsStore()
const syncStore = useSyncStore()

const connectionId = route.params.id as string

const connection = computed(() => connectionsStore.currentConnection)

onMounted(async () => {
  await Promise.all([
    connectionsStore.fetchConnection(connectionId),
    syncStore.fetchSyncHistory(connectionId),
    syncStore.fetchLatestSync(connectionId),
  ])
})

async function handleSyncNow() {
  await connectionsStore.triggerSync(connectionId)
  await syncStore.fetchSyncHistory(connectionId)
  await syncStore.fetchLatestSync(connectionId)
}

async function handleTogglePause() {
  if (!connection.value) return
  const newStatus =
    connection.value.status === ConnectionStatus.Paused
      ? ConnectionStatus.Active
      : ConnectionStatus.Paused
  await connectionsStore.updateConnection(connectionId, { status: newStatus })
  await connectionsStore.fetchConnection(connectionId)
}

function formatDate(dateStr: string | undefined): string {
  if (!dateStr) return '-'
  return new Date(dateStr).toLocaleString()
}

function downloadLastCsv() {
  if (syncStore.latestSync?.fileName) {
    const baseUrl = import.meta.env.VITE_API_BASE_URL ?? '/api'
    window.open(
      `${baseUrl}/connections/${connectionId}/sync-runs/${syncStore.latestSync.id}/download`,
      '_blank',
    )
  }
}
</script>

<template>
  <div>
    <button
      class="mb-4 text-sm text-indigo-600 hover:text-indigo-800"
      @click="router.push({ name: 'dashboard' })"
    >
      &larr; Back to Dashboard
    </button>

    <LoadingSpinner v-if="connectionsStore.loading && !connection" size="lg" />

    <template v-else-if="connection">
      <div
        class="mb-6 rounded-lg border border-gray-200 bg-white p-6 shadow-sm"
      >
        <div class="flex items-start justify-between">
          <div>
            <h2 class="text-2xl font-bold text-gray-900">
              {{ connection.name }}
            </h2>
            <p class="mt-1 text-sm text-gray-500">
              {{ connection.clientName }} / {{ connection.platformName }}
            </p>
          </div>
          <StatusBadge :status="connection.status" />
        </div>

        <div class="mt-6 grid grid-cols-2 gap-4 sm:grid-cols-4 text-sm">
          <div>
            <span class="text-gray-500">Base URL</span>
            <p class="mt-1 font-medium text-gray-900 break-all">
              {{ connection.baseUrl }}
            </p>
          </div>
          <div>
            <span class="text-gray-500">Auth Type</span>
            <p class="mt-1 font-medium text-gray-900">
              {{ connection.authType }}
            </p>
          </div>
          <div>
            <span class="text-gray-500">Schedule</span>
            <p class="mt-1 font-medium text-gray-900">
              {{ connection.scheduleCron || 'Not set' }}
            </p>
          </div>
          <div>
            <span class="text-gray-500">Health</span>
            <HealthIndicator :success-rate="connection.successRate ?? 100" />
          </div>
          <div>
            <span class="text-gray-500">SFTP Host</span>
            <p class="mt-1 font-medium text-gray-900">
              {{ connection.sftpHost || '-' }}
            </p>
          </div>
          <div>
            <span class="text-gray-500">Reporting Lag</span>
            <p class="mt-1 font-medium text-gray-900">
              {{ connection.reportingLagDays }} days
            </p>
          </div>
          <div>
            <span class="text-gray-500">Created</span>
            <p class="mt-1 font-medium text-gray-900">
              {{ formatDate(connection.createdAt) }}
            </p>
          </div>
          <div>
            <span class="text-gray-500">Updated</span>
            <p class="mt-1 font-medium text-gray-900">
              {{ formatDate(connection.updatedAt) }}
            </p>
          </div>
        </div>

        <div class="mt-6 flex gap-3 border-t border-gray-100 pt-4">
          <button
            class="rounded-md bg-indigo-600 px-4 py-2 text-sm font-medium text-white hover:bg-indigo-700"
            @click="handleSyncNow"
          >
            Sync Now
          </button>
          <button
            class="rounded-md border border-gray-300 px-4 py-2 text-sm font-medium text-gray-700 hover:bg-gray-50"
            @click="handleTogglePause"
          >
            {{
              connection.status === ConnectionStatus.Paused ? 'Resume' : 'Pause'
            }}
          </button>
          <button
            class="rounded-md border border-gray-300 px-4 py-2 text-sm font-medium text-gray-700 hover:bg-gray-50"
            :disabled="!syncStore.latestSync?.fileName"
            :class="{
              'opacity-50 cursor-not-allowed': !syncStore.latestSync?.fileName,
            }"
            @click="downloadLastCsv"
          >
            Download Last CSV
          </button>
          <RouterLink
            :to="{ name: 'wizard', query: { edit: connectionId } }"
            class="rounded-md border border-gray-300 px-4 py-2 text-sm font-medium text-gray-700 hover:bg-gray-50"
          >
            Edit Mappings
          </RouterLink>
        </div>
      </div>

      <div
        v-if="syncStore.latestSync"
        class="mb-6 rounded-lg border border-gray-200 bg-white p-6 shadow-sm"
      >
        <h3 class="text-lg font-semibold text-gray-900 mb-4">Latest Sync</h3>
        <div class="grid grid-cols-2 gap-4 sm:grid-cols-4 text-sm">
          <div>
            <span class="text-gray-500">Status</span>
            <div class="mt-1">
              <StatusBadge :status="syncStore.latestSync.status" />
            </div>
          </div>
          <div>
            <span class="text-gray-500">Started</span>
            <p class="mt-1 font-medium text-gray-900">
              {{ formatDate(syncStore.latestSync.startedAt) }}
            </p>
          </div>
          <div>
            <span class="text-gray-500">Records</span>
            <p class="mt-1 font-medium text-gray-900">
              {{ syncStore.latestSync.recordCount }}
            </p>
          </div>
          <div>
            <span class="text-gray-500">File</span>
            <p class="mt-1 font-medium text-gray-900">
              {{ syncStore.latestSync.fileName || '-' }}
            </p>
          </div>
          <div v-if="syncStore.latestSync.errorMessage" class="col-span-full">
            <span class="text-gray-500">Error</span>
            <p class="mt-1 text-sm text-red-600">
              {{ syncStore.latestSync.errorMessage }}
            </p>
          </div>
        </div>
      </div>

      <div class="rounded-lg border border-gray-200 bg-white p-6 shadow-sm">
        <h3 class="text-lg font-semibold text-gray-900 mb-4">Sync History</h3>
        <SyncHistoryTable
          :sync-runs="syncStore.syncHistory"
          :loading="syncStore.loading"
        />
      </div>
    </template>

    <div
      v-else
      class="rounded-md bg-red-50 p-4 text-sm text-red-700"
      role="alert"
    >
      {{ connectionsStore.error ?? 'Connection not found' }}
    </div>
  </div>
</template>
