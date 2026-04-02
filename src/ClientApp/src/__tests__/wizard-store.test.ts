import { describe, it, expect, beforeEach, vi } from 'vitest'
import { createPinia, setActivePinia } from 'pinia'
import { useWizardStore } from '@/stores/wizard'
import { AuthType } from '@/types'

// Mock useApi
const mockPostAsync = vi.fn()
vi.mock('@/composables/useApi', () => ({
  useApi: () => ({
    postAsync: mockPostAsync,
    getAsync: vi.fn(),
    putAsync: vi.fn(),
    deleteAsync: vi.fn(),
  }),
}))

describe('useWizardStore - submitWizard', () => {
  beforeEach(() => {
    setActivePinia(createPinia())
    vi.clearAllMocks()
  })

  function setupWizardData(store: ReturnType<typeof useWizardStore>) {
    store.wizardData.apiConfig = {
      baseUrl: 'https://api.test.com',
      authType: AuthType.ApiKey,
      apiKey: 'test-key-123',
      apiKeyHeader: 'X-Api-Key',
      oauthClientId: '',
      oauthClientSecret: '',
      oauthTokenUrl: '',
      basicUsername: '',
      basicPassword: '',
      customHeaders: [{ key: '', value: '' }],
      connectionTested: true,
    }
    store.wizardData.endpointConfig = {
      path: '/v1/buildings',
      sampleResponse: { data: [] },
      paginationStrategy: 'page',
      pageSize: 50,
      offsetParam: 'offset',
      cursorParam: 'cursor',
      pageParam: 'page',
      iterationEnabled: false,
      iterationEndpointPath: '',
      iterationJsonPath: '',
    }
    store.wizardData.mappings = [
      {
        targetColumn: 'Asset ID',
        sourcePath: 'data.building_id',
        transformType: 'Direct',
        transformConfig: {},
      },
      {
        targetColumn: 'Value',
        sourcePath: 'data.kwh',
        transformType: 'Unit Conversion',
        transformConfig: { unitFrom: 'MWh', unitTo: 'kWh' },
      },
      {
        targetColumn: 'Utility Type',
        sourcePath: '',
        transformType: 'Static Value',
        transformConfig: { staticValue: 'Electricity' },
      },
    ]
    store.wizardData.outputConfig = {
      clientName: 'acme',
      platformName: 'GreenMetrics',
      sftpHost: 'sftp.example.com',
      sftpPort: 22,
      sftpUsername: 'sftp_user',
      sftpPath: '/upload',
      sftpPassword: 'sftp_pass',
      scheduleMode: 'simple',
      simpleFrequency: 'monthly',
      simpleDay: 5,
      cronExpression: '0 2 5 * *',
      reportingLagDays: 30,
    }
  }

  it('maps wizard data to correct API payload shape', async () => {
    const store = useWizardStore()
    setupWizardData(store)

    mockPostAsync.mockResolvedValue({
      success: true,
      data: { id: 'conn-123' },
    })

    await store.submitWizard(true)

    expect(mockPostAsync).toHaveBeenCalledTimes(1)
    const [url, payload] = mockPostAsync.mock.calls[0]
    expect(url).toBe('/connections')
    expect(payload.name).toBe('acme - GreenMetrics')
    expect(payload.baseUrl).toBe('https://api.test.com')
    expect(payload.authType).toBe(AuthType.ApiKey)
    expect(payload.activate).toBe(true)
    expect(payload.clientName).toBe('acme')
    expect(payload.platformName).toBe('GreenMetrics')
    expect(payload.sftpHost).toBe('sftp.example.com')
    expect(payload.scheduleCron).toBe('0 2 5 * *')
    expect(payload.paginationStrategy).toBe('page')
  })

  it('converts transform type display names to enum strings', async () => {
    const store = useWizardStore()
    setupWizardData(store)

    mockPostAsync.mockResolvedValue({
      success: true,
      data: { id: 'conn-123' },
    })

    await store.submitWizard(true)

    const payload = mockPostAsync.mock.calls[0][1]
    expect(payload.mappings[0].transformType).toBe('DirectMapping')
    expect(payload.mappings[1].transformType).toBe('UnitConversion')
    expect(payload.mappings[2].transformType).toBe('StaticValue')
  })

  it('serializes transformConfig as JSON string', async () => {
    const store = useWizardStore()
    setupWizardData(store)

    mockPostAsync.mockResolvedValue({
      success: true,
      data: { id: 'conn-123' },
    })

    await store.submitWizard(true)

    const payload = mockPostAsync.mock.calls[0][1]
    expect(payload.mappings[0].transformConfig).toBeNull() // empty config
    expect(payload.mappings[1].transformConfig).toBe(
      JSON.stringify({ unitFrom: 'MWh', unitTo: 'kWh' }),
    )
  })

  it('assembles ApiKey credentials correctly', async () => {
    const store = useWizardStore()
    setupWizardData(store)

    mockPostAsync.mockResolvedValue({
      success: true,
      data: { id: 'conn-123' },
    })

    await store.submitWizard(true)

    const payload = mockPostAsync.mock.calls[0][1]
    expect(payload.credentials.apiKey).toBe('test-key-123')
    expect(payload.credentials.apiKeyHeader).toBe('X-Api-Key')
    expect(payload.credentials.sftpUsername).toBe('sftp_user')
    expect(payload.credentials.sftpPassword).toBe('sftp_pass')
  })

  it('assembles BasicAuth credentials correctly', async () => {
    const store = useWizardStore()
    setupWizardData(store)
    store.wizardData.apiConfig.authType = AuthType.BasicAuth
    store.wizardData.apiConfig.basicUsername = 'admin'
    store.wizardData.apiConfig.basicPassword = 'secret'

    mockPostAsync.mockResolvedValue({
      success: true,
      data: { id: 'conn-123' },
    })

    await store.submitWizard(true)

    const payload = mockPostAsync.mock.calls[0][1]
    expect(payload.credentials.basicUsername).toBe('admin')
    expect(payload.credentials.basicPassword).toBe('secret')
  })

  it('assembles OAuth2 credentials correctly', async () => {
    const store = useWizardStore()
    setupWizardData(store)
    store.wizardData.apiConfig.authType = AuthType.OAuth2ClientCredentials
    store.wizardData.apiConfig.oauthClientId = 'client-id'
    store.wizardData.apiConfig.oauthClientSecret = 'client-secret'
    store.wizardData.apiConfig.oauthTokenUrl = 'https://auth.example.com/token'

    mockPostAsync.mockResolvedValue({
      success: true,
      data: { id: 'conn-123' },
    })

    await store.submitWizard(true)

    const payload = mockPostAsync.mock.calls[0][1]
    expect(payload.credentials.oAuthClientId).toBe('client-id')
    expect(payload.credentials.oAuthClientSecret).toBe('client-secret')
    expect(payload.credentials.oAuthTokenUrl).toBe(
      'https://auth.example.com/token',
    )
  })

  it('sets createdConnectionId on success', async () => {
    const store = useWizardStore()
    setupWizardData(store)

    mockPostAsync.mockResolvedValue({
      success: true,
      data: { id: 'new-conn-456' },
    })

    const result = await store.submitWizard(true)

    expect(result).toBe(true)
    expect(store.createdConnectionId).toBe('new-conn-456')
  })

  it('returns false and does not set connectionId on failure', async () => {
    const store = useWizardStore()
    setupWizardData(store)

    mockPostAsync.mockResolvedValue({
      success: false,
      message: 'Validation failed',
    })

    const result = await store.submitWizard(true)

    expect(result).toBe(false)
    expect(store.createdConnectionId).toBeNull()
  })

  it('passes activate=false for test sync', async () => {
    const store = useWizardStore()
    setupWizardData(store)

    mockPostAsync.mockResolvedValue({
      success: true,
      data: { id: 'conn-789' },
    })

    await store.submitWizard(false)

    const payload = mockPostAsync.mock.calls[0][1]
    expect(payload.activate).toBe(false)
  })

  it('resets createdConnectionId on resetWizard', () => {
    const store = useWizardStore()
    store.createdConnectionId = 'some-id'

    store.resetWizard()

    expect(store.createdConnectionId).toBeNull()
  })

  it('sets paginationStrategy to null when none', async () => {
    const store = useWizardStore()
    setupWizardData(store)
    store.wizardData.endpointConfig.paginationStrategy = 'none'

    mockPostAsync.mockResolvedValue({
      success: true,
      data: { id: 'conn-123' },
    })

    await store.submitWizard(true)

    const payload = mockPostAsync.mock.calls[0][1]
    expect(payload.paginationStrategy).toBeNull()
  })
})
