<script setup lang="ts">
import { RouterView } from 'vue-router'
import AppSidebar from '@/components/layout/AppSidebar.vue'
import AppHeader from '@/components/layout/AppHeader.vue'
</script>

<template>
  <div class="flex min-h-screen" style="background-color: var(--obi-body-bg)">
    <AppSidebar />
    <div class="ml-16 flex flex-1 flex-col">
      <AppHeader />
      <main class="flex-1 px-6 py-6">
        <RouterView />
      </main>
    </div>
  </div>
</template>
