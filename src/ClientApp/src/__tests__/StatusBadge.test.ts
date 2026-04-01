import { describe, it, expect } from 'vitest'
import { mount } from '@vue/test-utils'
import StatusBadge from '@/components/shared/StatusBadge.vue'
import { ConnectionStatus, SyncRunStatus } from '@/types'

describe('StatusBadge', () => {
  it('renders Active status with green color', () => {
    const wrapper = mount(StatusBadge, {
      props: { status: ConnectionStatus.Active },
    })
    expect(wrapper.text()).toContain('Active')
    expect(wrapper.find('span').classes()).toContain('bg-green-50')
    expect(wrapper.find('span').classes()).toContain('text-green-600')
  })

  it('renders Paused status with amber color', () => {
    const wrapper = mount(StatusBadge, {
      props: { status: ConnectionStatus.Paused },
    })
    expect(wrapper.text()).toContain('Paused')
    expect(wrapper.find('span').classes()).toContain('bg-amber-50')
    expect(wrapper.find('span').classes()).toContain('text-amber-600')
  })

  it('renders Error status with red color', () => {
    const wrapper = mount(StatusBadge, {
      props: { status: ConnectionStatus.Error },
    })
    expect(wrapper.text()).toContain('Error')
    expect(wrapper.find('span').classes()).toContain('bg-red-50')
    expect(wrapper.find('span').classes()).toContain('text-red-600')
  })

  it('renders Succeeded status with green color', () => {
    const wrapper = mount(StatusBadge, {
      props: { status: SyncRunStatus.Succeeded },
    })
    expect(wrapper.text()).toContain('Succeeded')
    expect(wrapper.find('span').classes()).toContain('bg-green-50')
  })

  it('renders Running status with blue color', () => {
    const wrapper = mount(StatusBadge, {
      props: { status: SyncRunStatus.Running },
    })
    expect(wrapper.text()).toContain('Running')
    expect(wrapper.find('span').classes()).toContain('bg-blue-50')
    expect(wrapper.find('span').classes()).toContain('text-blue-600')
  })

  it('renders Failed status with red color', () => {
    const wrapper = mount(StatusBadge, {
      props: { status: SyncRunStatus.Failed },
    })
    expect(wrapper.text()).toContain('Failed')
    expect(wrapper.find('span').classes()).toContain('bg-red-50')
  })

  it('renders icon SVG', () => {
    const wrapper = mount(StatusBadge, {
      props: { status: ConnectionStatus.Active },
    })
    expect(wrapper.find('svg').exists()).toBe(true)
  })

  it('has correct aria-label', () => {
    const wrapper = mount(StatusBadge, {
      props: { status: ConnectionStatus.Active },
    })
    expect(wrapper.find('span').attributes('aria-label')).toBe('Status: Active')
  })
})
