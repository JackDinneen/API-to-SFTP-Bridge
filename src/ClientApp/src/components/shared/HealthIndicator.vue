<script setup lang="ts">
import { computed } from 'vue'

const props = defineProps<{
  successRate: number
}>()

const segments = computed(() => {
  const rate = props.successRate
  if (rate >= 95)
    return [
      { width: 85, color: 'var(--obi-success)' },
      { width: 10, color: 'var(--obi-warning)' },
      { width: 5, color: 'var(--obi-danger)' },
    ]
  if (rate >= 80)
    return [
      { width: 50, color: 'var(--obi-success)' },
      { width: 35, color: 'var(--obi-warning)' },
      { width: 15, color: 'var(--obi-danger)' },
    ]
  return [
    { width: 20, color: 'var(--obi-success)' },
    { width: 20, color: 'var(--obi-warning)' },
    { width: 60, color: 'var(--obi-danger)' },
  ]
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
    <div class="flex h-1.5 w-16 overflow-hidden rounded-full bg-gray-100">
      <div
        v-for="(seg, idx) in segments"
        :key="idx"
        :style="{ width: seg.width + '%', backgroundColor: seg.color }"
        class="h-full"
      />
    </div>
    <span class="text-xs text-gray-500">{{ props.successRate }}%</span>
  </div>
</template>
