<script setup lang="ts">
import { useRoute } from 'vue-router'

const route = useRoute()
const connectionId = route.params.id as string
</script>

<template>
  <div>
    <h2 class="text-2xl font-bold text-gray-900 mb-6">Connection Detail</h2>
    <p class="text-gray-600">Viewing connection: {{ connectionId }}</p>
  </div>
</template>
