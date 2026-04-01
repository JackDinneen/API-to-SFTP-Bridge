<script setup lang="ts">
defineProps<{
  size?: 'sm' | 'md' | 'lg'
}>()
</script>

<template>
  <div
    class="flex items-center justify-center"
    role="status"
    aria-label="Loading"
  >
    <svg
      :class="{
        'h-5 w-5': size === 'sm' || !size,
        'h-8 w-8': size === 'md',
        'h-12 w-12': size === 'lg',
      }"
      class="animate-spin text-indigo-600"
      xmlns="http://www.w3.org/2000/svg"
      fill="none"
      viewBox="0 0 24 24"
    >
      <circle
        class="opacity-25"
        cx="12"
        cy="12"
        r="10"
        stroke="currentColor"
        stroke-width="4"
      />
      <path
        class="opacity-75"
        fill="currentColor"
        d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4z"
      />
    </svg>
    <span class="sr-only">Loading...</span>
  </div>
</template>
