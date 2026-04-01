import { describe, it, expect } from 'vitest'
import { mount } from '@vue/test-utils'
import HealthIndicator from '@/components/shared/HealthIndicator.vue'

describe('HealthIndicator', () => {
  it('shows green for success rate >= 95%', () => {
    const wrapper = mount(HealthIndicator, {
      props: { successRate: 98 },
    })
    expect(wrapper.find('span.rounded-full').classes()).toContain(
      'bg-green-500',
    )
    expect(wrapper.text()).toContain('98%')
  })

  it('shows green for exactly 95%', () => {
    const wrapper = mount(HealthIndicator, {
      props: { successRate: 95 },
    })
    expect(wrapper.find('span.rounded-full').classes()).toContain(
      'bg-green-500',
    )
  })

  it('shows yellow for success rate between 80% and 95%', () => {
    const wrapper = mount(HealthIndicator, {
      props: { successRate: 88 },
    })
    expect(wrapper.find('span.rounded-full').classes()).toContain(
      'bg-yellow-500',
    )
    expect(wrapper.text()).toContain('88%')
  })

  it('shows red for success rate below 80%', () => {
    const wrapper = mount(HealthIndicator, {
      props: { successRate: 50 },
    })
    expect(wrapper.find('span.rounded-full').classes()).toContain('bg-red-500')
    expect(wrapper.text()).toContain('50%')
  })

  it('shows red for 0%', () => {
    const wrapper = mount(HealthIndicator, {
      props: { successRate: 0 },
    })
    expect(wrapper.find('span.rounded-full').classes()).toContain('bg-red-500')
  })
})
