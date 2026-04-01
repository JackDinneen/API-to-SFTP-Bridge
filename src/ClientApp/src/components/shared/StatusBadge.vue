<script setup lang="ts">
import { computed } from 'vue'
import { ConnectionStatus, SyncRunStatus } from '@/types'
import type { BadgeStatus } from '@/types'

const props = defineProps<{
  status: BadgeStatus
}>()

const colorClasses = computed(() => {
  switch (props.status) {
    case ConnectionStatus.Active:
    case SyncRunStatus.Succeeded:
      return 'bg-green-100 text-green-800'
    case ConnectionStatus.Paused:
    case SyncRunStatus.Pending:
      return 'bg-yellow-100 text-yellow-800'
    case ConnectionStatus.Error:
    case SyncRunStatus.Failed:
      return 'bg-red-100 text-red-800'
    case SyncRunStatus.Running:
      return 'bg-blue-100 text-blue-800'
    default:
      return 'bg-gray-100 text-gray-800'
  }
})
</script>

<template>
  <span
    :class="colorClasses"
    class="inline-flex items-center rounded-full px-2.5 py-0.5 text-xs font-medium"
    role="status"
    :aria-label="`Status: ${props.status}`"
  >
    {{ props.status }}
  </span>
</template>
