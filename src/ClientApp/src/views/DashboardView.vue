<script setup lang="ts">
import { onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { useConnectionsStore } from '@/stores/connections'
import { ConnectionStatus } from '@/types'
import ConnectionCard from '@/components/dashboard/ConnectionCard.vue'
import LoadingSpinner from '@/components/shared/LoadingSpinner.vue'

const router = useRouter()
const connectionsStore = useConnectionsStore()

onMounted(() => {
  connectionsStore.fetchConnections()
})

async function handleSyncNow(id: string) {
  await connectionsStore.triggerSync(id)
  await connectionsStore.fetchConnections()
}

async function handleTogglePause(id: string) {
  const conn = connectionsStore.connections.find((c) => c.id === id)
  if (!conn) return
  const newStatus =
    conn.status === ConnectionStatus.Paused
      ? ConnectionStatus.Active
      : ConnectionStatus.Paused
  await connectionsStore.updateConnection(id, { status: newStatus })
  await connectionsStore.fetchConnections()
}

function handleViewDetails(id: string) {
  router.push({ name: 'connection-detail', params: { id } })
}
</script>

<template>
  <div>
    <div class="mb-6 flex items-center justify-between">
      <div>
        <h2 class="text-2xl font-bold text-gray-900">Dashboard</h2>
        <p class="mt-1 text-sm text-gray-500">
          Manage your API-to-SFTP connections
        </p>
      </div>
      <RouterLink
        :to="{ name: 'wizard' }"
        class="rounded-md bg-indigo-600 px-4 py-2 text-sm font-medium text-white shadow-sm hover:bg-indigo-700"
      >
        New Connection
      </RouterLink>
    </div>

    <div
      v-if="connectionsStore.error"
      class="mb-4 rounded-md bg-red-50 p-4 text-sm text-red-700"
      role="alert"
    >
      {{ connectionsStore.error }}
    </div>

    <LoadingSpinner v-if="connectionsStore.loading" size="lg" />

    <div
      v-else-if="connectionsStore.connections.length === 0"
      class="rounded-lg border-2 border-dashed border-gray-300 p-12 text-center"
      data-testid="empty-state"
    >
      <svg
        class="mx-auto h-12 w-12 text-gray-400"
        fill="none"
        viewBox="0 0 24 24"
        stroke="currentColor"
        aria-hidden="true"
      >
        <path
          stroke-linecap="round"
          stroke-linejoin="round"
          stroke-width="1.5"
          d="M13.19 8.688a4.5 4.5 0 011.242 7.244l-4.5 4.5a4.5 4.5 0 01-6.364-6.364l1.757-1.757m9.86-2.04a4.5 4.5 0 00-6.364-6.364L4.34 8.398a4.5 4.5 0 001.242 7.244"
        />
      </svg>
      <h3 class="mt-4 text-lg font-medium text-gray-900">No connections yet</h3>
      <p class="mt-2 text-sm text-gray-500">
        Get started by creating your first API-to-SFTP connection.
      </p>
      <RouterLink
        :to="{ name: 'wizard' }"
        class="mt-4 inline-block rounded-md bg-indigo-600 px-4 py-2 text-sm font-medium text-white hover:bg-indigo-700"
      >
        Create Connection
      </RouterLink>
    </div>

    <div v-else class="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
      <ConnectionCard
        v-for="connection in connectionsStore.connections"
        :key="connection.id"
        :connection="connection"
        @sync-now="handleSyncNow"
        @toggle-pause="handleTogglePause"
        @view-details="handleViewDetails"
      />
    </div>
  </div>
</template>
