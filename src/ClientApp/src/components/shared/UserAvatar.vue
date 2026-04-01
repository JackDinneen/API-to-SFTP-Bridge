<script setup lang="ts">
import { computed } from 'vue'

const props = withDefaults(
  defineProps<{
    name: string
    size?: 'sm' | 'md' | 'lg'
  }>(),
  { size: 'md' },
)

const initials = computed(() => {
  const parts = props.name.trim().split(/\s+/)
  if (parts.length >= 2) {
    return (parts[0][0] + parts[parts.length - 1][0]).toUpperCase()
  }
  return props.name.slice(0, 2).toUpperCase()
})

const sizeClasses: Record<string, string> = {
  sm: 'h-7 w-7 text-xs',
  md: 'h-8 w-8 text-xs',
  lg: 'h-10 w-10 text-sm',
}
</script>

<template>
  <div
    :class="sizeClasses[props.size]"
    class="inline-flex items-center justify-center rounded-full font-semibold text-white"
    style="background-color: hsl(var(--navy-dark))"
    :aria-label="`User: ${props.name}`"
    role="img"
  >
    {{ initials }}
  </div>
</template>
