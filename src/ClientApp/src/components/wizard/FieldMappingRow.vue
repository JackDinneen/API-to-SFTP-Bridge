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

// Static Value config (backend key: "value")
const staticValue = ref(
  (props.modelTransformConfig?.value as string) ?? '',
)

// Date Parse config (backend key: "outputField")
const dateOutputField = ref(
  (props.modelTransformConfig?.outputField as string) ?? 'year',
)

// Unit Conversion config (backend keys: "fromUnit", "toUnit")
const fromUnit = ref(
  (props.modelTransformConfig?.fromUnit as string) ?? '',
)
const toUnit = ref((props.modelTransformConfig?.toUnit as string) ?? '')

// Value Mapping config (backend key: "mappings")
const valueMappingEntries = ref<{ from: string; to: string }[]>(
  initValueMappingEntries(),
)

function initValueMappingEntries(): { from: string; to: string }[] {
  const mappings = props.modelTransformConfig?.mappings
  if (mappings && typeof mappings === 'object') {
    return Object.entries(mappings as Record<string, string>).map(
      ([from, to]) => ({ from, to }),
    )
  }
  return [{ from: '', to: '' }]
}

function addValueMapping() {
  valueMappingEntries.value.push({ from: '', to: '' })
}

function removeValueMapping(index: number) {
  valueMappingEntries.value.splice(index, 1)
  if (valueMappingEntries.value.length === 0) {
    valueMappingEntries.value.push({ from: '', to: '' })
  }
}

// Split config (backend keys: "delimiter", "index")
const splitDelimiter = ref(
  (props.modelTransformConfig?.delimiter as string) ?? '-',
)
const splitIndex = ref(
  (props.modelTransformConfig?.index as number) ?? 0,
)

const isAutoDetected = computed(
  () => !!props.autoDetectMatch && sourcePath.value === props.autoDetectMatch,
)

watch(sourcePath, (v) => emit('update:modelSourcePath', v), { immediate: true })
watch(transformType, (v) => emit('update:modelTransformType', v))

// Emit config with correct backend key names
watch(
  [
    transformType,
    staticValue,
    dateOutputField,
    fromUnit,
    toUnit,
    valueMappingEntries,
    splitDelimiter,
    splitIndex,
  ],
  () => {
    const config: Record<string, unknown> = {}
    switch (transformType.value) {
      case 'Static Value':
        config.value = staticValue.value
        break
      case 'Date Parse':
        config.outputField = dateOutputField.value
        break
      case 'Unit Conversion':
        config.fromUnit = fromUnit.value
        config.toUnit = toUnit.value
        break
      case 'Value Mapping': {
        const mappings: Record<string, string> = {}
        for (const entry of valueMappingEntries.value) {
          if (entry.from.trim()) {
            mappings[entry.from.trim()] = entry.to.trim()
          }
        }
        if (Object.keys(mappings).length > 0) {
          config.mappings = mappings
        }
        break
      }
      case 'Split':
        config.delimiter = splitDelimiter.value
        config.index = splitIndex.value
        break
    }
    emit('update:modelTransformConfig', config)
  },
  { deep: true },
)
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
      <!-- Static Value -->
      <input
        v-if="transformType === 'Static Value'"
        v-model="staticValue"
        type="text"
        placeholder="Static value"
        class="w-full rounded-md border border-gray-300 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
      />

      <!-- Date Parse: output field selector -->
      <select
        v-else-if="transformType === 'Date Parse'"
        v-model="dateOutputField"
        class="w-full rounded-md border border-gray-300 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
      >
        <option value="year">Extract Year</option>
        <option value="month">Extract Month</option>
      </select>

      <!-- Unit Conversion -->
      <div v-else-if="transformType === 'Unit Conversion'" class="flex gap-1">
        <input
          v-model="fromUnit"
          type="text"
          placeholder="From"
          class="w-1/2 rounded-md border border-gray-300 px-2 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
        />
        <input
          v-model="toUnit"
          type="text"
          placeholder="To"
          class="w-1/2 rounded-md border border-gray-300 px-2 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
        />
      </div>

      <!-- Value Mapping -->
      <div v-else-if="transformType === 'Value Mapping'" class="space-y-1">
        <div
          v-for="(entry, idx) in valueMappingEntries"
          :key="idx"
          class="flex gap-1 items-center"
        >
          <input
            v-model="entry.from"
            type="text"
            placeholder="Source"
            class="w-2/5 rounded-md border border-gray-300 px-2 py-1 text-xs focus:outline-none focus:ring-2 focus:ring-blue-500"
          />
          <span class="text-gray-400 text-xs">→</span>
          <input
            v-model="entry.to"
            type="text"
            placeholder="Target"
            class="w-2/5 rounded-md border border-gray-300 px-2 py-1 text-xs focus:outline-none focus:ring-2 focus:ring-blue-500"
          />
          <button
            type="button"
            class="text-red-400 hover:text-red-600 text-xs"
            @click="removeValueMapping(idx)"
          >
            ×
          </button>
        </div>
        <button
          type="button"
          class="text-blue-600 hover:text-blue-800 text-xs"
          @click="addValueMapping"
        >
          + Add
        </button>
      </div>

      <!-- Split -->
      <div v-else-if="transformType === 'Split'" class="flex gap-1">
        <input
          v-model="splitDelimiter"
          type="text"
          placeholder="Delim"
          class="w-1/2 rounded-md border border-gray-300 px-2 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
        />
        <input
          v-model.number="splitIndex"
          type="number"
          placeholder="Idx"
          min="0"
          class="w-1/2 rounded-md border border-gray-300 px-2 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
        />
      </div>

      <!-- Direct / Concatenation -->
      <span v-else class="text-xs text-gray-400 py-2 block"
        >No config needed</span
      >
    </div>
  </div>
</template>
