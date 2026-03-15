<script setup lang="ts">
import { onMounted } from 'vue'
import { RouterView, useRoute } from 'vue-router'
import { useAppStore } from '@/stores/app'
import AppDialog from '@/components/AppDialog.vue'

const store = useAppStore()
const route = useRoute()

onMounted(() => {
  if (route.path !== '/mock-up') {
    store.loadMasterData()
  }
})
</script>

<template>
  <div class="h-screen flex flex-col bg-gray-50 overflow-hidden">
    <AppDialog />
    <!-- 각 뷰가 자체적으로 flex-col + 스크롤 구조를 가짐 -->
    <main class="flex-1 overflow-hidden">
      <RouterView />
    </main>

    <!-- 하단 탭바 — /mock-up에서는 MockupView 내부 탭 사용 -->
    <nav v-if="route.path !== '/mock-up'" class="flex-shrink-0 bg-white border-t border-gray-200 flex z-50">
      <RouterLink
        to="/"
        class="flex-1 flex flex-col items-center py-2 text-xs gap-1"
        :class="route.path === '/' ? 'text-blue-600' : 'text-gray-500'"
      >
        <svg class="w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2"
            d="M3 12l2-2m0 0l7-7 7 7M5 10v10a1 1 0 001 1h3m10-11l2 2m-2-2v10a1 1 0 01-1 1h-3m-6 0a1 1 0 001-1v-4a1 1 0 011-1h2a1 1 0 011 1v4a1 1 0 001 1m-6 0h6" />
        </svg>
        가계부
      </RouterLink>
      <RouterLink
        to="/stats"
        class="flex-1 flex flex-col items-center py-2 text-xs gap-1"
        :class="route.path === '/stats' ? 'text-blue-600' : 'text-gray-500'"
      >
        <svg class="w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2"
            d="M9 19v-6a2 2 0 00-2-2H5a2 2 0 00-2 2v6a2 2 0 002 2h2a2 2 0 002-2zm0 0V9a2 2 0 012-2h2a2 2 0 012 2v10m-6 0a2 2 0 002 2h2a2 2 0 002-2m0 0V5a2 2 0 012-2h2a2 2 0 012 2v14a2 2 0 01-2 2h-2a2 2 0 01-2-2z" />
        </svg>
        통계
      </RouterLink>
      <RouterLink
        to="/settings"
        class="flex-1 flex flex-col items-center py-2 text-xs gap-1"
        :class="route.path === '/settings' ? 'text-blue-600' : 'text-gray-500'"
      >
        <svg class="w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2"
            d="M10.325 4.317c.426-1.756 2.924-1.756 3.35 0a1.724 1.724 0 002.573 1.066c1.543-.94 3.31.826 2.37 2.37a1.724 1.724 0 001.065 2.572c1.756.426 1.756 2.924 0 3.35a1.724 1.724 0 00-1.066 2.573c.94 1.543-.826 3.31-2.37 2.37a1.724 1.724 0 00-2.572 1.065c-.426 1.756-2.924 1.756-3.35 0a1.724 1.724 0 00-2.573-1.066c-1.543.94-3.31-.826-2.37-2.37a1.724 1.724 0 00-1.065-2.572c-1.756-.426-1.756-2.924 0-3.35a1.724 1.724 0 001.066-2.573c-.94-1.543.826-3.31 2.37-2.37.996.608 2.296.07 2.572-1.065z" />
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 12a3 3 0 11-6 0 3 3 0 016 0z" />
        </svg>
        설정
      </RouterLink>
    </nav>
  </div>
</template>
