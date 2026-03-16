import { createRouter, createWebHistory } from 'vue-router'
import HomeView from '@/views/HomeView.vue'
import MockupView from '@/views/MockupView.vue'
import type { RouteRecordRaw } from 'vue-router'

const routes: RouteRecordRaw[] = [
  { path: '/', redirect: '/mock-up' },
  { path: '/home', name: 'home', component: HomeView },
  { path: '/stats', name: 'stats', component: () => import('@/views/StatsView.vue') },
  { path: '/settings', name: 'settings', component: () => import('@/views/SettingsView.vue') },
  { path: '/mock-up', name: 'mockup', component: MockupView },
]

const router = createRouter({
  history: createWebHistory(import.meta.env.BASE_URL),
  routes,
})

export default router
