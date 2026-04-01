<script setup lang="ts">
import { computed } from 'vue'

const props = defineProps<{
  successRate: number
}>()

const colorClass = computed(() => {
  if (props.successRate >= 95) return 'bg-green-500'
  if (props.successRate >= 80) return 'bg-yellow-500'
  return 'bg-red-500'
})

const label = computed(() => {
  if (props.successRate >= 95) return 'Healthy'
  if (props.successRate >= 80) return 'Degraded'
  return 'Unhealthy'
})
</script>

<template>
  <div
    class="flex items-center gap-2"
    :aria-label="`Health: ${label} (${props.successRate}%)`"
  >
    <span
      :class="colorClass"
      class="inline-block h-3 w-3 rounded-full"
      role="img"
      :aria-label="label"
    />
    <span class="text-sm text-gray-600">{{ props.successRate }}%</span>
  </div>
</template>
