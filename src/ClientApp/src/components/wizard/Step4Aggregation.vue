<script setup lang="ts">
import { ref, watch, computed, onMounted } from 'vue'
import { useWizardStore, type AggregationConfig } from '@/stores/wizard'

const wizard = useWizardStore()
const config = ref<AggregationConfig>({ ...wizard.wizardData.aggregation })

const aggregationMethods = [
  { value: 'sum', label: 'Sum', description: 'Add all values within each month' },
  { value: 'average', label: 'Average', description: 'Average of all values within each month' },
  { value: 'last', label: 'Last', description: 'Use the last recorded value in each month' },
  { value: 'max', label: 'Max', description: 'Use the maximum value in each month' },
] as const

const isValid = computed(() => {
  if (!config.value.isSubMonthly) return true
  return !!config.value.method
})

watch(
  config,
  (val) => {
    wizard.setStepData('aggregation', { ...val })
    wizard.setStepValid(4, isValid.value)
  },
  { deep: true },
)

onMounted(() => {
  wizard.setStepValid(4, isValid.value)
})
</script>

<template>
  <div class="space-y-6">
    <h3 class="text-lg font-semibold text-gray-800">Aggregation Settings</h3>
    <p class="text-sm text-gray-600">
      Obi requires monthly data. If your source provides sub-monthly readings (daily, hourly, etc.),
      configure how values should be aggregated into monthly totals.
    </p>

    <div>
      <label class="flex items-center gap-3 cursor-pointer">
        <input
          v-model="config.isSubMonthly"
          type="checkbox"
          class="h-4 w-4 rounded border-gray-300 text-indigo-600 focus:ring-indigo-500"
        />
        <span class="text-sm font-medium text-gray-700">
          Source data is sub-monthly (daily, hourly, or other granularity)
        </span>
      </label>
    </div>

    <template v-if="config.isSubMonthly">
      <div class="border border-gray-200 rounded-lg p-4 bg-gray-50">
        <h4 class="text-sm font-medium text-gray-700 mb-3">Aggregation Method</h4>
        <div class="space-y-2">
          <label
            v-for="method in aggregationMethods"
            :key="method.value"
            class="flex items-start gap-3 p-3 rounded-md cursor-pointer transition-colors"
            :class="config.method === method.value ? 'bg-indigo-50 border border-indigo-200' : 'hover:bg-white'"
          >
            <input
              v-model="config.method"
              type="radio"
              :value="method.value"
              class="mt-0.5 h-4 w-4 border-gray-300 text-indigo-600 focus:ring-indigo-500"
            />
            <div>
              <span class="text-sm font-semibold text-gray-800">{{ method.label }}</span>
              <p class="text-xs text-gray-500 mt-0.5">{{ method.description }}</p>
            </div>
          </label>
        </div>
      </div>
    </template>

    <template v-else>
      <div class="border border-green-200 rounded-lg p-4 bg-green-50">
        <div class="flex items-center gap-2">
          <svg class="w-5 h-5 text-green-600" fill="currentColor" viewBox="0 0 20 20">
            <path
              fill-rule="evenodd"
              d="M10 18a8 8 0 100-16 8 8 0 000 16zm3.707-9.293a1 1 0 00-1.414-1.414L9 10.586 7.707 9.293a1 1 0 00-1.414 1.414l2 2a1 1 0 001.414 0l4-4z"
              clip-rule="evenodd"
            />
          </svg>
          <span class="text-sm font-medium text-green-800">
            No aggregation needed -- source data is already at monthly granularity.
          </span>
        </div>
      </div>
    </template>
  </div>
</template>
