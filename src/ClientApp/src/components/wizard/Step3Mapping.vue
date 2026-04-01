<script setup lang="ts">
import { ref, watch, computed, onMounted } from 'vue'
import { useWizardStore, OBI_COLUMNS, type FieldMapping } from '@/stores/wizard'
import FieldMappingRow from './FieldMappingRow.vue'

const wizard = useWizardStore()

const AUTO_DETECT_PATTERNS: Record<string, RegExp[]> = {
  'Asset ID': [/building_code/i, /asset_id/i, /building_id/i, /site_code/i],
  'Asset name': [
    /building_name/i,
    /asset_name/i,
    /site_name/i,
    /property_name/i,
  ],
  'Submeter Code': [/meter_id/i, /submeter/i, /meter_code/i, /meter_number/i],
  'Utility Type': [
    /utility_type/i,
    /utility/i,
    /energy_type/i,
    /resource_type/i,
  ],
  Year: [/year/i],
  Month: [/month/i],
  Value: [/kwh/i, /value/i, /consumption/i, /usage/i, /reading/i, /amount/i],
}

function extractFieldPaths(obj: unknown, prefix = ''): string[] {
  const paths: string[] = []
  if (obj && typeof obj === 'object') {
    for (const [key, val] of Object.entries(obj as Record<string, unknown>)) {
      const path = prefix ? `${prefix}.${key}` : key
      if (Array.isArray(val) && val.length > 0 && typeof val[0] === 'object') {
        paths.push(...extractFieldPaths(val[0], `${path}[0]`))
      } else if (val && typeof val === 'object' && !Array.isArray(val)) {
        paths.push(...extractFieldPaths(val, path))
      } else {
        paths.push(path)
      }
    }
  }
  return paths
}

const sourceFields = computed(() => {
  const sample = wizard.wizardData.endpointConfig.sampleResponse
  return sample ? extractFieldPaths(sample) : []
})

function autoDetect(targetColumn: string): string | null {
  const patterns = AUTO_DETECT_PATTERNS[targetColumn]
  if (!patterns) return null
  for (const field of sourceFields.value) {
    const leaf = field.split('.').pop() ?? field
    for (const pattern of patterns) {
      if (pattern.test(leaf)) return field
    }
  }
  return null
}

const mappings = ref<FieldMapping[]>(
  wizard.wizardData.mappings.length > 0
    ? [...wizard.wizardData.mappings]
    : OBI_COLUMNS.map((col) => ({
        targetColumn: col,
        sourcePath: '',
        transformType: 'Direct',
        transformConfig: {},
      })),
)

const isValid = computed(() =>
  mappings.value.every(
    (m) => m.sourcePath.trim() || m.transformType === 'Static Value',
  ),
)

watch(
  mappings,
  (val) => {
    wizard.setStepData(
      'mappings',
      val.map((m) => ({ ...m })),
    )
    wizard.setStepValid(3, isValid.value)
  },
  { deep: true },
)

onMounted(() => {
  wizard.setStepValid(3, isValid.value)
})
</script>

<template>
  <div class="space-y-6">
    <h3 class="text-lg font-semibold text-gray-800">Field Mapping</h3>
    <p class="text-sm text-gray-600">
      Map source API fields to Obi's required columns. Fields marked with
      <span
        class="bg-yellow-400 text-yellow-900 text-xs font-bold px-1 py-0.5 rounded-full"
        >*</span
      >
      were auto-detected.
    </p>

    <div class="bg-white border border-gray-200 rounded-lg p-4">
      <div class="grid grid-cols-12 gap-3 pb-2 border-b border-gray-300 mb-1">
        <div class="col-span-3 text-xs font-semibold text-gray-500 uppercase">
          Target Column
        </div>
        <div class="col-span-4 text-xs font-semibold text-gray-500 uppercase">
          Source Field
        </div>
        <div class="col-span-2 text-xs font-semibold text-gray-500 uppercase">
          Transform
        </div>
        <div class="col-span-3 text-xs font-semibold text-gray-500 uppercase">
          Config
        </div>
      </div>

      <FieldMappingRow
        v-for="(mapping, idx) in mappings"
        :key="mapping.targetColumn"
        :target-column="mapping.targetColumn"
        :source-fields="sourceFields"
        :auto-detect-match="autoDetect(mapping.targetColumn)"
        :model-source-path="mapping.sourcePath"
        :model-transform-type="mapping.transformType"
        :model-transform-config="mapping.transformConfig"
        @update:model-source-path="mappings[idx].sourcePath = $event"
        @update:model-transform-type="mappings[idx].transformType = $event"
        @update:model-transform-config="mappings[idx].transformConfig = $event"
      />
    </div>
  </div>
</template>
