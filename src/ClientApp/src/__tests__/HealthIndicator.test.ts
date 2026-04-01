import { describe, it, expect } from 'vitest'
import { mount } from '@vue/test-utils'
import HealthIndicator from '@/components/shared/HealthIndicator.vue'

describe('HealthIndicator', () => {
  it('shows percentage text', () => {
    const wrapper = mount(HealthIndicator, {
      props: { successRate: 98 },
    })
    expect(wrapper.text()).toContain('98%')
  })

  it('renders multi-segment progress bar', () => {
    const wrapper = mount(HealthIndicator, {
      props: { successRate: 95 },
    })
    const segments = wrapper.findAll('.h-full')
    expect(segments.length).toBeGreaterThanOrEqual(2)
  })

  it('shows percentage for degraded rate', () => {
    const wrapper = mount(HealthIndicator, {
      props: { successRate: 88 },
    })
    expect(wrapper.text()).toContain('88%')
  })

  it('shows percentage for unhealthy rate', () => {
    const wrapper = mount(HealthIndicator, {
      props: { successRate: 50 },
    })
    expect(wrapper.text()).toContain('50%')
  })

  it('has correct aria-label', () => {
    const wrapper = mount(HealthIndicator, {
      props: { successRate: 0 },
    })
    expect(wrapper.find('div').attributes('aria-label')).toContain('0%')
  })
})
