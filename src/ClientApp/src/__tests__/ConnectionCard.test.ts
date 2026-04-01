import { describe, it, expect } from 'vitest'
import { mount } from '@vue/test-utils'
import ConnectionCard from '@/components/dashboard/ConnectionCard.vue'
import { ConnectionStatus, AuthType } from '@/types'
import type { Connection } from '@/types'

const mockConnection: Connection = {
  id: '1',
  name: 'Test Connection',
  baseUrl: 'https://api.example.com',
  authType: AuthType.ApiKey,
  status: ConnectionStatus.Active,
  scheduleCron: '0 5 1 * *',
  clientName: 'Acme Corp',
  platformName: 'EnergyPlatform',
  sftpHost: 'sftp.obi.com',
  sftpPort: 22,
  sftpPath: '/uploads',
  reportingLagDays: 5,
  createdBy: 'admin@example.com',
  createdAt: '2026-01-01T00:00:00Z',
  updatedAt: '2026-03-01T00:00:00Z',
  lastSyncAt: '2026-03-15T10:00:00Z',
  lastSyncRecordCount: 247,
  nextSyncAt: '2026-04-01T05:00:00Z',
  successRate: 98,
}

describe('ConnectionCard', () => {
  it('renders the connection name', () => {
    const wrapper = mount(ConnectionCard, {
      props: { connection: mockConnection },
    })
    expect(wrapper.text()).toContain('Test Connection')
  })

  it('renders the connection status', () => {
    const wrapper = mount(ConnectionCard, {
      props: { connection: mockConnection },
    })
    expect(wrapper.text()).toContain('Active')
  })

  it('renders client and platform names', () => {
    const wrapper = mount(ConnectionCard, {
      props: { connection: mockConnection },
    })
    expect(wrapper.text()).toContain('Acme Corp')
    expect(wrapper.text()).toContain('EnergyPlatform')
  })

  it('renders record count', () => {
    const wrapper = mount(ConnectionCard, {
      props: { connection: mockConnection },
    })
    expect(wrapper.text()).toContain('247')
  })

  it('emits syncNow when Sync Now is clicked', async () => {
    const wrapper = mount(ConnectionCard, {
      props: { connection: mockConnection },
    })
    await wrapper.find('button[aria-label="Sync now"]').trigger('click')
    expect(wrapper.emitted('syncNow')).toBeTruthy()
    expect(wrapper.emitted('syncNow')![0]).toEqual(['1'])
  })

  it('shows Resume button when connection is paused', () => {
    const wrapper = mount(ConnectionCard, {
      props: {
        connection: { ...mockConnection, status: ConnectionStatus.Paused },
      },
    })
    expect(wrapper.text()).toContain('Resume')
  })
})
