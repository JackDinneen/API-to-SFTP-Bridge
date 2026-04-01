<script setup lang="ts">
import { ref, computed } from 'vue'

const props = defineProps<{
  data: unknown
  path?: string
  depth?: number
}>()

const emit = defineEmits<{
  select: [path: string]
}>()

const collapsed = ref(false)
const currentDepth = computed(() => props.depth ?? 0)
const currentPath = computed(() => props.path ?? '')

function typeOf(value: unknown): string {
  if (value === null) return 'null'
  if (Array.isArray(value)) return 'array'
  return typeof value
}

function entries(value: unknown): [string, unknown][] {
  if (value && typeof value === 'object') {
    return Object.entries(value as Record<string, unknown>)
  }
  return []
}

function childPath(key: string): string {
  const parent = currentPath.value
  if (!parent) return key
  if (/^\d+$/.test(key)) return `${parent}[${key}]`
  return `${parent}.${key}`
}

function toggle() {
  collapsed.value = !collapsed.value
}

function selectPath(path: string) {
  emit('select', path)
}

const isObject = computed(() => {
  const t = typeOf(props.data)
  return t === 'object' || t === 'array'
})

const dataEntries = computed(() => entries(props.data))
const dataType = computed(() => typeOf(props.data))
const arrayLength = computed(() =>
  Array.isArray(props.data) ? props.data.length : 0,
)
</script>

<template>
  <div :class="['font-mono text-sm', currentDepth > 0 ? 'ml-4' : '']">
    <template v-if="isObject">
      <button
        type="button"
        class="flex items-center gap-1 hover:bg-blue-50 rounded px-1 -ml-1 text-left"
        @click="toggle"
      >
        <span class="text-gray-400 w-4 text-center">{{ collapsed ? '+' : '-' }}</span>
        <span class="text-purple-600">{{
          dataType === 'array' ? `Array(${arrayLength})` : 'Object'
        }}</span>
      </button>
      <div v-if="!collapsed" class="border-l border-gray-200 ml-2 pl-1">
        <div
          v-for="[key, val] in dataEntries"
          :key="key"
          class="flex items-start"
        >
          <button
            type="button"
            class="text-blue-700 font-semibold hover:underline cursor-pointer shrink-0 mt-0.5"
            :title="'Select ' + childPath(key)"
            @click.stop="selectPath(childPath(key))"
          >
            {{ key }}:
          </button>
          <template v-if="val && typeof val === 'object'">
            <JsonTreeViewer
              :data="val"
              :path="childPath(key)"
              :depth="currentDepth + 1"
              @select="selectPath"
            />
          </template>
          <template v-else>
            <span class="ml-1" :class="{
              'text-green-700': typeof val === 'string',
              'text-orange-600': typeof val === 'number',
              'text-red-600': typeof val === 'boolean',
              'text-gray-400': val === null,
            }">
              {{ typeof val === 'string' ? `"${val}"` : String(val) }}
            </span>
          </template>
        </div>
      </div>
    </template>
    <template v-else>
      <span :class="{
        'text-green-700': typeof data === 'string',
        'text-orange-600': typeof data === 'number',
        'text-red-600': typeof data === 'boolean',
        'text-gray-400': data === null,
      }">
        {{ typeof data === 'string' ? `"${data}"` : String(data) }}
      </span>
    </template>
  </div>
</template>
