<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { useWizardStore, OBI_COLUMNS, type TestResult } from '@/stores/wizard'
import { AuthType } from '@/types'
import { useRouter } from 'vue-router'

const wizard = useWizardStore()
const router = useRouter()

const testStatus = ref<'idle' | 'loading' | 'success' | 'error'>('idle')
const testError = ref('')
const activateStatus = ref<'idle' | 'loading' | 'success' | 'error'>('idle')
const testResult = ref<TestResult | null>(wizard.wizardData.testResult)

const authLabel = computed(() => {
  switch (wizard.wizardData.apiConfig.authType) {
    case AuthType.ApiKey:
      return 'API Key'
    case AuthType.OAuth2ClientCredentials:
      return 'OAuth 2.0'
    case AuthType.BasicAuth:
      return 'Basic Auth'
    case AuthType.CustomHeaders:
      return 'Custom Headers'
    default:
      return 'Unknown'
  }
})

const cronLabel = computed(() => {
  const o = wizard.wizardData.outputConfig
  switch (o.simpleFrequency) {
    case 'daily':
      return 'Daily at 2:00 AM'
    case 'weekly':
      return 'Weekly on Monday at 2:00 AM'
    case 'monthly':
      return `Monthly on day ${o.simpleDay} at 2:00 AM`
    default:
      return o.cronExpression
  }
})

const summaryItems = computed(() => [
  { label: 'Base URL', value: wizard.wizardData.apiConfig.baseUrl },
  { label: 'Auth Method', value: authLabel.value },
  { label: 'Endpoint', value: wizard.wizardData.endpointConfig.path },
  {
    label: 'Mapped Fields',
    value: `${wizard.wizardData.mappings.filter((m) => m.sourcePath).length} / ${OBI_COLUMNS.length}`,
  },
  {
    label: 'Aggregation',
    value: wizard.wizardData.aggregation.isSubMonthly
      ? wizard.wizardData.aggregation.method
      : 'None (monthly)',
  },
  { label: 'Client', value: wizard.wizardData.outputConfig.clientName },
  { label: 'Platform', value: wizard.wizardData.outputConfig.platformName },
  {
    label: 'SFTP Host',
    value: `${wizard.wizardData.outputConfig.sftpHost}:${wizard.wizardData.outputConfig.sftpPort}`,
  },
  { label: 'Schedule', value: cronLabel.value },
  {
    label: 'Reporting Lag',
    value: `${wizard.wizardData.outputConfig.reportingLagDays} days`,
  },
])

async function runTestSync() {
  testStatus.value = 'loading'
  testError.value = ''
  try {
    // TODO: Replace with actual API call
    await new Promise((resolve) => setTimeout(resolve, 1500))
    testResult.value = {
      success: true,
      csvPreview: [
        [...OBI_COLUMNS],
        [
          'BLD-100',
          'Main Office',
          'MTR-001',
          'electricity',
          '2025',
          '1',
          '15230.5',
        ],
        [
          'BLD-100',
          'Main Office',
          'MTR-001',
          'electricity',
          '2025',
          '2',
          '14890.2',
        ],
        ['BLD-200', 'Warehouse', 'MTR-005', 'gas', '2025', '1', '3200.0'],
      ],
      validationResults: [
        { row: 1, status: 'pass', message: 'All fields valid' },
        { row: 2, status: 'pass', message: 'All fields valid' },
        { row: 3, status: 'pass', message: 'All fields valid' },
      ],
    }
    wizard.setStepData('testResult', testResult.value)
    testStatus.value = 'success'
    wizard.setStepValid(6, true)
  } catch {
    testStatus.value = 'error'
    testError.value =
      'Test sync failed. Check your configuration and try again.'
    wizard.setStepValid(6, false)
  }
}

async function activateConnection() {
  activateStatus.value = 'loading'
  try {
    const success = await wizard.submitWizard()
    if (success) {
      activateStatus.value = 'success'
    } else {
      activateStatus.value = 'error'
    }
  } catch {
    activateStatus.value = 'error'
  }
}

function goToDashboard() {
  wizard.resetWizard()
  router.push('/dashboard')
}

onMounted(() => {
  wizard.setStepValid(6, testResult.value?.success === true)
})
</script>

<template>
  <div class="space-y-6">
    <h3 class="text-lg font-semibold text-gray-800">Test &amp; Activate</h3>

    <!-- Success State -->
    <template v-if="activateStatus === 'success'">
      <div
        class="border border-green-200 rounded-lg p-6 bg-green-50 text-center space-y-4"
      >
        <svg
          class="w-12 h-12 text-green-600 mx-auto"
          fill="currentColor"
          viewBox="0 0 20 20"
        >
          <path
            fill-rule="evenodd"
            d="M10 18a8 8 0 100-16 8 8 0 000 16zm3.707-9.293a1 1 0 00-1.414-1.414L9 10.586 7.707 9.293a1 1 0 00-1.414 1.414l2 2a1 1 0 001.414 0l4-4z"
            clip-rule="evenodd"
          />
        </svg>
        <h4 class="text-lg font-bold text-green-800">
          Connection Activated Successfully
        </h4>
        <p class="text-sm text-green-700">
          Your connection is now live and will sync on the configured schedule.
        </p>
        <button
          type="button"
          class="inline-flex items-center gap-2 rounded-md bg-green-700 px-4 py-2 text-sm font-medium text-white hover:bg-green-600"
          @click="goToDashboard"
        >
          Go to Dashboard
        </button>
      </div>
    </template>

    <template v-else>
      <!-- Configuration Summary -->
      <div class="border border-gray-200 rounded-lg p-4">
        <h4 class="text-sm font-semibold text-gray-700 mb-3">
          Configuration Summary
        </h4>
        <dl class="grid grid-cols-2 gap-x-4 gap-y-2">
          <template v-for="item in summaryItems" :key="item.label">
            <dt class="text-sm text-gray-500">{{ item.label }}</dt>
            <dd class="text-sm font-medium text-gray-800">{{ item.value }}</dd>
          </template>
        </dl>
      </div>

      <!-- Test Sync -->
      <div class="border border-gray-200 rounded-lg p-4">
        <h4 class="text-sm font-semibold text-gray-700 mb-3">Test Sync</h4>
        <p class="text-sm text-gray-600 mb-3">
          Run a test sync to verify your configuration produces valid output
          before activating.
        </p>
        <button
          type="button"
          class="inline-flex items-center gap-2 rounded-md bg-gray-800 px-4 py-2 text-sm font-medium text-white hover:bg-gray-700 disabled:opacity-50"
          :disabled="testStatus === 'loading'"
          @click="runTestSync"
        >
          {{ testStatus === 'loading' ? 'Running Test...' : 'Run Test Sync' }}
        </button>
        <span
          v-if="testStatus === 'error'"
          class="ml-3 text-sm text-red-600 font-medium"
        >
          {{ testError }}
        </span>
      </div>

      <!-- CSV Preview -->
      <div
        v-if="testResult?.success"
        class="border border-gray-200 rounded-lg p-4"
      >
        <h4 class="text-sm font-semibold text-gray-700 mb-3">CSV Preview</h4>
        <div class="overflow-x-auto">
          <table class="min-w-full text-sm">
            <thead>
              <tr class="bg-gray-50">
                <th
                  v-for="(header, i) in testResult.csvPreview[0]"
                  :key="i"
                  class="px-3 py-2 text-left text-xs font-semibold text-gray-600 uppercase"
                >
                  {{ header }}
                </th>
              </tr>
            </thead>
            <tbody>
              <tr
                v-for="(row, rIdx) in testResult.csvPreview.slice(1)"
                :key="rIdx"
                class="border-t border-gray-100"
              >
                <td
                  v-for="(cell, cIdx) in row"
                  :key="cIdx"
                  class="px-3 py-2 text-gray-700"
                >
                  {{ cell }}
                </td>
              </tr>
            </tbody>
          </table>
        </div>

        <!-- Validation Results -->
        <div class="mt-3 space-y-1">
          <div
            v-for="v in testResult.validationResults"
            :key="v.row"
            class="flex items-center gap-2 text-xs"
          >
            <span
              class="inline-block w-2 h-2 rounded-full"
              :class="{
                'bg-green-500': v.status === 'pass',
                'bg-yellow-500': v.status === 'warning',
                'bg-red-500': v.status === 'error',
              }"
            />
            <span class="text-gray-600">Row {{ v.row }}: {{ v.message }}</span>
          </div>
        </div>
      </div>

      <!-- Activate -->
      <div class="pt-2">
        <button
          type="button"
          class="inline-flex items-center gap-2 rounded-md bg-indigo-600 px-6 py-2.5 text-sm font-semibold text-white hover:bg-indigo-500 disabled:opacity-50 disabled:cursor-not-allowed"
          :disabled="testStatus !== 'success' || activateStatus === 'loading'"
          @click="activateConnection"
        >
          {{
            activateStatus === 'loading'
              ? 'Activating...'
              : 'Activate Connection'
          }}
        </button>
        <span
          v-if="activateStatus === 'error'"
          class="ml-3 text-sm text-red-600 font-medium"
        >
          Activation failed. Please try again.
        </span>
      </div>
    </template>
  </div>
</template>
