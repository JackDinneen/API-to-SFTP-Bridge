<script setup lang="ts">
import { useRouter } from 'vue-router'

const router = useRouter()
</script>

<template>
  <div class="flex flex-col items-center justify-center py-24 text-center">
    <div
      class="flex h-16 w-16 items-center justify-center rounded-2xl"
      style="background-color: hsl(var(--navy-dark) / 0.1)"
    >
      <svg
        class="h-8 w-8"
        style="color: hsl(var(--navy-dark))"
        fill="none"
        stroke="currentColor"
        stroke-width="1.5"
        viewBox="0 0 24 24"
      >
        <path
          stroke-linecap="round"
          stroke-linejoin="round"
          d="M3 13.125C3 12.504 3.504 12 4.125 12h2.25c.621 0 1.125.504 1.125 1.125v6.75C7.5 20.496 6.996 21 6.375 21h-2.25A1.125 1.125 0 013 19.875v-6.75zM9.75 8.625c0-.621.504-1.125 1.125-1.125h2.25c.621 0 1.125.504 1.125 1.125v11.25c0 .621-.504 1.125-1.125 1.125h-2.25a1.125 1.125 0 01-1.125-1.125V8.625zM16.5 4.125c0-.621.504-1.125 1.125-1.125h2.25C20.496 3 21 3.504 21 4.125v15.75c0 .621-.504 1.125-1.125 1.125h-2.25a1.125 1.125 0 01-1.125-1.125V4.125z"
        />
      </svg>
    </div>

    <h2 class="mt-6 text-xl font-semibold text-gray-900">
      Analytics — Coming Soon
    </h2>
    <p class="mt-2 max-w-md text-sm text-gray-500">
      Sync performance trends, records delivered over time, data quality scores,
      and outlier detection history. Track how your integrations are performing
      across all connections.
    </p>

    <button
      class="mt-6 rounded-md border border-gray-300 px-4 py-2 text-sm font-medium text-gray-700 hover:bg-gray-50"
      @click="router.push({ name: 'dashboard' })"
    >
      Back to Dashboard
    </button>
  </div>
</template>
