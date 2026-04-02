<script setup lang="ts">
import { ref, watch, computed } from 'vue'
import { useWizardStore, type EndpointConfig } from '@/stores/wizard'
import { AuthType } from '@/types'
import { useApi } from '@/composables/useApi'
import JsonTreeViewer from '@/components/shared/JsonTreeViewer.vue'

const wizard = useWizardStore()
const api = useApi()
const config = ref<EndpointConfig>({ ...wizard.wizardData.endpointConfig })
const fetchStatus = ref<'idle' | 'loading' | 'success' | 'error'>('idle')
const fetchError = ref('')
const selectedPath = ref('')

const paginationOptions = [
  { value: 'none', label: 'None' },
  { value: 'offset', label: 'Offset' },
  { value: 'cursor', label: 'Cursor' },
  { value: 'page', label: 'Page Number' },
] as const

function buildCredentials() {
  const a = wizard.wizardData.apiConfig
  const creds: Record<string, unknown> = {}
  switch (a.authType) {
    case AuthType.ApiKey:
      creds.apiKey = a.apiKey
      creds.apiKeyHeader = a.apiKeyHeader
      break
    case AuthType.BasicAuth:
      creds.basicUsername = a.basicUsername
      creds.basicPassword = a.basicPassword
      break
    case AuthType.OAuth2ClientCredentials:
      creds.oAuthClientId = a.oauthClientId
      creds.oAuthClientSecret = a.oauthClientSecret
      creds.oAuthTokenUrl = a.oauthTokenUrl
      break
    case AuthType.CustomHeaders:
      creds.customHeaders = a.customHeaders
        .filter((h) => h.key.trim())
        .map((h) => ({ key: h.key, value: h.value }))
      break
  }
  return creds
}

async function fetchSample() {
  fetchStatus.value = 'loading'
  fetchError.value = ''
  try {
    const result = await api.postAsync<unknown>('/connections/preview/fetch-sample', {
      baseUrl: wizard.wizardData.apiConfig.baseUrl,
      endpointPath: config.value.path,
      authType: wizard.wizardData.apiConfig.authType,
      credentials: buildCredentials(),
      reportingLagDays: wizard.wizardData.outputConfig.reportingLagDays,
    })
    if (result.success && result.data) {
      config.value.sampleResponse = result.data
      fetchStatus.value = 'success'
    } else {
      fetchStatus.value = 'error'
      fetchError.value = result.message ?? 'Failed to fetch sample data'
    }
  } catch {
    fetchStatus.value = 'error'
    fetchError.value = 'Network error fetching sample'
  }
}

function onFieldSelect(path: string) {
  selectedPath.value = path
}

const isValid = computed(
  () => !!config.value.path.trim() && config.value.sampleResponse !== null,
)

watch(
  config,
  (val) => {
    wizard.setStepData('endpointConfig', { ...val })
    wizard.setStepValid(2, isValid.value)
  },
  { deep: true },
)

watch(isValid, (v) => wizard.setStepValid(2, v), { immediate: true })
</script>

<template>
  <div class="space-y-6">
    <h3 class="text-lg font-semibold text-gray-800">Endpoint Discovery</h3>

    <div>
      <label class="block text-sm font-medium text-gray-700 mb-1"
        >Endpoint Path</label
      >
      <input
        v-model="config.path"
        type="text"
        placeholder="/v1/buildings/{id}/consumption"
        class="w-full rounded-md border border-gray-300 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
      />
    </div>

    <div>
      <button
        type="button"
        class="inline-flex items-center gap-2 rounded-md bg-gray-800 px-4 py-2 text-sm font-medium text-white hover:bg-gray-700 disabled:opacity-50"
        :disabled="!config.path.trim() || fetchStatus === 'loading'"
        @click="fetchSample"
      >
        {{ fetchStatus === 'loading' ? 'Fetching...' : 'Fetch Sample' }}
      </button>
      <span
        v-if="fetchStatus === 'error'"
        class="ml-3 text-sm text-red-600 font-medium"
      >
        {{ fetchError }}
      </span>
    </div>

    <!-- JSON Tree Viewer -->
    <div
      v-if="config.sampleResponse"
      class="bg-white border border-gray-200 rounded-lg p-4 max-h-96 overflow-auto"
    >
      <h4 class="text-sm font-medium text-gray-700 mb-2">Response Structure</h4>
      <JsonTreeViewer :data="config.sampleResponse" @select="onFieldSelect" />
      <p v-if="selectedPath" class="mt-2 text-xs text-blue-600">
        Selected:
        <code class="bg-blue-50 px-1 py-0.5 rounded">{{ selectedPath }}</code>
      </p>
    </div>

    <!-- Pagination -->
    <div class="border-t border-gray-200 pt-4">
      <h4 class="text-sm font-medium text-gray-700 mb-3">
        Pagination Configuration
      </h4>
      <div>
        <label class="block text-sm font-medium text-gray-600 mb-1"
          >Strategy</label
        >
        <select
          v-model="config.paginationStrategy"
          class="w-full rounded-md border border-gray-300 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
        >
          <option
            v-for="opt in paginationOptions"
            :key="opt.value"
            :value="opt.value"
          >
            {{ opt.label }}
          </option>
        </select>
      </div>

      <div v-if="config.paginationStrategy !== 'none'" class="mt-3 space-y-3">
        <div>
          <label class="block text-sm font-medium text-gray-600 mb-1"
            >Page Size</label
          >
          <input
            v-model.number="config.pageSize"
            type="number"
            min="1"
            class="w-full rounded-md border border-gray-300 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
          />
        </div>

        <div v-if="config.paginationStrategy === 'offset'">
          <label class="block text-sm font-medium text-gray-600 mb-1"
            >Offset Parameter</label
          >
          <input
            v-model="config.offsetParam"
            type="text"
            placeholder="offset"
            class="w-full rounded-md border border-gray-300 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
          />
        </div>

        <div v-if="config.paginationStrategy === 'cursor'">
          <label class="block text-sm font-medium text-gray-600 mb-1"
            >Cursor Parameter</label
          >
          <input
            v-model="config.cursorParam"
            type="text"
            placeholder="cursor"
            class="w-full rounded-md border border-gray-300 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
          />
        </div>

        <div v-if="config.paginationStrategy === 'page'">
          <label class="block text-sm font-medium text-gray-600 mb-1"
            >Page Parameter</label
          >
          <input
            v-model="config.pageParam"
            type="text"
            placeholder="page"
            class="w-full rounded-md border border-gray-300 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
          />
        </div>
      </div>
    </div>
  </div>
</template>
