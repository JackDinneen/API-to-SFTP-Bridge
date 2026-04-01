<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { useSettingsStore } from '@/stores/settings'
import type { DataTableColumn } from '@/types'
import DataTable from '@/components/shared/DataTable.vue'
import LoadingSpinner from '@/components/shared/LoadingSpinner.vue'

const settingsStore = useSettingsStore()
const activeTab = ref<'reference' | 'conversions' | 'platform'>('reference')
const fileInput = ref<HTMLInputElement | null>(null)
const uploadMessage = ref('')

onMounted(() => {
  settingsStore.fetchReferenceData()
  settingsStore.fetchUserProfile()
})

const tabs = [
  { key: 'reference' as const, label: 'Reference Data' },
  { key: 'conversions' as const, label: 'Unit Conversions' },
  { key: 'platform' as const, label: 'Platform' },
]

// Reference Data table columns
const refColumns: DataTableColumn[] = [
  { key: 'assetId', label: 'Asset ID', sortable: true },
  { key: 'assetName', label: 'Asset Name', sortable: true },
  { key: 'submeterCode', label: 'Submeter Code', sortable: true },
  { key: 'utilityType', label: 'Utility Type', sortable: true },
  { key: 'createdAt', label: 'Uploaded', sortable: true },
  { key: 'actions', label: '', sortable: false },
]

// Unit conversion table
const conversions = [
  { from: 'MWh', to: 'kWh', formula: '× 1,000' },
  { from: 'GJ', to: 'kWh', formula: '× 277.778' },
  { from: 'therms', to: 'kWh', formula: '× 29.3071' },
  { from: 'gallons (US)', to: 'm³', formula: '× 0.00378541' },
  { from: 'litres', to: 'm³', formula: '÷ 1,000' },
  { from: 'kg', to: 't', formula: '÷ 1,000' },
  { from: 'lbs', to: 't', formula: '÷ 2,204.62' },
  { from: 'cubic feet', to: 'm³', formula: '× 0.0283168' },
]

const conversionColumns: DataTableColumn[] = [
  { key: 'from', label: 'From', sortable: false },
  { key: 'to', label: 'To', sortable: false },
  { key: 'formula', label: 'Formula', sortable: false },
]

const apiBaseUrl = import.meta.env.VITE_API_BASE_URL ?? '/api'

const utilityTypes = [
  { type: 'Electricity', unit: 'kWh' },
  { type: 'Gas', unit: 'kWh' },
  { type: 'Water', unit: 'm³' },
  { type: 'Waste', unit: 't (tonnes)' },
  { type: 'District Heating', unit: 'kWh' },
  { type: 'District Cooling', unit: 'kWh' },
]

function triggerUpload() {
  fileInput.value?.click()
}

async function handleFileUpload(event: Event) {
  const target = event.target as HTMLInputElement
  const file = target.files?.[0]
  if (!file) return

  uploadMessage.value = ''
  const count = await settingsStore.uploadReferenceData(file)
  if (count !== null) {
    uploadMessage.value = `Successfully imported ${count} records`
  }
  target.value = ''
}

async function handleDelete(id: string) {
  await settingsStore.deleteReferenceData(id)
}

function formatDate(val: unknown): string {
  if (!val || typeof val !== 'string') return '-'
  return new Date(val).toLocaleDateString('en-GB', {
    year: 'numeric',
    month: 'short',
    day: 'numeric',
  })
}
</script>

<template>
  <div>
    <!-- Tabs -->
    <div class="border-b border-gray-200">
      <nav class="-mb-px flex gap-6" aria-label="Settings tabs">
        <button
          v-for="tab in tabs"
          :key="tab.key"
          class="border-b-2 px-1 py-3 text-sm font-medium transition-colors"
          :class="
            activeTab === tab.key
              ? 'border-gray-900 text-gray-900'
              : 'border-transparent text-gray-500 hover:border-gray-300 hover:text-gray-700'
          "
          @click="activeTab = tab.key"
        >
          {{ tab.label }}
        </button>
      </nav>
    </div>

    <!-- Tab Content -->
    <div class="mt-6">
      <!-- Reference Data Tab -->
      <div v-if="activeTab === 'reference'">
        <div class="mb-4 flex items-center justify-between">
          <div>
            <h3 class="text-base font-semibold text-gray-900">
              Obi Reference Data
            </h3>
            <p class="mt-1 text-sm text-gray-500">
              Asset IDs, Submeter Codes, and Utility Types used to validate sync
              data against Obi's records.
            </p>
          </div>
          <div class="flex items-center gap-3">
            <input
              ref="fileInput"
              type="file"
              accept=".csv,.json"
              class="hidden"
              @change="handleFileUpload"
            />
            <button
              class="flex items-center gap-1.5 rounded-md px-3 py-2 text-sm font-medium text-white"
              style="background-color: hsl(var(--navy-dark))"
              @click="triggerUpload"
            >
              <svg
                class="h-4 w-4"
                fill="none"
                stroke="currentColor"
                stroke-width="2"
                viewBox="0 0 24 24"
              >
                <path
                  stroke-linecap="round"
                  stroke-linejoin="round"
                  d="M3 16.5v2.25A2.25 2.25 0 005.25 21h13.5A2.25 2.25 0 0021 18.75V16.5m-13.5-9L12 3m0 0l4.5 4.5M12 3v13.5"
                />
              </svg>
              Upload CSV / JSON
            </button>
          </div>
        </div>

        <!-- Upload feedback -->
        <div
          v-if="uploadMessage"
          class="mb-4 rounded-md bg-green-50 p-3 text-sm text-green-700"
        >
          {{ uploadMessage }}
        </div>
        <div
          v-if="settingsStore.error"
          class="mb-4 rounded-md bg-red-50 p-3 text-sm text-red-700"
        >
          {{ settingsStore.error }}
        </div>

        <LoadingSpinner v-if="settingsStore.loading" size="md" />

        <!-- Empty state -->
        <div
          v-else-if="settingsStore.referenceData.length === 0"
          class="rounded-lg border border-gray-200 bg-white py-12 text-center"
        >
          <svg
            class="mx-auto h-10 w-10 text-gray-300"
            fill="none"
            stroke="currentColor"
            stroke-width="1"
            viewBox="0 0 24 24"
          >
            <path
              stroke-linecap="round"
              stroke-linejoin="round"
              d="M19.5 14.25v-2.625a3.375 3.375 0 00-3.375-3.375h-1.5A1.125 1.125 0 0113.5 7.125v-1.5a3.375 3.375 0 00-3.375-3.375H8.25m2.25 0H5.625c-.621 0-1.125.504-1.125 1.125v17.25c0 .621.504 1.125 1.125 1.125h12.75c.621 0 1.125-.504 1.125-1.125V11.25a9 9 0 00-9-9z"
            />
          </svg>
          <h3 class="mt-3 text-sm font-semibold text-gray-900">
            No reference data uploaded
          </h3>
          <p class="mt-1 text-sm text-gray-500 max-w-sm mx-auto">
            Upload a CSV or JSON file containing Obi's Asset IDs, Submeter
            Codes, and Utility Types to enable validation.
          </p>
          <button
            class="mt-4 text-sm font-medium hover:underline"
            style="color: hsl(var(--navy-dark))"
            @click="triggerUpload"
          >
            Upload reference data
          </button>
        </div>

        <!-- Reference data table -->
        <DataTable
          v-else
          :columns="refColumns"
          :data="
            settingsStore.referenceData as unknown as Record<string, unknown>[]
          "
        >
          <template #cell-createdAt="{ value }">
            {{ formatDate(value) }}
          </template>
          <template #cell-actions="{ row }">
            <button
              class="text-xs text-red-600 hover:text-red-800"
              @click="
                handleDelete((row as Record<string, unknown>).id as string)
              "
            >
              Delete
            </button>
          </template>
        </DataTable>
      </div>

      <!-- Unit Conversions Tab -->
      <div v-if="activeTab === 'conversions'">
        <div class="mb-4">
          <h3 class="text-base font-semibold text-gray-900">
            Unit Conversions
          </h3>
          <p class="mt-1 text-sm text-gray-500">
            Pre-loaded conversion factors applied during data transformation.
            These are used when source APIs provide data in different units than
            Obi requires.
          </p>
        </div>

        <DataTable
          :columns="conversionColumns"
          :data="conversions as unknown as Record<string, unknown>[]"
        />

        <p class="mt-4 text-xs text-gray-400">
          Custom conversions can be configured per connection in the field
          mapping step of the connection wizard.
        </p>
      </div>

      <!-- Platform Tab -->
      <div v-if="activeTab === 'platform'">
        <div class="grid gap-6 lg:grid-cols-2">
          <!-- Current User -->
          <div class="rounded-lg border border-gray-200 bg-white p-5">
            <h3 class="text-base font-semibold text-gray-900 mb-4">
              Current User
            </h3>
            <div v-if="settingsStore.userProfile" class="space-y-3 text-sm">
              <div class="flex justify-between">
                <span class="text-gray-500">Name</span>
                <span class="font-medium text-gray-900">{{
                  settingsStore.userProfile.displayName
                }}</span>
              </div>
              <div class="flex justify-between">
                <span class="text-gray-500">Email</span>
                <span class="font-medium text-gray-900">{{
                  settingsStore.userProfile.email
                }}</span>
              </div>
              <div class="flex justify-between">
                <span class="text-gray-500">Role</span>
                <span class="font-medium text-gray-900">{{
                  settingsStore.userProfile.role
                }}</span>
              </div>
            </div>
            <p v-else class="text-sm text-gray-400">Loading...</p>
          </div>

          <!-- Utility Types -->
          <div class="rounded-lg border border-gray-200 bg-white p-5">
            <h3 class="text-base font-semibold text-gray-900 mb-4">
              Accepted Utility Types
            </h3>
            <div class="space-y-2">
              <div
                v-for="ut in utilityTypes"
                :key="ut.type"
                class="flex items-center justify-between rounded-md bg-gray-50 px-3 py-2 text-sm"
              >
                <span class="font-medium text-gray-900">{{ ut.type }}</span>
                <span class="text-gray-500">{{ ut.unit }}</span>
              </div>
            </div>
          </div>

          <!-- App Info -->
          <div class="rounded-lg border border-gray-200 bg-white p-5">
            <h3 class="text-base font-semibold text-gray-900 mb-4">
              Platform Info
            </h3>
            <div class="space-y-3 text-sm">
              <div class="flex justify-between">
                <span class="text-gray-500">Application</span>
                <span class="font-medium text-gray-900">Obi Bridge</span>
              </div>
              <div class="flex justify-between">
                <span class="text-gray-500">Version</span>
                <span class="font-medium text-gray-900">1.0.0-dev</span>
              </div>
              <div class="flex justify-between">
                <span class="text-gray-500">Environment</span>
                <span class="font-medium text-gray-900">Development</span>
              </div>
              <div class="flex justify-between">
                <span class="text-gray-500">API</span>
                <span class="font-medium text-gray-900">{{
                  apiBaseUrl
                }}</span>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>
