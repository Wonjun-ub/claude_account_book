<script setup lang="ts">
// Sprint 6 Step 1 목업 — 카드 결제 예정 배너 (반복 예정 배너 아래에 독립 표시)
// Step 3에서 실제 API(card billing summary)로 교체 예정

import { ref, computed } from 'vue'
import dayjs from 'dayjs'

export interface CardFilter {
  id: number
  name: string
  periodFrom: string
  periodTo: string
}

export interface CardBillingSummary extends CardFilter {
  dueDay: number
  dueDate: string
  amount: number
}

const props = defineProps<{
  cards: CardBillingSummary[]
}>()

const emit = defineEmits<{
  'filter-card': [filter: CardFilter]
}>()

const isExpanded = ref(false)

const cardTotal = computed(() =>
  props.cards.reduce((s, c) => s + c.amount, 0)
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
  return 'bg-gray-700 text-gray-300'
}

function selectCard(card: CardBillingSummary) {
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
  <div v-if="cards.length > 0" data-testid="card-billing-widget" class="flex-shrink-0 bg-gray-800 border-b border-gray-700">
    <!-- 아코디언 헤더 -->
    <button
      data-testid="card-billing-toggle"
      class="w-full px-4 py-2.5 flex items-center justify-between"
      @click="isExpanded = !isExpanded"
    >
      <div class="flex items-center gap-2 min-w-0">
        <svg class="w-3.5 h-3.5 text-blue-600 flex-shrink-0" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2"
            d="M3 10h18M7 15h1m4 0h1m-7 4h12a3 3 0 003-3V8a3 3 0 00-3-3H6a3 3 0 00-3 3v8a3 3 0 003 3z" />
        </svg>
        <span class="text-xs font-semibold text-blue-400">{{ dayjs(cards[0]!.dueDate).format('M월') }} 카드 청구 내역</span>
        <span class="text-xs text-blue-400 truncate">
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
    <div v-if="isExpanded" data-testid="card-billing-list" class="px-4 pb-3 space-y-1">
      <button
        v-for="card in cards"
        :key="card.id"
        data-testid="card-billing-card-item"
        :data-card-name="card.name"
        class="w-full flex items-center justify-between bg-gray-800 rounded-lg px-3 py-2 border border-gray-700 text-left active:bg-gray-700 transition-colors"
        @click="selectCard(card)"
      >
        <div class="flex items-center gap-2 min-w-0">
          <div class="w-1.5 h-1.5 rounded-full bg-blue-400 flex-shrink-0" />
          <span class="text-xs font-medium text-gray-100 truncate">{{ card.name }}</span>
          <span class="text-[11px] text-gray-400 flex-shrink-0">결제일 {{ card.dueDay }}일</span>
          <span
            class="text-[10px] px-1 py-0.5 rounded-full font-bold flex-shrink-0"
            :class="dDayClass(card.dueDate)"
          >{{ calcDDay(card.dueDate) }}</span>
        </div>
        <div class="flex items-center gap-1 ml-2 flex-shrink-0">
          <span class="text-xs font-semibold text-red-500">
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
