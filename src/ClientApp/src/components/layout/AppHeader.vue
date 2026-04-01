<script setup lang="ts">
import { useRoute, useRouter } from 'vue-router'
import { computed } from 'vue'
import UserAvatar from '@/components/shared/UserAvatar.vue'

const route = useRoute()
const router = useRouter()

const pageTitle = computed(() => {
  switch (route.name) {
    case 'dashboard':
      return 'Connections'
    case 'connection-detail':
      return 'Connection Details'
    case 'wizard':
      return 'New Connection'
    default:
      return 'Obi Bridge'
  }
})
</script>

<template>
  <header
    class="flex h-14 items-center justify-between border-b border-gray-200 bg-white px-6"
  >
    <!-- Left: Page title -->
    <h1 class="text-lg font-semibold text-gray-900">{{ pageTitle }}</h1>

    <!-- Right: Actions -->
    <div class="flex items-center gap-4">
      <!-- Add New button -->
      <button
        class="flex items-center gap-1.5 rounded-md px-3 py-1.5 text-sm font-medium text-white transition-colors"
        style="background-color: hsl(var(--navy-dark))"
        @click="router.push({ name: 'wizard' })"
      >
        <svg
          class="h-4 w-4"
          fill="none"
          stroke="currentColor"
          stroke-width="2"
          viewBox="0 0 24 24"
        >
          <path
            stroke-linecap="round"
            stroke-linejoin="round"
            d="M12 4.5v15m7.5-7.5h-15"
          />
        </svg>
        ADD NEW
      </button>

      <!-- Notification bell -->
      <button
        class="relative flex h-8 w-8 items-center justify-center rounded-full text-gray-500 hover:bg-gray-100 hover:text-gray-700"
        aria-label="Notifications"
      >
        <svg
          class="h-5 w-5"
          fill="none"
          stroke="currentColor"
          stroke-width="1.5"
          viewBox="0 0 24 24"
        >
          <path
            stroke-linecap="round"
            stroke-linejoin="round"
            d="M14.857 17.082a23.848 23.848 0 005.454-1.31A8.967 8.967 0 0118 9.75v-.7V9A6 6 0 006 9v.75a8.967 8.967 0 01-2.312 6.022c1.733.64 3.56 1.085 5.455 1.31m5.714 0a24.255 24.255 0 01-5.714 0m5.714 0a3 3 0 11-5.714 0"
          />
        </svg>
        <!-- Notification count badge -->
        <span
          class="absolute -right-0.5 -top-0.5 flex h-4 w-4 items-center justify-center rounded-full bg-red-500 text-[10px] font-bold text-white"
        >
          3
        </span>
      </button>

      <!-- User avatar -->
      <UserAvatar name="Dev Admin" size="md" />
    </div>
  </header>
</template>
