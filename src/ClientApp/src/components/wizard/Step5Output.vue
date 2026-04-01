<script setup lang="ts">
import { ref, watch, computed, onMounted } from 'vue'
import { useWizardStore, type OutputConfig } from '@/stores/wizard'

const wizard = useWizardStore()
const config = ref<OutputConfig>({ ...wizard.wizardData.outputConfig })

const frequencyOptions = [
  { value: 'daily', label: 'Daily' },
  { value: 'weekly', label: 'Weekly' },
  { value: 'monthly', label: 'Monthly' },
] as const

const cronPreview = computed(() => {
  switch (config.value.simpleFrequency) {
    case 'daily':
      return '0 2 * * *'
    case 'weekly':
      return '0 2 * * 1'
    case 'monthly':
      return `0 2 ${config.value.simpleDay} * *`
    default:
      return '0 2 * * *'
  }
})

const fileNamePreview = computed(() => {
  const client = config.value.clientName.trim() || 'ClientName'
  const platform = config.value.platformName.trim() || 'PlatformName'
  const safe = (s: string) =>
    s.replace(/\s+/g, '_').replace(/[^a-zA-Z0-9_-]/g, '')
  return `${safe(client)}_${safe(platform)}_2026-03.csv`
})

const isValid = computed(() => {
  return !!(
    config.value.clientName.trim() &&
    config.value.platformName.trim() &&
    config.value.sftpHost.trim() &&
    config.value.sftpPort > 0 &&
    config.value.sftpUsername.trim() &&
    config.value.sftpPath.trim()
  )
})

watch(
  config,
  (val) => {
    if (val.scheduleMode === 'simple') {
      val.cronExpression = cronPreview.value
    }
    wizard.setStepData('outputConfig', { ...val })
    wizard.setStepValid(5, isValid.value)
  },
  { deep: true },
)

onMounted(() => {
  wizard.setStepValid(5, isValid.value)
})
</script>

<template>
  <div class="space-y-6">
    <h3 class="text-lg font-semibold text-gray-800">Output Configuration</h3>

    <!-- Client / Platform -->
    <div class="grid grid-cols-2 gap-4">
      <div>
        <label class="block text-sm font-medium text-gray-700 mb-1"
          >Client Name</label
        >
        <input
          v-model="config.clientName"
          type="text"
          placeholder="Acme Corp"
          class="w-full rounded-md border border-gray-300 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-indigo-500"
        />
      </div>
      <div>
        <label class="block text-sm font-medium text-gray-700 mb-1"
          >Platform Name</label
        >
        <input
          v-model="config.platformName"
          type="text"
          placeholder="EnergyManager Pro"
          class="w-full rounded-md border border-gray-300 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-indigo-500"
        />
      </div>
    </div>

    <!-- SFTP Configuration -->
    <div class="border border-gray-200 rounded-lg p-4">
      <h4 class="text-sm font-semibold text-gray-700 mb-3">SFTP Destination</h4>
      <div class="grid grid-cols-2 gap-4">
        <div>
          <label class="block text-sm font-medium text-gray-600 mb-1"
            >Host</label
          >
          <input
            v-model="config.sftpHost"
            type="text"
            placeholder="sftp.example.com"
            class="w-full rounded-md border border-gray-300 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-indigo-500"
          />
        </div>
        <div>
          <label class="block text-sm font-medium text-gray-600 mb-1"
            >Port</label
          >
          <input
            v-model.number="config.sftpPort"
            type="number"
            min="1"
            max="65535"
            class="w-full rounded-md border border-gray-300 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-indigo-500"
          />
        </div>
        <div>
          <label class="block text-sm font-medium text-gray-600 mb-1"
            >Username</label
          >
          <input
            v-model="config.sftpUsername"
            type="text"
            placeholder="sftp_user"
            class="w-full rounded-md border border-gray-300 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-indigo-500"
          />
        </div>
        <div>
          <label class="block text-sm font-medium text-gray-600 mb-1"
            >Password</label
          >
          <input
            v-model="config.sftpPassword"
            type="password"
            placeholder="Password"
            class="w-full rounded-md border border-gray-300 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-indigo-500"
          />
        </div>
        <div class="col-span-2">
          <label class="block text-sm font-medium text-gray-600 mb-1"
            >Upload Path</label
          >
          <input
            v-model="config.sftpPath"
            type="text"
            placeholder="/upload"
            class="w-full rounded-md border border-gray-300 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-indigo-500"
          />
        </div>
      </div>
    </div>

    <!-- Schedule -->
    <div class="border border-gray-200 rounded-lg p-4">
      <h4 class="text-sm font-semibold text-gray-700 mb-3">Sync Schedule</h4>
      <div class="space-y-3">
        <div>
          <label class="block text-sm font-medium text-gray-600 mb-1"
            >Frequency</label
          >
          <select
            v-model="config.simpleFrequency"
            class="w-full rounded-md border border-gray-300 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-indigo-500"
          >
            <option
              v-for="opt in frequencyOptions"
              :key="opt.value"
              :value="opt.value"
            >
              {{ opt.label }}
            </option>
          </select>
        </div>

        <div v-if="config.simpleFrequency === 'monthly'">
          <label class="block text-sm font-medium text-gray-600 mb-1"
            >Day of Month</label
          >
          <input
            v-model.number="config.simpleDay"
            type="number"
            min="1"
            max="28"
            class="w-full rounded-md border border-gray-300 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-indigo-500"
          />
        </div>

        <div class="bg-gray-50 rounded-md p-3">
          <span class="text-xs font-medium text-gray-500"
            >Cron Expression:
          </span>
          <code class="text-sm text-indigo-700 font-mono">{{
            cronPreview
          }}</code>
        </div>
      </div>
    </div>

    <!-- Reporting Lag -->
    <div>
      <label class="block text-sm font-medium text-gray-700 mb-1"
        >Reporting Lag (days)</label
      >
      <input
        v-model.number="config.reportingLagDays"
        type="number"
        min="0"
        max="365"
        class="w-48 rounded-md border border-gray-300 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-indigo-500"
      />
      <p class="text-xs text-gray-500 mt-1">
        How many days behind the current date the data is expected to be
        available.
      </p>
    </div>

    <!-- File Name Preview -->
    <div class="bg-gray-50 border border-gray-200 rounded-lg p-4">
      <span class="text-sm font-medium text-gray-700">File Name Preview: </span>
      <code class="text-sm text-indigo-700 font-mono">{{
        fileNamePreview
      }}</code>
    </div>
  </div>
</template>
