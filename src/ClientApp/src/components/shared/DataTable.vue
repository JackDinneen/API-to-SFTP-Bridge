<script setup lang="ts">
import { ref, computed } from 'vue'
import type { DataTableColumn } from '@/types'
import LoadingSpinner from './LoadingSpinner.vue'

const props = defineProps<{
  columns: DataTableColumn[]
  data: Record<string, unknown>[]
  loading?: boolean
  emptyMessage?: string
}>()

const sortKey = ref<string | null>(null)
const sortAsc = ref(true)

function toggleSort(column: DataTableColumn) {
  if (!column.sortable) return
  if (sortKey.value === column.key) {
    sortAsc.value = !sortAsc.value
  } else {
    sortKey.value = column.key
    sortAsc.value = true
  }
}

const sortedData = computed(() => {
  if (!sortKey.value) return props.data
  const key = sortKey.value
  const dir = sortAsc.value ? 1 : -1
  return [...props.data].sort((a, b) => {
    const aVal = a[key]
    const bVal = b[key]
    if (aVal == null && bVal == null) return 0
    if (aVal == null) return 1
    if (bVal == null) return -1
    if (typeof aVal === 'string' && typeof bVal === 'string') {
      return aVal.localeCompare(bVal) * dir
    }
    return (Number(aVal) - Number(bVal)) * dir
  })
})

function getSortIndicator(column: DataTableColumn): string {
  if (!column.sortable) return ''
  if (sortKey.value !== column.key) return ' \u2195'
  return sortAsc.value ? ' \u2191' : ' \u2193'
}
</script>

<template>
  <div class="overflow-x-auto">
    <LoadingSpinner v-if="loading" size="md" />
    <div
      v-else-if="data.length === 0"
      class="py-12 text-center text-gray-500"
      role="status"
    >
      {{ emptyMessage ?? 'No data available' }}
    </div>
    <table v-else class="min-w-full divide-y divide-gray-200">
      <thead class="bg-gray-50">
        <tr>
          <th
            v-for="col in columns"
            :key="col.key"
            class="px-4 py-3 text-left text-xs font-medium uppercase tracking-wider text-gray-500"
            :class="{
              'cursor-pointer select-none hover:text-gray-700': col.sortable,
            }"
            :aria-sort="
              sortKey === col.key
                ? sortAsc
                  ? 'ascending'
                  : 'descending'
                : undefined
            "
            @click="toggleSort(col)"
          >
            {{ col.label }}{{ getSortIndicator(col) }}
          </th>
        </tr>
      </thead>
      <tbody class="divide-y divide-gray-200 bg-white">
        <tr
          v-for="(row, idx) in sortedData"
          :key="idx"
          class="hover:bg-gray-50"
        >
          <td
            v-for="col in columns"
            :key="col.key"
            class="whitespace-nowrap px-4 py-3 text-sm text-gray-700"
          >
            <slot :name="`cell-${col.key}`" :row="row" :value="row[col.key]">
              {{ row[col.key] ?? '-' }}
            </slot>
          </td>
        </tr>
      </tbody>
    </table>
  </div>
</template>
