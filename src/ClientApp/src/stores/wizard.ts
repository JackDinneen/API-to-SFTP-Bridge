import { defineStore } from 'pinia'
import { ref, computed } from 'vue'
import { AuthType } from '@/types'
import type { Connection, ApiResponse } from '@/types'
import { useApi } from '@/composables/useApi'

export interface ApiConfig {
  baseUrl: string
  authType: AuthType
  apiKey: string
  apiKeyHeader: string
  oauthClientId: string
  oauthClientSecret: string
  oauthTokenUrl: string
  basicUsername: string
  basicPassword: string
  customHeaders: { key: string; value: string }[]
  connectionTested: boolean
}

export interface EndpointConfig {
  path: string
  sampleResponse: unknown
  paginationStrategy: 'none' | 'offset' | 'cursor' | 'page'
  pageSize: number
  offsetParam: string
  cursorParam: string
  pageParam: string
  iterationEnabled: boolean
  iterationEndpointPath: string
  iterationJsonPath: string
}

export interface FieldMapping {
  targetColumn: string
  sourcePath: string
  transformType: string
  transformConfig: Record<string, unknown>
}

export interface AggregationConfig {
  isSubMonthly: boolean
  method: 'sum' | 'average' | 'last' | 'max'
}

export interface OutputConfig {
  clientName: string
  platformName: string
  sftpHost: string
  sftpPort: number
  sftpUsername: string
  sftpPath: string
  sftpPassword: string
  scheduleMode: 'simple' | 'advanced'
  simpleFrequency: 'daily' | 'weekly' | 'monthly'
  simpleDay: number
  cronExpression: string
  reportingLagDays: number
}

export interface TestResult {
  success: boolean
  csvPreview: string[][]
  validationResults: {
    row: number
    status: 'pass' | 'warning' | 'error'
    message: string
  }[]
}

export interface WizardData {
  apiConfig: ApiConfig
  endpointConfig: EndpointConfig
  mappings: FieldMapping[]
  aggregation: AggregationConfig
  outputConfig: OutputConfig
  testResult: TestResult | null
}

function createDefaultWizardData(): WizardData {
  return {
    apiConfig: {
      baseUrl: '',
      authType: AuthType.ApiKey,
      apiKey: '',
      apiKeyHeader: 'X-API-Key',
      oauthClientId: '',
      oauthClientSecret: '',
      oauthTokenUrl: '',
      basicUsername: '',
      basicPassword: '',
      customHeaders: [{ key: '', value: '' }],
      connectionTested: false,
    },
    endpointConfig: {
      path: '',
      sampleResponse: null,
      paginationStrategy: 'none',
      pageSize: 100,
      offsetParam: 'offset',
      cursorParam: 'cursor',
      pageParam: 'page',
      iterationEnabled: false,
      iterationEndpointPath: '',
      iterationJsonPath: '',
    },
    mappings: [],
    aggregation: {
      isSubMonthly: false,
      method: 'sum',
    },
    outputConfig: {
      clientName: '',
      platformName: '',
      sftpHost: '',
      sftpPort: 22,
      sftpUsername: '',
      sftpPath: '/upload',
      sftpPassword: '',
      scheduleMode: 'simple',
      simpleFrequency: 'daily',
      simpleDay: 1,
      cronExpression: '0 2 * * *',
      reportingLagDays: 30,
    },
    testResult: null,
  }
}

export const STEP_TITLES = [
  'Source API',
  'Endpoint',
  'Field Mapping',
  'Aggregation',
  'Output',
  'Test & Activate',
] as const

export const OBI_COLUMNS = [
  'Asset ID',
  'Asset name',
  'Submeter Code',
  'Utility Type',
  'Year',
  'Month',
  'Value',
] as const

const TRANSFORM_TYPE_MAP: Record<string, string> = {
  Direct: 'DirectMapping',
  'Value Mapping': 'ValueMapping',
  'Unit Conversion': 'UnitConversion',
  'Date Parse': 'DateParse',
  'Static Value': 'StaticValue',
  Concatenation: 'Concatenation',
  Split: 'Split',
}

export const useWizardStore = defineStore('wizard', () => {
  const currentStep = ref(1)
  const createdConnectionId = ref<string | null>(null)
  const wizardData = ref<WizardData>(createDefaultWizardData())
  const stepValid = ref<Record<number, boolean>>({
    1: false,
    2: false,
    3: false,
    4: false,
    5: false,
    6: false,
  })

  const isFirstStep = computed(() => currentStep.value === 1)
  const isLastStep = computed(() => currentStep.value === 6)
  const currentStepValid = computed(
    () => stepValid.value[currentStep.value] ?? false,
  )

  function nextStep() {
    if (currentStep.value < 6) {
      currentStep.value++
    }
  }

  function prevStep() {
    if (currentStep.value > 1) {
      currentStep.value--
    }
  }

  function setStepData<K extends keyof WizardData>(
    key: K,
    data: WizardData[K],
  ) {
    wizardData.value[key] = data
  }

  function setStepValid(step: number, valid: boolean) {
    stepValid.value[step] = valid
  }

  function resetWizard() {
    currentStep.value = 1
    createdConnectionId.value = null
    wizardData.value = createDefaultWizardData()
    stepValid.value = {
      1: false,
      2: false,
      3: false,
      4: false,
      5: false,
      6: false,
    }
  }

  async function submitWizard(activate = true): Promise<boolean> {
    const api = useApi()
    const d = wizardData.value
    const o = d.outputConfig
    const a = d.apiConfig

    const mappings = d.mappings.map((m, index) => ({
      sourcePath: m.sourcePath,
      targetColumn: m.targetColumn,
      transformType: TRANSFORM_TYPE_MAP[m.transformType] ?? 'DirectMapping',
      transformConfig:
        Object.keys(m.transformConfig).length > 0
          ? JSON.stringify(m.transformConfig)
          : null,
      sortOrder: index,
    }))

    const credentials: Record<string, unknown> = {
      sftpUsername: o.sftpUsername,
      sftpPassword: o.sftpPassword,
    }

    switch (a.authType) {
      case AuthType.ApiKey:
        credentials.apiKey = a.apiKey
        credentials.apiKeyHeader = a.apiKeyHeader
        break
      case AuthType.BasicAuth:
        credentials.basicUsername = a.basicUsername
        credentials.basicPassword = a.basicPassword
        break
      case AuthType.OAuth2ClientCredentials:
        credentials.oAuthClientId = a.oauthClientId
        credentials.oAuthClientSecret = a.oauthClientSecret
        credentials.oAuthTokenUrl = a.oauthTokenUrl
        break
      case AuthType.CustomHeaders:
        credentials.customHeaders = a.customHeaders
          .filter((h) => h.key.trim())
          .map((h) => ({ key: h.key, value: h.value }))
        break
    }

    const payload = {
      name: `${o.clientName} - ${o.platformName}`,
      baseUrl: a.baseUrl,
      authType: a.authType,
      scheduleCron: o.cronExpression,
      clientName: o.clientName,
      platformName: o.platformName,
      sftpHost: o.sftpHost,
      sftpPort: o.sftpPort,
      sftpPath: o.sftpPath,
      reportingLagDays: o.reportingLagDays,
      endpointPath: d.endpointConfig.path,
      paginationStrategy:
        d.endpointConfig.paginationStrategy !== 'none'
          ? d.endpointConfig.paginationStrategy
          : null,
      iterationEndpointPath: d.endpointConfig.iterationEnabled
        ? d.endpointConfig.iterationEndpointPath || null
        : null,
      iterationJsonPath: d.endpointConfig.iterationEnabled
        ? d.endpointConfig.iterationJsonPath || null
        : null,
      activate,
      mappings,
      credentials,
    }

    const result = await api.postAsync<Connection>('/connections', payload)
    if (result.success && result.data) {
      createdConnectionId.value = result.data.id
      return true
    }
    return false
  }

  return {
    currentStep,
    createdConnectionId,
    wizardData,
    stepValid,
    isFirstStep,
    isLastStep,
    currentStepValid,
    nextStep,
    prevStep,
    setStepData,
    setStepValid,
    resetWizard,
    submitWizard,
  }
})
