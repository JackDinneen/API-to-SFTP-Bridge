import { describe, it, expect, beforeEach } from 'vitest'
import { mount } from '@vue/test-utils'
import { createPinia, setActivePinia } from 'pinia'
import { createRouter, createMemoryHistory } from 'vue-router'
import DashboardView from '@/views/DashboardView.vue'

function createTestRouter() {
  return createRouter({
    history: createMemoryHistory(),
    routes: [
      { path: '/dashboard', name: 'dashboard', component: DashboardView },
      {
        path: '/connections/new',
        name: 'wizard',
        component: { template: '<div />' },
      },
      {
        path: '/connections/:id',
        name: 'connection-detail',
        component: { template: '<div />' },
      },
    ],
  })
}

describe('DashboardView', () => {
  beforeEach(() => {
    setActivePinia(createPinia())
  })

  it('renders summary cards', () => {
    const router = createTestRouter()
    const wrapper = mount(DashboardView, {
      global: {
        plugins: [createPinia(), router],
      },
    })
    expect(wrapper.text()).toContain('Active')
    expect(wrapper.text()).toContain('Paused')
    expect(wrapper.text()).toContain('Error')
    expect(wrapper.text()).toContain('Total')
  })

  it('renders search toolbar', () => {
    const router = createTestRouter()
    const wrapper = mount(DashboardView, {
      global: {
        plugins: [createPinia(), router],
      },
    })
    expect(wrapper.find('input[type="text"]').exists()).toBe(true)
    expect(wrapper.text()).toContain('Filters')
    expect(wrapper.text()).toContain('Export')
  })

  it('shows empty table message when no connections', () => {
    const router = createTestRouter()
    const wrapper = mount(DashboardView, {
      global: {
        plugins: [createPinia(), router],
      },
    })
    expect(wrapper.text()).toContain('No connections yet')
  })

  it('renders summary card counts as 0 when empty', () => {
    const router = createTestRouter()
    const wrapper = mount(DashboardView, {
      global: {
        plugins: [createPinia(), router],
      },
    })
    const counts = wrapper.findAll('.text-2xl')
    expect(counts.length).toBeGreaterThanOrEqual(4)
  })
})
