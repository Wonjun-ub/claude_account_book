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

    <!-- 하단 탭바 — /mock-up에서는 MockupView 내부 탭 사용 / 현재 가계부 탭만 노출 -->
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
    </nav>
  </div>
</template>
