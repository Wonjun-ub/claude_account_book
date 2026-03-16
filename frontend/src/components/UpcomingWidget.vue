<script setup lang="ts">
// Sprint 6 Step 1 목업 — 예정된 지출 통합 위젯 (반복 예정 + 카드 결제 예정)
// Step 3에서 실제 API(recurring pending + card billing summary)로 교체 예정

import { ref, computed } from 'vue'
import dayjs from 'dayjs'
import { mockCardBillings, type MockCardBilling } from '@/mocks/upcoming.mock'

interface RecurringItem {
  id: number
  categoryName: string
  type: 'Income' | 'Expense'
  amount: number
  dayOfMonth: number
  memo?: string
}

export interface CardFilter {
  id: number
  name: string
  periodFrom: string
  periodTo: string
}

const props = defineProps<{
  recurringItems: RecurringItem[]
  pendingSummary: { income: number; expense: number }
}>()

const emit = defineEmits<{
  'filter-card': [filter: CardFilter | null]
  'recurring-delete': [id: number]
}>()

const isExpanded = ref(false)

const cardTotal = computed(() =>
  mockCardBillings.reduce((s, c) => s + c.amount, 0)
)

const totalCount = computed(() =>
  props.recurringItems.length + mockCardBillings.length
)

const totalExpense = computed(() =>
  props.pendingSummary.expense + cardTotal.value
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

function periodLabel(card: MockCardBilling): string {
  return `${dayjs(card.periodFrom).format('M/D')}~${dayjs(card.periodTo).format('M/D')}`
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
  <div class="flex-shrink-0 bg-amber-50 border-b border-amber-100">
    <!-- 아코디언 헤더 -->
    <button
      class="w-full px-4 py-2.5 flex items-center justify-between"
      @click="isExpanded = !isExpanded"
    >
      <div class="flex items-center gap-2 min-w-0">
        <!-- 캘린더 아이콘 -->
        <svg class="w-4 h-4 text-amber-600 flex-shrink-0" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2"
            d="M8 7V3m8 4V3m-9 8h10M5 21h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v12a2 2 0 002 2z" />
        </svg>
        <span class="text-xs font-semibold text-amber-800">예정된 지출</span>
        <span class="text-xs text-amber-600 flex-shrink-0">{{ totalCount }}건</span>
        <span v-if="totalExpense > 0" class="text-xs font-medium text-red-500 flex-shrink-0">
          -{{ totalExpense.toLocaleString() }}원
        </span>
        <span v-if="pendingSummary.income > 0" class="text-xs text-blue-500 flex-shrink-0">
          (+{{ pendingSummary.income.toLocaleString() }})
        </span>
      </div>
      <svg
        class="w-4 h-4 text-amber-500 flex-shrink-0 transition-transform duration-200"
        :class="isExpanded ? 'rotate-180' : ''"
        fill="none" stroke="currentColor" viewBox="0 0 24 24"
      >
        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 9l-7 7-7-7" />
      </svg>
    </button>

    <!-- 펼쳐진 내용 -->
    <div v-if="isExpanded" class="px-3 pb-3 space-y-3">

      <!-- 반복 예정 섹션 (있을 때만) -->
      <div v-if="recurringItems.length > 0">
        <p class="text-[10px] font-semibold text-amber-600 uppercase tracking-wide px-1 mb-1.5">
          반복 예정 {{ recurringItems.length }}건
        </p>
        <div
          v-for="item in recurringItems"
          :key="item.id"
          class="flex items-center justify-between bg-white rounded-xl px-3 py-2.5 border border-amber-100"
        >
          <div class="flex items-center gap-2 min-w-0">
            <div
              class="w-1.5 h-1.5 rounded-full flex-shrink-0"
              :class="item.type === 'Income' ? 'bg-blue-400' : 'bg-red-400'"
            />
            <span class="text-sm text-gray-700 truncate">{{ item.categoryName }}</span>
            <span class="text-xs text-gray-400 flex-shrink-0">매월 {{ item.dayOfMonth }}일</span>
            <span v-if="item.memo" class="text-xs text-gray-400 truncate">· {{ item.memo }}</span>
          </div>
          <div class="flex items-center gap-2 ml-2 flex-shrink-0">
            <span
              class="text-sm font-semibold"
              :class="item.type === 'Income' ? 'text-blue-600' : 'text-red-500'"
            >
              {{ item.type === 'Income' ? '+' : '-' }}{{ item.amount.toLocaleString() }}원
            </span>
            <button
              class="p-0.5 text-gray-300 hover:text-red-400"
              @click.stop="emit('recurring-delete', item.id)"
            >
              <svg class="w-3.5 h-3.5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12" />
              </svg>
            </button>
          </div>
        </div>
      </div>

      <!-- 카드 결제 예정 섹션 -->
      <div>
        <p class="text-[10px] font-semibold text-amber-600 uppercase tracking-wide px-1 mb-1.5">
          카드 결제 예정
          <span class="text-amber-400 font-normal">· 탭하면 해당 청구 내역만 필터링</span>
        </p>
        <button
          v-for="card in mockCardBillings"
          :key="card.id"
          class="w-full flex items-center justify-between bg-white rounded-xl px-3 py-2.5 border border-amber-100 text-left mb-1.5 last:mb-0 active:bg-amber-50 transition-colors"
          @click="selectCard(card)"
        >
          <div class="flex items-center gap-2 min-w-0">
            <!-- 카드 아이콘 -->
            <svg class="w-4 h-4 text-gray-400 flex-shrink-0" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2"
                d="M3 10h18M7 15h1m4 0h1m-7 4h12a3 3 0 003-3V8a3 3 0 00-3-3H6a3 3 0 00-3 3v8a3 3 0 003 3z" />
            </svg>
            <div class="min-w-0">
              <div class="flex items-center gap-1.5">
                <span class="text-sm font-medium text-gray-800">{{ card.name }}</span>
                <span
                  class="text-[10px] px-1.5 py-0.5 rounded-full font-bold"
                  :class="dDayClass(card.dueDate)"
                >
                  {{ calcDDay(card.dueDate) }}
                </span>
              </div>
              <p class="text-[11px] text-gray-400 mt-0.5">
                결제일 {{ card.dueDay }}일 · {{ periodLabel(card) }}
              </p>
            </div>
          </div>
          <div class="flex items-center gap-1.5 ml-2 flex-shrink-0">
            <span class="text-sm font-bold text-gray-800">
              {{ card.amount.toLocaleString() }}원
            </span>
            <!-- 드릴다운 화살표 -->
            <svg class="w-4 h-4 text-gray-300" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 5l7 7-7 7" />
            </svg>
          </div>
        </button>
      </div>

    </div>
  </div>
</template>
