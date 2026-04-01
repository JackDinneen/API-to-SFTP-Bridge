<script setup lang="ts">
import { ref, computed } from 'vue'
import type { DataTableColumn } from '@/types'
import LoadingSpinner from './LoadingSpinner.vue'

const props = defineProps<{
  columns: DataTableColumn[]
  data: Record<string, unknown>[]
  loading?: boolean
  emptyMessage?: string
  selectable?: boolean
}>()

const sortKey = ref<string | null>(null)
const sortAsc = ref(true)
const selectedRows = ref<Set<number>>(new Set())

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

function getSortIcon(column: DataTableColumn): string | null {
  if (!column.sortable) return null
  if (sortKey.value !== column.key) return 'both'
  return sortAsc.value ? 'asc' : 'desc'
}

function toggleRow(idx: number) {
  if (selectedRows.value.has(idx)) {
    selectedRows.value.delete(idx)
  } else {
    selectedRows.value.add(idx)
  }
}

function toggleAll() {
  if (selectedRows.value.size === sortedData.value.length) {
    selectedRows.value.clear()
  } else {
    selectedRows.value = new Set(sortedData.value.map((_, i) => i))
  }
}
</script>

<template>
  <div class="overflow-x-auto rounded-lg border border-gray-200 bg-white">
    <LoadingSpinner v-if="loading" size="md" />
    <div
      v-else-if="data.length === 0"
      class="py-12 text-center text-sm text-gray-500"
      role="status"
    >
      {{ emptyMessage ?? 'No data available' }}
    </div>
    <table v-else class="min-w-full">
      <thead>
        <tr class="border-b border-gray-200">
          <!-- Checkbox header -->
          <th v-if="selectable" class="w-10 px-3 py-3">
            <input
              type="checkbox"
              class="h-4 w-4 rounded border-gray-300 text-gray-600"
              :checked="
                selectedRows.size === sortedData.length && sortedData.length > 0
              "
              @change="toggleAll"
            />
          </th>
          <th
            v-for="col in columns"
            :key="col.key"
            class="px-4 py-3 text-left text-xs font-medium text-gray-500"
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
            <span class="inline-flex items-center gap-1">
              {{ col.label }}
              <svg
                v-if="getSortIcon(col)"
                class="h-3 w-3 text-gray-400"
                :class="{ 'text-gray-700': sortKey === col.key }"
                fill="currentColor"
                viewBox="0 0 20 20"
              >
                <path
                  v-if="getSortIcon(col) === 'asc'"
                  d="M14.707 12.707a1 1 0 01-1.414 0L10 9.414l-3.293 3.293a1 1 0 01-1.414-1.414l4-4a1 1 0 011.414 0l4 4a1 1 0 010 1.414z"
                />
                <path
                  v-else-if="getSortIcon(col) === 'desc'"
                  d="M5.293 7.293a1 1 0 011.414 0L10 10.586l3.293-3.293a1 1 0 111.414 1.414l-4 4a1 1 0 01-1.414 0l-4-4a1 1 0 010-1.414z"
                />
                <path
                  v-else
                  d="M10 3a.75.75 0 01.55.24l3.25 3.5a.75.75 0 11-1.1 1.02L10 4.852 7.3 7.76a.75.75 0 01-1.1-1.02l3.25-3.5A.75.75 0 0110 3zm-3.76 9.2a.75.75 0 011.06.04l2.7 2.908 2.7-2.908a.75.75 0 111.1 1.02l-3.25 3.5a.75.75 0 01-1.1 0l-3.25-3.5a.75.75 0 01.04-1.06z"
                />
              </svg>
            </span>
          </th>
        </tr>
      </thead>
      <tbody>
        <tr
          v-for="(row, idx) in sortedData"
          :key="idx"
          class="border-b border-gray-100 transition-colors hover:bg-gray-50"
          :class="{ 'bg-blue-50/50': selectedRows.has(idx) }"
        >
          <td v-if="selectable" class="w-10 px-3 py-3">
            <input
              type="checkbox"
              class="h-4 w-4 rounded border-gray-300 text-gray-600"
              :checked="selectedRows.has(idx)"
              @change="toggleRow(idx)"
            />
          </td>
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
