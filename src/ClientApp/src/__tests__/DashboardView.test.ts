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

  it('shows empty state when no connections exist', () => {
    const router = createTestRouter()
    const wrapper = mount(DashboardView, {
      global: {
        plugins: [createPinia(), router],
      },
    })
    expect(wrapper.find('[data-testid="empty-state"]').exists()).toBe(true)
    expect(wrapper.text()).toContain('No connections yet')
  })

  it('renders the dashboard title', () => {
    const router = createTestRouter()
    const wrapper = mount(DashboardView, {
      global: {
        plugins: [createPinia(), router],
      },
    })
    expect(wrapper.text()).toContain('Dashboard')
  })

  it('has a New Connection link', () => {
    const router = createTestRouter()
    const wrapper = mount(DashboardView, {
      global: {
        plugins: [createPinia(), router],
      },
    })
    expect(wrapper.text()).toContain('New Connection')
  })

  it('shows Create Connection button in empty state', () => {
    const router = createTestRouter()
    const wrapper = mount(DashboardView, {
      global: {
        plugins: [createPinia(), router],
      },
    })
    expect(wrapper.text()).toContain('Create Connection')
  })
})
