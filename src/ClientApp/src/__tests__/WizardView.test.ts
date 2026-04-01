import { describe, it, expect, beforeEach } from 'vitest'
import { mount } from '@vue/test-utils'
import { createPinia, setActivePinia } from 'pinia'
import { createRouter, createMemoryHistory } from 'vue-router'
import WizardView from '@/views/WizardView.vue'
import { useWizardStore } from '@/stores/wizard'

function createTestRouter() {
  return createRouter({
    history: createMemoryHistory(),
    routes: [
      { path: '/', redirect: '/dashboard' },
      {
        path: '/dashboard',
        name: 'dashboard',
        component: { template: '<div>Dashboard</div>' },
      },
      { path: '/connections/new', name: 'wizard', component: WizardView },
    ],
  })
}

function mountWizard() {
  const pinia = createPinia()
  const router = createTestRouter()
  const wrapper = mount(WizardView, {
    global: {
      plugins: [pinia, router],
    },
  })
  const store = useWizardStore()
  return { wrapper, store, router }
}

describe('WizardView', () => {
  beforeEach(() => {
    setActivePinia(createPinia())
  })

  it('renders step indicator with all 6 steps', () => {
    const { wrapper } = mountWizard()
    for (let i = 1; i <= 6; i++) {
      expect(wrapper.find(`[data-testid="step-indicator-${i}"]`).exists()).toBe(
        true,
      )
    }
  })

  it('renders step 1 by default', () => {
    const { wrapper } = mountWizard()
    expect(wrapper.text()).toContain('Source API Configuration')
  })

  it('navigates to step 2 when next is clicked and step 1 is valid', async () => {
    const { wrapper, store } = mountWizard()
    store.setStepValid(1, true)
    await wrapper.vm.$nextTick()
    const nextButton = wrapper.find('[data-testid="next-button"]')
    expect(nextButton.exists()).toBe(true)
    await nextButton.trigger('click')
    await wrapper.vm.$nextTick()
    expect(store.currentStep).toBe(2)
    expect(wrapper.text()).toContain('Endpoint Discovery')
  })

  it('disables next button when step is not valid', () => {
    const { wrapper, store } = mountWizard()
    store.setStepValid(1, false)
    const nextButton = wrapper.find('[data-testid="next-button"]')
    expect((nextButton.element as HTMLButtonElement).disabled).toBe(true)
  })

  it('shows back button on step 2 and navigates back', async () => {
    const { wrapper, store } = mountWizard()
    store.setStepValid(1, true)
    store.nextStep()
    await wrapper.vm.$nextTick()
    const backButton = wrapper.find('[data-testid="back-button"]')
    expect(backButton.exists()).toBe(true)
    await backButton.trigger('click')
    await wrapper.vm.$nextTick()
    expect(store.currentStep).toBe(1)
  })

  it('does not show back button on step 1', () => {
    const { wrapper } = mountWizard()
    expect(wrapper.find('[data-testid="back-button"]').exists()).toBe(false)
  })

  it('displays step labels', () => {
    const { wrapper } = mountWizard()
    expect(wrapper.text()).toContain('Source API')
    expect(wrapper.text()).toContain('Endpoint')
    expect(wrapper.text()).toContain('Field Mapping')
    expect(wrapper.text()).toContain('Aggregation')
    expect(wrapper.text()).toContain('Output')
    expect(wrapper.text()).toContain('Test & Activate')
  })
})
