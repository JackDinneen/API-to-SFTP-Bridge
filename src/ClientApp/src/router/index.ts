import { createRouter, createWebHistory } from 'vue-router'
import DashboardView from '@/views/DashboardView.vue'
import ConnectionDetailView from '@/views/ConnectionDetailView.vue'
import WizardView from '@/views/WizardView.vue'
import LoginView from '@/views/LoginView.vue'

const router = createRouter({
  history: createWebHistory(import.meta.env.BASE_URL),
  routes: [
    { path: '/', redirect: '/dashboard' },
    { path: '/dashboard', name: 'dashboard', component: DashboardView },
    { path: '/connections/new', name: 'wizard', component: WizardView },
    {
      path: '/connections/:id',
      name: 'connection-detail',
      component: ConnectionDetailView,
    },
    { path: '/login', name: 'login', component: LoginView },
  ],
})

export default router
