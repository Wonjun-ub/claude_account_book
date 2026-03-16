<script setup lang="ts">
// Sprint 6 Step 1 목업 — 카드 결제 예정 배너 (반복 예정 배너 아래에 독립 표시)
// Step 3에서 실제 API(card billing summary)로 교체 예정

import { ref, computed } from 'vue'
import dayjs from 'dayjs'
import { mockCardBillings, type MockCardBilling } from '@/mocks/upcoming.mock'

export interface CardFilter {
  id: number
  name: string
  periodFrom: string
  periodTo: string
}

const emit = defineEmits<{
  'filter-card': [filter: CardFilter | null]
}>()

const isExpanded = ref(false)

const cardTotal = computed(() =>
  mockCardBillings.reduce((s, c) => s + c.amount, 0)
)

function calcDDay(dueDate: string): string {
  const diff = dayjs(dueDate).diff(dayjs().startOf('day'), 'day')
  if (diff === 0) return 'D-day'
  if (diff > 0) return `D-${diff}`
  return `D+${Math.abs(diff)}`
}

function dDayClass(dueDate: string): string {
  const diff = dayjs(dueDate).diff(dayjs().startOf('day'), 'day')
  if (diff === 0) return 'bg-red-500 text-white'
  if (diff > 0 && diff <= 7) return 'bg-orange-500 text-white'
  if (diff < 0) return 'bg-gray-300 text-gray-600'
  return 'bg-gray-100 text-gray-600'
}

function selectCard(card: MockCardBilling) {
  emit('filter-card', {
    id: card.id,
    name: card.name,
    periodFrom: card.periodFrom,
    periodTo: card.periodTo,
  })
  isExpanded.value = false
}
</script>

<template>
  <div class="flex-shrink-0 bg-blue-50 border-b border-blue-100">
    <!-- 아코디언 헤더 -->
    <button
      class="w-full px-4 py-2.5 flex items-center justify-between"
      @click="isExpanded = !isExpanded"
    >
      <div class="flex items-center gap-2 min-w-0">
        <svg class="w-3.5 h-3.5 text-blue-600 flex-shrink-0" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2"
            d="M3 10h18M7 15h1m4 0h1m-7 4h12a3 3 0 003-3V8a3 3 0 00-3-3H6a3 3 0 00-3 3v8a3 3 0 003 3z" />
        </svg>
        <span class="text-xs font-semibold text-blue-700">카드 결제 예정 {{ mockCardBillings.length }}건</span>
        <span class="text-xs text-blue-600 truncate">
          총 {{ cardTotal.toLocaleString() }}원
        </span>
      </div>
      <svg
        class="w-4 h-4 text-blue-400 flex-shrink-0 transition-transform duration-200"
        :class="isExpanded ? 'rotate-180' : ''"
        fill="none" stroke="currentColor" viewBox="0 0 24 24"
      >
        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 9l-7 7-7-7" />
      </svg>
    </button>

    <!-- 카드 목록 -->
    <div v-if="isExpanded" class="px-4 pb-3 space-y-2">
      <button
        v-for="card in mockCardBillings"
        :key="card.id"
        class="w-full flex items-center justify-between bg-white rounded-xl px-3 py-2.5 border border-blue-100 text-left active:bg-blue-50 transition-colors"
        @click="selectCard(card)"
      >
        <div class="flex items-center gap-2 min-w-0">
          <div class="w-1.5 h-1.5 rounded-full bg-blue-400 flex-shrink-0" />
          <span class="text-sm text-gray-700 truncate">{{ card.name }}</span>
          <span class="text-xs text-gray-400 flex-shrink-0">결제일 {{ card.dueDay }}일</span>
          <span
            class="text-[10px] px-1.5 py-0.5 rounded-full font-bold flex-shrink-0"
            :class="dDayClass(card.dueDate)"
          >{{ calcDDay(card.dueDate) }}</span>
        </div>
        <div class="flex items-center gap-1 ml-2 flex-shrink-0">
          <span class="text-sm font-semibold text-red-500">
            -{{ card.amount.toLocaleString() }}원
          </span>
          <svg class="w-3.5 h-3.5 text-gray-300" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 5l7 7-7 7" />
          </svg>
        </div>
      </button>
    </div>
  </div>
</template>
