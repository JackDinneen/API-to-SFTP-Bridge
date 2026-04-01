<script setup lang="ts">
import { onMounted, computed, ref } from 'vue'
import { useRouter } from 'vue-router'
import { useConnectionsStore } from '@/stores/connections'
import { ConnectionStatus } from '@/types'
import type { DataTableColumn } from '@/types'
import SummaryCard from '@/components/shared/SummaryCard.vue'
import SearchToolbar from '@/components/shared/SearchToolbar.vue'
import DataTable from '@/components/shared/DataTable.vue'
import StatusBadge from '@/components/shared/StatusBadge.vue'
import HealthIndicator from '@/components/shared/HealthIndicator.vue'
import LoadingSpinner from '@/components/shared/LoadingSpinner.vue'

const router = useRouter()
const connectionsStore = useConnectionsStore()
const searchQuery = ref('')

onMounted(() => {
  connectionsStore.fetchConnections()
})

const filteredConnections = computed(() => {
  const q = searchQuery.value.toLowerCase()
  if (!q) return connectionsStore.connections
  return connectionsStore.connections.filter(
    (c) =>
      c.name.toLowerCase().includes(q) ||
      c.clientName.toLowerCase().includes(q) ||
      c.platformName.toLowerCase().includes(q),
  )
})

const activeCount = computed(
  () =>
    connectionsStore.connections.filter(
      (c) => c.status === ConnectionStatus.Active,
    ).length,
)
const errorCount = computed(
  () =>
    connectionsStore.connections.filter(
      (c) => c.status === ConnectionStatus.Error,
    ).length,
)
const totalCount = computed(() => connectionsStore.connections.length)

const columns: DataTableColumn[] = [
  { key: 'rowNum', label: '#', sortable: false },
  { key: 'name', label: 'Name', sortable: true },
  { key: 'clientPlatform', label: 'Client / Platform', sortable: true },
  { key: 'status', label: 'Status', sortable: true },
  { key: 'lastSyncAt', label: 'Last Sync', sortable: true },
  { key: 'lastSyncRecordCount', label: 'Records', sortable: true },
  { key: 'health', label: 'Health', sortable: true },
  { key: 'nextSyncAt', label: 'Next Sync', sortable: true },
]

const tableData = computed(() =>
  filteredConnections.value.map((c, idx) => ({
    id: c.id,
    rowNum: idx + 1,
    name: c.name,
    clientPlatform: `${c.clientName} / ${c.platformName}`,
    status: c.status,
    lastSyncAt: c.lastSyncAt,
    lastSyncRecordCount: c.lastSyncRecordCount ?? 0,
    health: c.successRate ?? 100,
    nextSyncAt: c.nextSyncAt,
  })),
)

function formatDate(val: unknown): string {
  if (!val || typeof val !== 'string') return '-'
  return new Date(val).toLocaleDateString('en-GB', {
    year: 'numeric',
    month: '2-digit',
    day: '2-digit',
  })
}

function handleRowClick(row: Record<string, unknown>) {
  router.push({ name: 'connection-detail', params: { id: row.id as string } })
}
</script>

<template>
  <div>
    <LoadingSpinner v-if="connectionsStore.loading" size="lg" />

    <template v-else>
      <!-- Summary Tiles -->
      <div class="grid grid-cols-2 gap-4 sm:grid-cols-4">
        <SummaryCard
          title="Active Connections"
          :count="activeCount"
          description="Connections currently running on schedule and delivering CSV files to the SFTP server."
          color="var(--obi-success)"
          icon="link"
        />
        <SummaryCard
          title="Syncs This Month"
          :count="0"
          description="Total number of sync runs (scheduled and manual) completed in the current calendar month."
          color="var(--obi-info)"
          icon="sync"
        />
        <SummaryCard
          title="Records Delivered"
          :count="0"
          description="Total data rows successfully transformed and delivered via SFTP across all connections this month."
          color="hsl(var(--navy-dark))"
          icon="data"
        />
        <SummaryCard
          title="Errors"
          :count="errorCount"
          description="Connections with failed syncs that need attention. Check the connection detail for error logs."
          color="var(--obi-danger)"
          icon="alert"
        />
      </div>

      <!-- Search Toolbar -->
      <SearchToolbar
        placeholder="Search name, client, platform..."
        :result-count="filteredConnections.length"
        @search="(q: string) => (searchQuery = q)"
      />

      <!-- Empty State -->
      <div
        v-if="totalCount === 0"
        class="rounded-lg border border-gray-200 bg-white py-16 text-center"
      >
        <svg
          class="mx-auto h-12 w-12 text-gray-300"
          fill="none"
          stroke="currentColor"
          stroke-width="1"
          viewBox="0 0 24 24"
        >
          <path
            stroke-linecap="round"
            stroke-linejoin="round"
            d="M13.828 10.172a4 4 0 00-5.656 0l-4 4a4 4 0 105.656 5.656l1.102-1.101m-.758-4.899a4 4 0 005.656 0l4-4a4 4 0 00-5.656-5.656l-1.1 1.1"
          />
        </svg>
        <h3 class="mt-4 text-base font-semibold text-gray-900">
          No connections configured
        </h3>
        <p class="mt-2 text-sm text-gray-500 max-w-md mx-auto">
          Create your first API-to-SFTP connection to start pulling data from an
          external REST API, transforming it, and delivering it to Obi's SFTP
          server.
        </p>
        <button
          class="mt-6 inline-flex items-center gap-1.5 rounded-md px-4 py-2 text-sm font-medium text-white"
          style="background-color: hsl(var(--navy-dark))"
          @click="router.push({ name: 'wizard' })"
        >
          <svg
            class="h-4 w-4"
            fill="none"
            stroke="currentColor"
            stroke-width="2"
            viewBox="0 0 24 24"
          >
            <path
              stroke-linecap="round"
              stroke-linejoin="round"
              d="M12 4.5v15m7.5-7.5h-15"
            />
          </svg>
          Create Connection
        </button>
      </div>

      <!-- Connections Table -->
      <DataTable
        v-else
        :columns="columns"
        :data="tableData as unknown as Record<string, unknown>[]"
        :selectable="true"
        empty-message="No matching connections found."
      >
        <template #cell-rowNum="{ value }">
          <span class="text-gray-400">{{ value }}</span>
        </template>
        <template #cell-name="{ row }">
          <button
            class="font-medium text-gray-900 hover:underline"
            @click="handleRowClick(row)"
          >
            {{ row.name }}
          </button>
        </template>
        <template #cell-status="{ row }">
          <StatusBadge
            :status="
              (row as Record<string, unknown>).status as ConnectionStatus
            "
          />
        </template>
        <template #cell-lastSyncAt="{ value }">
          {{ formatDate(value) }}
        </template>
        <template #cell-health="{ row }">
          <HealthIndicator
            :success-rate="(row as Record<string, unknown>).health as number"
          />
        </template>
        <template #cell-nextSyncAt="{ value }">
          <span
            :class="{
              'text-red-600 font-medium':
                value && new Date(value as string) < new Date(),
            }"
          >
            {{ formatDate(value) }}
          </span>
        </template>
      </DataTable>
    </template>
  </div>
</template>
