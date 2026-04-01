<script setup lang="ts">
import type { SyncRun } from '@/types'
import type { DataTableColumn } from '@/types'
import DataTable from '@/components/shared/DataTable.vue'
import StatusBadge from '@/components/shared/StatusBadge.vue'

defineProps<{
  syncRuns: SyncRun[]
  loading?: boolean
}>()

const columns: DataTableColumn[] = [
  { key: 'startedAt', label: 'Started', sortable: true },
  { key: 'status', label: 'Status', sortable: true },
  { key: 'recordCount', label: 'Records', sortable: true },
  { key: 'fileSize', label: 'File Size', sortable: true },
  { key: 'fileName', label: 'File Name', sortable: false },
  { key: 'completedAt', label: 'Completed', sortable: true },
  { key: 'triggeredBy', label: 'Triggered By', sortable: false },
]

function formatDate(val: unknown): string {
  if (!val || typeof val !== 'string') return '-'
  return new Date(val).toLocaleString()
}

function formatFileSize(val: unknown): string {
  const bytes = Number(val)
  if (isNaN(bytes) || bytes === 0) return '-'
  if (bytes < 1024) return `${bytes} B`
  if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} KB`
  return `${(bytes / (1024 * 1024)).toFixed(1)} MB`
}
</script>

<template>
  <DataTable
    :columns="columns"
    :data="syncRuns as unknown as Record<string, unknown>[]"
    :loading="loading"
    empty-message="No sync history available"
  >
    <template #cell-startedAt="{ value }">
      {{ formatDate(value) }}
    </template>
    <template #cell-status="{ row }">
      <StatusBadge :status="(row as unknown as SyncRun).status" />
    </template>
    <template #cell-fileSize="{ value }">
      {{ formatFileSize(value) }}
    </template>
    <template #cell-completedAt="{ value }">
      {{ formatDate(value) }}
    </template>
  </DataTable>
</template>
