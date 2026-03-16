import { createRouter, createWebHistory } from 'vue-router'
import HomeView from '@/views/HomeView.vue'
import type { RouteRecordRaw } from 'vue-router'

const routes: RouteRecordRaw[] = [
  { path: '/', name: 'home', component: HomeView },
  { path: '/stats', name: 'stats', component: () => import('@/views/StatsView.vue') },
  // Sprint 6 Step 1 목업 — Step 3에서 실제 API 연동
  { path: '/card-billing', name: 'card-billing', component: () => import('@/views/CardBillingView.vue') },
  { path: '/settings', name: 'settings', component: () => import('@/views/SettingsView.vue') },
]

// DEV-only: 목업 페이지 — production 빌드 시 번들에서 제외
if (import.meta.env.DEV) {
  routes.push({
    path: '/mock-up',
    name: 'mockup',
    component: () => import('@/views/MockupView.vue'),
  })
}

const router = createRouter({
  history: createWebHistory(import.meta.env.BASE_URL),
  routes,
})

export default router
