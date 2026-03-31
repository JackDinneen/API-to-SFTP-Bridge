<script setup lang="ts">
import { useConnectionsStore } from '@/stores/connections'

const connectionsStore = useConnectionsStore()
</script>

<template>
  <div>
    <h2 class="text-2xl font-bold text-gray-900 mb-6">Dashboard</h2>
    <p class="text-gray-600">
      {{ connectionsStore.connections.length }} connections configured.
    </p>
  </div>
</template>
