<script setup lang="ts">
import { ref } from 'vue'

const props = defineProps<{
  title: string
  count: number
  description: string
  color: string
  icon: string
}>()

const showTooltip = ref(false)
</script>

<template>
  <div class="rounded-lg border border-gray-200 bg-white px-5 py-4">
    <div class="flex items-center justify-between">
      <p class="text-sm font-medium text-gray-600">{{ props.title }}</p>
      <!-- Info icon with tooltip -->
      <div class="relative">
        <button
          class="flex h-5 w-5 items-center justify-center rounded-full text-gray-400 hover:bg-gray-100 hover:text-gray-600"
          aria-label="More info"
          @mouseenter="showTooltip = true"
          @mouseleave="showTooltip = false"
          @focus="showTooltip = true"
          @blur="showTooltip = false"
        >
          <svg
            class="h-4 w-4"
            fill="none"
            stroke="currentColor"
            stroke-width="1.5"
            viewBox="0 0 24 24"
          >
            <path
              stroke-linecap="round"
              stroke-linejoin="round"
              d="M11.25 11.25l.041-.02a.75.75 0 011.063.852l-.708 2.836a.75.75 0 001.063.853l.041-.021M21 12a9 9 0 11-18 0 9 9 0 0118 0zm-9-3.75h.008v.008H12V8.25z"
            />
          </svg>
        </button>
        <div
          v-show="showTooltip"
          class="absolute right-0 top-7 z-10 w-56 rounded-lg border border-gray-200 bg-white p-3 text-xs text-gray-600 shadow-lg"
        >
          {{ props.description }}
        </div>
      </div>
    </div>

    <div class="mt-2 flex items-baseline gap-2">
      <span class="text-2xl font-bold text-gray-900">{{ props.count }}</span>
    </div>

    <!-- Colored accent bar -->
    <div class="mt-3 h-1.5 w-full overflow-hidden rounded-full bg-gray-100">
      <div
        class="h-full rounded-full transition-all duration-500"
        :style="{
          width: props.count > 0 ? '100%' : '0%',
          backgroundColor: props.color,
        }"
      />
    </div>
  </div>
</template>
