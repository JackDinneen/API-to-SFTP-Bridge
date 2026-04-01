<script setup lang="ts">
import { computed } from 'vue'
import { RouterLink, useRoute } from 'vue-router'

const route = useRoute()

const navItems = [
  {
    to: '/dashboard',
    name: 'dashboard',
    label: 'Dashboard',
    icon: 'M3 12l2-2m0 0l7-7 7 7M5 10v10a1 1 0 001 1h3m10-11l2 2m-2-2v10a1 1 0 01-1 1h-3m-6 0a1 1 0 001-1v-4a1 1 0 011-1h2a1 1 0 011 1v4a1 1 0 001 1m-6 0h6',
  },
  {
    to: '/analytics',
    name: 'analytics',
    label: 'Analytics',
    icon: 'M9 19v-6a2 2 0 00-2-2H5a2 2 0 00-2 2v6a2 2 0 002 2h2a2 2 0 002-2zm0 0V9a2 2 0 012-2h2a2 2 0 012 2v10m-6 0a2 2 0 002 2h2a2 2 0 002-2m0 0V5a2 2 0 012-2h2a2 2 0 012 2v14a2 2 0 01-2 2h-2a2 2 0 01-2-2z',
  },
  {
    to: '/connections/new',
    name: 'wizard',
    label: 'New Connection',
    icon: 'M13.828 10.172a4 4 0 00-5.656 0l-4 4a4 4 0 105.656 5.656l1.102-1.101m-.758-4.899a4 4 0 005.656 0l4-4a4 4 0 00-5.656-5.656l-1.1 1.1',
  },
  {
    to: '/settings',
    name: 'settings',
    label: 'Settings',
    icon: 'M10.325 4.317c.426-1.756 2.924-1.756 3.35 0a1.724 1.724 0 002.573 1.066c1.543-.94 3.31.826 2.37 2.37a1.724 1.724 0 001.066 2.573c1.756.426 1.756 2.924 0 3.35a1.724 1.724 0 00-1.066 2.573c.94 1.543-.826 3.31-2.37 2.37a1.724 1.724 0 00-2.573 1.066c-.426 1.756-2.924 1.756-3.35 0a1.724 1.724 0 00-2.573-1.066c-1.543.94-3.31-.826-2.37-2.37a1.724 1.724 0 00-1.066-2.573c-1.756-.426-1.756-2.924 0-3.35a1.724 1.724 0 001.066-2.573c-.94-1.543.826-3.31 2.37-2.37.996.608 2.296.07 2.572-1.065z M15 12a3 3 0 11-6 0 3 3 0 016 0z',
  },
]

const currentRouteName = computed(() => route.name)
</script>

<template>
  <aside
    class="fixed left-0 top-0 z-40 flex h-screen w-16 flex-col items-center py-4"
    style="background-color: var(--obi-sidebar)"
  >
    <!-- Obi Logo -->
    <RouterLink
      to="/dashboard"
      class="mb-8 flex items-center justify-center"
      aria-label="Obi Bridge home"
    >
      <span class="text-lg font-bold tracking-wide text-white">obi</span>
    </RouterLink>

    <!-- Navigation Icons -->
    <nav
      class="flex flex-1 flex-col items-center gap-2"
      aria-label="Main navigation"
    >
      <RouterLink
        v-for="item in navItems"
        :key="item.name"
        :to="item.to"
        :aria-label="item.label"
        class="group relative flex h-10 w-10 items-center justify-center rounded-lg transition-colors"
        :class="
          currentRouteName === item.name
            ? 'text-white'
            : 'text-[var(--obi-sidebar-text)] hover:text-white'
        "
        :style="
          currentRouteName === item.name
            ? 'background-color: var(--obi-sidebar-active)'
            : ''
        "
      >
        <svg
          class="h-5 w-5"
          fill="none"
          stroke="currentColor"
          stroke-width="1.5"
          viewBox="0 0 24 24"
          aria-hidden="true"
        >
          <path stroke-linecap="round" stroke-linejoin="round" :d="item.icon" />
        </svg>
        <!-- Tooltip -->
        <span
          class="pointer-events-none absolute left-full ml-3 whitespace-nowrap rounded-md px-2 py-1 text-xs font-medium text-white opacity-0 shadow-lg transition-opacity group-hover:opacity-100"
          style="background-color: var(--obi-sidebar-hover)"
        >
          {{ item.label }}
        </span>
      </RouterLink>
    </nav>

    <!-- Bottom: Help Icon -->
    <div class="mt-auto flex flex-col items-center gap-3 pb-2">
      <button
        class="flex h-10 w-10 items-center justify-center rounded-lg transition-colors"
        style="color: var(--obi-sidebar-text)"
        aria-label="Help"
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
            d="M9.879 7.519c1.171-1.025 3.071-1.025 4.242 0 1.172 1.025 1.172 2.687 0 3.712-.203.179-.43.326-.67.442-.745.361-1.45.999-1.45 1.827v.75M21 12a9 9 0 11-18 0 9 9 0 0118 0zm-9 5.25h.008v.008H12v-.008z"
          />
        </svg>
      </button>
    </div>
  </aside>
</template>
