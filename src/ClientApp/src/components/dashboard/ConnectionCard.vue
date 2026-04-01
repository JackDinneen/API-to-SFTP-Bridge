<script setup lang="ts">
import type { Connection } from '@/types'
import { ConnectionStatus } from '@/types'
import StatusBadge from '@/components/shared/StatusBadge.vue'
import HealthIndicator from '@/components/shared/HealthIndicator.vue'

const props = defineProps<{
  connection: Connection
}>()

const emit = defineEmits<{
  syncNow: [id: string]
  togglePause: [id: string]
  viewDetails: [id: string]
}>()

function formatDate(dateStr: string | undefined): string {
  if (!dateStr) return 'Never'
  return new Date(dateStr).toLocaleString()
}
</script>

<template>
  <div
    class="rounded-lg border border-gray-200 bg-white p-5 shadow-sm hover:shadow-md transition-shadow"
  >
    <div class="flex items-start justify-between">
      <div class="min-w-0 flex-1">
        <h3 class="truncate text-base font-semibold text-gray-900">
          {{ props.connection.name }}
        </h3>
        <p class="mt-1 text-sm text-gray-500">
          {{ props.connection.clientName }} /
          {{ props.connection.platformName }}
        </p>
      </div>
      <StatusBadge :status="props.connection.status" />
    </div>

    <div class="mt-4 grid grid-cols-2 gap-3 text-sm">
      <div>
        <span class="text-gray-500">Last sync</span>
        <p class="font-medium text-gray-900">
          {{ formatDate(props.connection.lastSyncAt) }}
        </p>
      </div>
      <div>
        <span class="text-gray-500">Records</span>
        <p class="font-medium text-gray-900">
          {{ props.connection.lastSyncRecordCount ?? '-' }}
        </p>
      </div>
      <div>
        <span class="text-gray-500">Next sync</span>
        <p class="font-medium text-gray-900">
          {{ formatDate(props.connection.nextSyncAt) }}
        </p>
      </div>
      <div>
        <span class="text-gray-500">Health</span>
        <HealthIndicator :success-rate="props.connection.successRate ?? 100" />
      </div>
    </div>

    <div class="mt-4 flex gap-2 border-t border-gray-100 pt-3">
      <button
        class="rounded-md bg-indigo-50 px-3 py-1.5 text-xs font-medium text-indigo-700 hover:bg-indigo-100"
        aria-label="Sync now"
        @click="emit('syncNow', props.connection.id)"
      >
        Sync Now
      </button>
      <button
        class="rounded-md bg-gray-50 px-3 py-1.5 text-xs font-medium text-gray-700 hover:bg-gray-100"
        :aria-label="
          props.connection.status === ConnectionStatus.Paused
            ? 'Resume connection'
            : 'Pause connection'
        "
        @click="emit('togglePause', props.connection.id)"
      >
        {{
          props.connection.status === ConnectionStatus.Paused
            ? 'Resume'
            : 'Pause'
        }}
      </button>
      <button
        class="rounded-md bg-gray-50 px-3 py-1.5 text-xs font-medium text-gray-700 hover:bg-gray-100"
        aria-label="View details"
        @click="emit('viewDetails', props.connection.id)"
      >
        View Details
      </button>
    </div>
  </div>
</template>
