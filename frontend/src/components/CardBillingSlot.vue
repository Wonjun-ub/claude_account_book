<script setup lang="ts">
// Sprint 6 Step 1 목업용 컴포넌트
// Step 3에서 실제 API 데이터를 props로 받는 구조는 동일하게 유지됨
import { computed } from 'vue'
import type { MockCardSlot } from '@/mocks/cardBilling.mock'
import { calcDDay } from '@/mocks/cardBilling.mock'
import dayjs from 'dayjs'

const props = defineProps<{
  slot: MockCardSlot
  active: boolean   // 드릴다운 열린 슬롯 여부
}>()

const emit = defineEmits<{
  click: []
}>()

const dDay = computed(() => calcDDay(props.slot.paymentDueDate))
const dDayClass = computed(() => {
  const d = dDay.value
  if (d === 'D-day') return 'bg-red-500 text-white'
  if (d.startsWith('D-')) {
    const n = parseInt(d.slice(2))
    if (n <= 7) return 'bg-orange-500 text-white'
  }
  return 'bg-gray-100 text-gray-600'
})

const periodLabel = computed(() => {
  const from = dayjs(props.slot.periodFrom).format('M/D')
  const to = dayjs(props.slot.periodTo).format('M/D')
  return `${from} ~ ${to}`
})

const paymentDateLabel = computed(() =>
  dayjs(props.slot.paymentDueDate).format('M월 D일 결제')
)

const statusBadge = computed(() =>
  props.slot.isConfirmed ? '확정' : '이용 중'
)
</script>

<template>
  <button
    type="button"
    class="flex-1 rounded-xl p-3 text-left transition-colors"
    :class="active ? 'bg-blue-50 ring-1 ring-blue-300' : 'bg-gray-50'"
    @click="emit('click')"
  >
    <!-- 슬롯 레이블 + 상태 -->
    <div class="flex items-center justify-between mb-2">
      <span class="text-xs font-semibold text-gray-700">{{ slot.label }}</span>
      <span
        class="text-[10px] px-1.5 py-0.5 rounded-full font-medium"
        :class="slot.isConfirmed ? 'bg-gray-200 text-gray-600' : 'bg-blue-100 text-blue-600'"
      >
        {{ statusBadge }}
      </span>
    </div>

    <!-- 청구 기간 -->
    <p class="text-[11px] text-gray-400 mb-1">{{ periodLabel }}</p>

    <!-- 결제일 + D-day -->
    <div class="flex items-center gap-1.5 mb-2">
      <span class="text-[11px] text-gray-500">{{ paymentDateLabel }}</span>
      <span class="text-[10px] px-1.5 py-0.5 rounded-full font-bold" :class="dDayClass">
        {{ dDay }}
      </span>
    </div>

    <!-- 금액 -->
    <p class="text-base font-bold text-gray-900">
      {{ slot.totalAmount === 0 ? '—' : slot.totalAmount.toLocaleString() + '원' }}
    </p>

    <!-- 거래 건수 -->
    <p v-if="slot.transactionCount > 0" class="text-[11px] text-gray-400 mt-0.5">
      {{ slot.transactionCount }}건
    </p>
    <p v-else class="text-[11px] text-gray-300 mt-0.5">거래 없음</p>
  </button>
</template>
