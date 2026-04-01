<script setup lang="ts">
import { ref, watch, computed } from 'vue'

const props = defineProps<{
  targetColumn: string
  sourceFields: string[]
  autoDetectMatch: string | null
  modelSourcePath: string
  modelTransformType: string
  modelTransformConfig: Record<string, unknown>
}>()

const emit = defineEmits<{
  'update:modelSourcePath': [value: string]
  'update:modelTransformType': [value: string]
  'update:modelTransformConfig': [value: Record<string, unknown>]
}>()

const TRANSFORM_TYPES = [
  'Direct',
  'Value Mapping',
  'Unit Conversion',
  'Date Parse',
  'Static Value',
  'Concatenation',
  'Split',
] as const

const sourcePath = ref(props.modelSourcePath || props.autoDetectMatch || '')
const transformType = ref(props.modelTransformType || 'Direct')
const staticValue = ref(
  (props.modelTransformConfig?.staticValue as string) ?? '',
)
const dateFormat = ref(
  (props.modelTransformConfig?.dateFormat as string) ?? 'YYYY-MM-DD',
)
const unitFrom = ref((props.modelTransformConfig?.unitFrom as string) ?? '')
const unitTo = ref((props.modelTransformConfig?.unitTo as string) ?? '')

const isAutoDetected = computed(
  () => !!props.autoDetectMatch && sourcePath.value === props.autoDetectMatch,
)

watch(sourcePath, (v) => emit('update:modelSourcePath', v))
watch(transformType, (v) => emit('update:modelTransformType', v))
watch([staticValue, dateFormat, unitFrom, unitTo], () => {
  const config: Record<string, unknown> = {}
  if (transformType.value === 'Static Value')
    config.staticValue = staticValue.value
  if (transformType.value === 'Date Parse') config.dateFormat = dateFormat.value
  if (transformType.value === 'Unit Conversion') {
    config.unitFrom = unitFrom.value
    config.unitTo = unitTo.value
  }
  emit('update:modelTransformConfig', config)
})
</script>

<template>
  <div
    class="grid grid-cols-12 gap-3 items-start py-3 border-b border-gray-100"
  >
    <div class="col-span-3">
      <span class="text-sm font-semibold text-gray-800">{{
        targetColumn
      }}</span>
    </div>
    <div class="col-span-4">
      <div class="relative">
        <select
          v-model="sourcePath"
          class="w-full rounded-md border border-gray-300 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
        >
          <option value="">-- Select field --</option>
          <option v-for="field in sourceFields" :key="field" :value="field">
            {{ field }}
          </option>
        </select>
        <span
          v-if="isAutoDetected"
          class="absolute -top-2 -right-2 bg-yellow-400 text-yellow-900 text-xs font-bold px-1.5 py-0.5 rounded-full"
          title="Auto-detected"
        >
          *
        </span>
      </div>
    </div>
    <div class="col-span-2">
      <select
        v-model="transformType"
        class="w-full rounded-md border border-gray-300 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
      >
        <option v-for="t in TRANSFORM_TYPES" :key="t" :value="t">
          {{ t }}
        </option>
      </select>
    </div>
    <div class="col-span-3">
      <input
        v-if="transformType === 'Static Value'"
        v-model="staticValue"
        type="text"
        placeholder="Static value"
        class="w-full rounded-md border border-gray-300 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
      />
      <input
        v-else-if="transformType === 'Date Parse'"
        v-model="dateFormat"
        type="text"
        placeholder="e.g. YYYY-MM-DD"
        class="w-full rounded-md border border-gray-300 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
      />
      <div v-else-if="transformType === 'Unit Conversion'" class="flex gap-1">
        <input
          v-model="unitFrom"
          type="text"
          placeholder="From"
          class="w-1/2 rounded-md border border-gray-300 px-2 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
        />
        <input
          v-model="unitTo"
          type="text"
          placeholder="To"
          class="w-1/2 rounded-md border border-gray-300 px-2 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
        />
      </div>
      <span v-else class="text-xs text-gray-400 py-2 block"
        >No config needed</span
      >
    </div>
  </div>
</template>
