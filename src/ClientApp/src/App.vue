<script setup lang="ts">
import { RouterLink, RouterView, useRoute } from 'vue-router'
import { computed } from 'vue'

const route = useRoute()

const navItems = [
  { to: '/dashboard', label: 'Dashboard', name: 'dashboard' },
  { to: '/connections/new', label: 'New Connection', name: 'wizard' },
]

const currentRouteName = computed(() => route.name)
</script>

<template>
  <div class="min-h-screen bg-gray-50">
    <header class="bg-white shadow-sm border-b border-gray-200">
      <div
        class="max-w-7xl mx-auto px-4 py-0 flex items-center justify-between"
      >
        <div class="flex items-center gap-8">
          <RouterLink to="/dashboard" class="flex items-center py-4">
            <h1 class="text-xl font-bold text-gray-900">Obi Bridge</h1>
          </RouterLink>
          <nav class="flex gap-1" aria-label="Main navigation">
            <RouterLink
              v-for="item in navItems"
              :key="item.name"
              :to="item.to"
              class="rounded-md px-3 py-2 text-sm font-medium transition-colors"
              :class="
                currentRouteName === item.name
                  ? 'bg-indigo-50 text-indigo-700'
                  : 'text-gray-600 hover:bg-gray-100 hover:text-gray-900'
              "
            >
              {{ item.label }}
            </RouterLink>
          </nav>
        </div>
      </div>
    </header>
    <main class="max-w-7xl mx-auto px-4 py-8">
      <RouterView />
    </main>
  </div>
</template>
