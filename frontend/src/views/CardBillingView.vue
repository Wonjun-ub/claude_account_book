<script setup lang="ts">
// Sprint 6 Step 1 목업 뷰 — mock 데이터로 동작
// Step 3에서 mockCards → API 호출로 교체 예정
import { mockCards } from '@/mocks/cardBilling.mock'
import CardBillingCard from '@/components/CardBillingCard.vue'
</script>

<template>
  <div class="h-full flex flex-col max-w-lg mx-auto">
    <!-- 헤더 -->
    <div class="flex-shrink-0 bg-white border-b border-gray-100 px-4 py-4">
      <h1 class="text-lg font-bold text-gray-900">카드 결제 현황</h1>
      <p class="text-xs text-gray-400 mt-0.5">슬롯을 탭하면 이용 내역을 확인할 수 있어요</p>
    </div>

    <!-- 콘텐츠 -->
    <div class="flex-1 overflow-y-auto px-4 py-4 space-y-4">
      <!-- 카드 없음 -->
      <div
        v-if="mockCards.length === 0"
        class="py-16 flex flex-col items-center text-center"
      >
        <div class="w-14 h-14 bg-gray-100 rounded-full flex items-center justify-center mb-3">
          <svg class="w-7 h-7 text-gray-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2"
              d="M3 10h18M7 15h1m4 0h1m-7 4h12a3 3 0 003-3V8a3 3 0 00-3-3H6a3 3 0 00-3 3v8a3 3 0 003 3z" />
          </svg>
        </div>
        <p class="text-sm text-gray-500 font-medium">등록된 카드가 없어요</p>
        <p class="text-xs text-gray-400 mt-1">설정에서 카드 정산일/결제일을 등록하세요</p>
      </div>

      <!-- 카드 목록 -->
      <CardBillingCard
        v-for="card in mockCards"
        :key="card.id"
        :card="card"
      />

      <!-- 하단 여백 (탭바 overlap 방지) -->
      <div class="h-2" />
    </div>
  </div>
</template>
