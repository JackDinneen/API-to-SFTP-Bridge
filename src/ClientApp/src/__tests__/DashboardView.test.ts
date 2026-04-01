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

  it('renders domain-relevant summary tiles', () => {
    const router = createTestRouter()
    const wrapper = mount(DashboardView, {
      global: {
        plugins: [createPinia(), router],
      },
    })
    expect(wrapper.text()).toContain('Active Connections')
    expect(wrapper.text()).toContain('Syncs This Month')
    expect(wrapper.text()).toContain('Records Delivered')
    expect(wrapper.text()).toContain('Errors')
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

  it('shows empty state with create button when no connections', () => {
    const router = createTestRouter()
    const wrapper = mount(DashboardView, {
      global: {
        plugins: [createPinia(), router],
      },
    })
    expect(wrapper.text()).toContain('No connections configured')
    expect(wrapper.text()).toContain('Create Connection')
  })

  it('renders info tooltips on summary tiles', () => {
    const router = createTestRouter()
    const wrapper = mount(DashboardView, {
      global: {
        plugins: [createPinia(), router],
      },
    })
    // Each tile has an info button
    const infoButtons = wrapper.findAll('button[aria-label="More info"]')
    expect(infoButtons.length).toBe(4)
  })
})
