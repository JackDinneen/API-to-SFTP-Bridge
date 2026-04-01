<script setup lang="ts">
const props = defineProps<{
  title: string
  count: number
  segments?: { color: string; value: number }[]
}>()
</script>

<template>
  <div class="rounded-lg border border-gray-200 bg-white px-5 py-4">
    <p class="text-sm font-medium text-gray-600">{{ props.title }}</p>
    <div class="mt-1 flex items-baseline gap-2">
      <span class="text-2xl font-bold text-gray-900">{{ props.count }}</span>
      <span class="text-sm text-gray-500">Open</span>
    </div>
    <!-- Multi-segment progress bar -->
    <div
      v-if="props.segments && props.segments.length > 0"
      class="mt-3 flex h-1.5 w-full overflow-hidden rounded-full bg-gray-100"
    >
      <div
        v-for="(seg, idx) in props.segments"
        :key="idx"
        :style="{ width: seg.value + '%', backgroundColor: seg.color }"
        class="h-full"
      />
    </div>
  </div>
</template>
