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
const pausedCount = computed(
  () =>
    connectionsStore.connections.filter(
      (c) => c.status === ConnectionStatus.Paused,
    ).length,
)
const errorCount = computed(
  () =>
    connectionsStore.connections.filter(
      (c) => c.status === ConnectionStatus.Error,
    ).length,
)
const totalCount = computed(() => connectionsStore.connections.length)

const summarySegments = computed(() => {
  const total = totalCount.value || 1
  return [
    { color: 'var(--obi-success)', value: (activeCount.value / total) * 100 },
    { color: 'var(--obi-warning)', value: (pausedCount.value / total) * 100 },
    { color: 'var(--obi-danger)', value: (errorCount.value / total) * 100 },
  ]
})

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
      <!-- Summary Cards -->
      <div class="grid grid-cols-2 gap-4 sm:grid-cols-4 lg:grid-cols-4">
        <SummaryCard
          title="Active"
          :count="activeCount"
          :segments="summarySegments"
        />
        <SummaryCard
          title="Paused"
          :count="pausedCount"
          :segments="[
            { color: 'var(--obi-warning)', value: 60 },
            { color: 'var(--obi-success)', value: 40 },
          ]"
        />
        <SummaryCard
          title="Error"
          :count="errorCount"
          :segments="[
            { color: 'var(--obi-danger)', value: 70 },
            { color: 'var(--obi-warning)', value: 30 },
          ]"
        />
        <SummaryCard
          title="Total"
          :count="totalCount"
          :segments="summarySegments"
        />
      </div>

      <!-- Search Toolbar -->
      <SearchToolbar
        placeholder="Search name, client, platform..."
        :result-count="filteredConnections.length"
        @search="(q: string) => (searchQuery = q)"
      />

      <!-- Connections Table -->
      <DataTable
        :columns="columns"
        :data="tableData as unknown as Record<string, unknown>[]"
        :selectable="true"
        empty-message="No connections yet. Click '+ ADD NEW' to create one."
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
