import { createRouter, createWebHistory } from 'vue-router'
import HomeView from '@/views/HomeView.vue'
import type { RouteRecordRaw } from 'vue-router'

const routes: RouteRecordRaw[] = [
  { path: '/', redirect: '/home' },
  { path: '/home', name: 'home', component: HomeView },
  { path: '/stats', name: 'stats', component: () => import('@/views/StatsView.vue') },
  { path: '/settings', name: 'settings', component: () => import('@/views/SettingsView.vue') },
]

// DEV 환경에서만 /mock-up 라우트 등록 — production 빌드 시 dead code elimination으로 번들 미포함
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
