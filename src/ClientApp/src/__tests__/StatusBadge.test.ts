import { describe, it, expect } from 'vitest'
import { mount } from '@vue/test-utils'
import StatusBadge from '@/components/shared/StatusBadge.vue'
import { ConnectionStatus, SyncRunStatus } from '@/types'

describe('StatusBadge', () => {
  it('renders Active status with green color', () => {
    const wrapper = mount(StatusBadge, {
      props: { status: ConnectionStatus.Active },
    })
    expect(wrapper.text()).toBe('Active')
    expect(wrapper.find('span').classes()).toContain('bg-green-100')
    expect(wrapper.find('span').classes()).toContain('text-green-800')
  })

  it('renders Paused status with yellow color', () => {
    const wrapper = mount(StatusBadge, {
      props: { status: ConnectionStatus.Paused },
    })
    expect(wrapper.text()).toBe('Paused')
    expect(wrapper.find('span').classes()).toContain('bg-yellow-100')
    expect(wrapper.find('span').classes()).toContain('text-yellow-800')
  })

  it('renders Error status with red color', () => {
    const wrapper = mount(StatusBadge, {
      props: { status: ConnectionStatus.Error },
    })
    expect(wrapper.text()).toBe('Error')
    expect(wrapper.find('span').classes()).toContain('bg-red-100')
    expect(wrapper.find('span').classes()).toContain('text-red-800')
  })

  it('renders Succeeded status with green color', () => {
    const wrapper = mount(StatusBadge, {
      props: { status: SyncRunStatus.Succeeded },
    })
    expect(wrapper.text()).toBe('Succeeded')
    expect(wrapper.find('span').classes()).toContain('bg-green-100')
  })

  it('renders Running status with blue color', () => {
    const wrapper = mount(StatusBadge, {
      props: { status: SyncRunStatus.Running },
    })
    expect(wrapper.text()).toBe('Running')
    expect(wrapper.find('span').classes()).toContain('bg-blue-100')
    expect(wrapper.find('span').classes()).toContain('text-blue-800')
  })

  it('renders Failed status with red color', () => {
    const wrapper = mount(StatusBadge, {
      props: { status: SyncRunStatus.Failed },
    })
    expect(wrapper.text()).toBe('Failed')
    expect(wrapper.find('span').classes()).toContain('bg-red-100')
  })

  it('has correct aria-label', () => {
    const wrapper = mount(StatusBadge, {
      props: { status: ConnectionStatus.Active },
    })
    expect(wrapper.find('span').attributes('aria-label')).toBe('Status: Active')
  })
})
