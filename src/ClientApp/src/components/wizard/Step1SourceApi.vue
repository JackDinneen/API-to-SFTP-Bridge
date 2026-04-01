<script setup lang="ts">
import { ref, watch, computed } from 'vue'
import { useWizardStore, type ApiConfig } from '@/stores/wizard'
import { AuthType } from '@/types'

const wizard = useWizardStore()
const config = ref<ApiConfig>({ ...wizard.wizardData.apiConfig })
const testStatus = ref<'idle' | 'loading' | 'success' | 'error'>('idle')
const testMessage = ref('')

const authOptions = [
  { value: AuthType.ApiKey, label: 'API Key' },
  { value: AuthType.OAuth2ClientCredentials, label: 'OAuth 2.0' },
  { value: AuthType.BasicAuth, label: 'Basic Auth' },
  { value: AuthType.CustomHeaders, label: 'Custom Headers' },
]

function addHeader() {
  config.value.customHeaders.push({ key: '', value: '' })
}

function removeHeader(index: number) {
  config.value.customHeaders.splice(index, 1)
}

async function testConnection() {
  testStatus.value = 'loading'
  testMessage.value = ''
  try {
    // TODO: Replace with actual API call
    await new Promise((resolve) => setTimeout(resolve, 1000))
    testStatus.value = 'success'
    testMessage.value = 'Connection successful'
    config.value.connectionTested = true
  } catch {
    testStatus.value = 'error'
    testMessage.value = 'Connection failed'
    config.value.connectionTested = false
  }
}

const isValid = computed(() => {
  if (!config.value.baseUrl.trim()) return false
  switch (config.value.authType) {
    case AuthType.ApiKey:
      return !!(config.value.apiKey.trim() && config.value.apiKeyHeader.trim())
    case AuthType.OAuth2ClientCredentials:
      return !!(
        config.value.oauthClientId.trim() &&
        config.value.oauthClientSecret.trim() &&
        config.value.oauthTokenUrl.trim()
      )
    case AuthType.BasicAuth:
      return !!(config.value.basicUsername.trim() && config.value.basicPassword.trim())
    case AuthType.CustomHeaders:
      return config.value.customHeaders.some((h) => h.key.trim() && h.value.trim())
    default:
      return false
  }
})

watch(
  config,
  (val) => {
    wizard.setStepData('apiConfig', { ...val })
    wizard.setStepValid(1, isValid.value)
  },
  { deep: true },
)

watch(isValid, (v) => wizard.setStepValid(1, v), { immediate: true })
</script>

<template>
  <div class="space-y-6">
    <h3 class="text-lg font-semibold text-gray-800">Source API Configuration</h3>

    <div>
      <label class="block text-sm font-medium text-gray-700 mb-1">Base URL</label>
      <input
        v-model="config.baseUrl"
        type="url"
        placeholder="https://api.example.com"
        class="w-full rounded-md border border-gray-300 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
      />
    </div>

    <div>
      <label class="block text-sm font-medium text-gray-700 mb-1">Authentication Method</label>
      <select
        v-model="config.authType"
        class="w-full rounded-md border border-gray-300 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
      >
        <option v-for="opt in authOptions" :key="opt.value" :value="opt.value">
          {{ opt.label }}
        </option>
      </select>
    </div>

    <!-- API Key -->
    <template v-if="config.authType === AuthType.ApiKey">
      <div>
        <label class="block text-sm font-medium text-gray-700 mb-1">API Key</label>
        <input
          v-model="config.apiKey"
          type="password"
          placeholder="Enter API key"
          class="w-full rounded-md border border-gray-300 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
        />
      </div>
      <div>
        <label class="block text-sm font-medium text-gray-700 mb-1">Header Name</label>
        <input
          v-model="config.apiKeyHeader"
          type="text"
          placeholder="X-API-Key"
          class="w-full rounded-md border border-gray-300 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
        />
      </div>
    </template>

    <!-- OAuth 2.0 -->
    <template v-if="config.authType === AuthType.OAuth2ClientCredentials">
      <div>
        <label class="block text-sm font-medium text-gray-700 mb-1">Client ID</label>
        <input
          v-model="config.oauthClientId"
          type="text"
          placeholder="Client ID"
          class="w-full rounded-md border border-gray-300 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
        />
      </div>
      <div>
        <label class="block text-sm font-medium text-gray-700 mb-1">Client Secret</label>
        <input
          v-model="config.oauthClientSecret"
          type="password"
          placeholder="Client Secret"
          class="w-full rounded-md border border-gray-300 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
        />
      </div>
      <div>
        <label class="block text-sm font-medium text-gray-700 mb-1">Token URL</label>
        <input
          v-model="config.oauthTokenUrl"
          type="url"
          placeholder="https://auth.example.com/oauth/token"
          class="w-full rounded-md border border-gray-300 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
        />
      </div>
    </template>

    <!-- Basic Auth -->
    <template v-if="config.authType === AuthType.BasicAuth">
      <div>
        <label class="block text-sm font-medium text-gray-700 mb-1">Username</label>
        <input
          v-model="config.basicUsername"
          type="text"
          placeholder="Username"
          class="w-full rounded-md border border-gray-300 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
        />
      </div>
      <div>
        <label class="block text-sm font-medium text-gray-700 mb-1">Password</label>
        <input
          v-model="config.basicPassword"
          type="password"
          placeholder="Password"
          class="w-full rounded-md border border-gray-300 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
        />
      </div>
    </template>

    <!-- Custom Headers -->
    <template v-if="config.authType === AuthType.CustomHeaders">
      <div class="space-y-2">
        <label class="block text-sm font-medium text-gray-700">Custom Headers</label>
        <div
          v-for="(header, idx) in config.customHeaders"
          :key="idx"
          class="flex gap-2 items-center"
        >
          <input
            v-model="header.key"
            type="text"
            placeholder="Header name"
            class="flex-1 rounded-md border border-gray-300 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
          />
          <input
            v-model="header.value"
            type="text"
            placeholder="Header value"
            class="flex-1 rounded-md border border-gray-300 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
          />
          <button
            type="button"
            class="text-red-500 hover:text-red-700 text-sm font-medium"
            @click="removeHeader(idx)"
          >
            Remove
          </button>
        </div>
        <button
          type="button"
          class="text-blue-600 hover:text-blue-800 text-sm font-medium"
          @click="addHeader"
        >
          + Add Header
        </button>
      </div>
    </template>

    <!-- Test Connection -->
    <div class="pt-2">
      <button
        type="button"
        class="inline-flex items-center gap-2 rounded-md bg-gray-800 px-4 py-2 text-sm font-medium text-white hover:bg-gray-700 disabled:opacity-50"
        :disabled="!isValid || testStatus === 'loading'"
        @click="testConnection"
      >
        {{ testStatus === 'loading' ? 'Testing...' : 'Test Connection' }}
      </button>
      <span
        v-if="testStatus === 'success'"
        class="ml-3 text-sm text-green-600 font-medium"
      >
        {{ testMessage }}
      </span>
      <span
        v-if="testStatus === 'error'"
        class="ml-3 text-sm text-red-600 font-medium"
      >
        {{ testMessage }}
      </span>
    </div>
  </div>
</template>
