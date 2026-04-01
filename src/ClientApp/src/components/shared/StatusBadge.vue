<script setup lang="ts">
import { computed } from 'vue'
import { ConnectionStatus, SyncRunStatus } from '@/types'
import type { BadgeStatus } from '@/types'

const props = defineProps<{
  status: BadgeStatus
}>()

const config = computed(() => {
  switch (props.status) {
    case ConnectionStatus.Active:
    case SyncRunStatus.Succeeded:
      return {
        color: 'text-green-600',
        bg: 'bg-green-50',
        icon: 'M9 12.75L11.25 15 15 9.75M21 12a9 9 0 11-18 0 9 9 0 0118 0z',
      }
    case ConnectionStatus.Paused:
    case SyncRunStatus.Pending:
      return {
        color: 'text-amber-600',
        bg: 'bg-amber-50',
        icon: 'M12 6v6h4.5m4.5 0a9 9 0 11-18 0 9 9 0 0118 0z',
      }
    case ConnectionStatus.Error:
    case SyncRunStatus.Failed:
      return {
        color: 'text-red-600',
        bg: 'bg-red-50',
        icon: 'M12 9v3.75m9-.75a9 9 0 11-18 0 9 9 0 0118 0zm-9 3.75h.008v.008H12v-.008z',
      }
    case SyncRunStatus.Running:
      return {
        color: 'text-blue-600',
        bg: 'bg-blue-50',
        icon: 'M16.023 9.348h4.992v-.001M2.985 19.644v-4.992m0 0h4.992m-4.993 0l3.181 3.183a8.25 8.25 0 0013.803-3.7M4.031 9.865a8.25 8.25 0 0113.803-3.7l3.181 3.182m0-4.991v4.99',
      }
    default:
      return {
        color: 'text-gray-600',
        bg: 'bg-gray-50',
        icon: 'M9.879 7.519c1.171-1.025 3.071-1.025 4.242 0 1.172 1.025 1.172 2.687 0 3.712',
      }
  }
})
</script>

<template>
  <span
    :class="[config.color, config.bg]"
    class="inline-flex items-center gap-1 rounded-md px-2 py-1 text-xs font-medium"
    role="status"
    :aria-label="`Status: ${props.status}`"
  >
    <svg
      class="h-3.5 w-3.5"
      fill="none"
      stroke="currentColor"
      stroke-width="1.5"
      viewBox="0 0 24 24"
    >
      <path stroke-linecap="round" stroke-linejoin="round" :d="config.icon" />
    </svg>
    {{ props.status }}
  </span>
</template>
