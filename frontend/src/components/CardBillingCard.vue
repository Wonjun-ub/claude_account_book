<script setup lang="ts">
// Sprint 6 Step 1 목업용 컴포넌트
import { ref, computed } from 'vue'
import type { MockCard } from '@/mocks/cardBilling.mock'
import { mockSlotTransactions } from '@/mocks/cardBilling.mock'
import CardBillingSlot from '@/components/CardBillingSlot.vue'
import dayjs from 'dayjs'

const props = defineProps<{
  card: MockCard
}>()

// 열린 슬롯 인덱스 (null = 드릴다운 닫힘)
const activeSlotIndex = ref<1 | 2 | null>(null)

function toggleSlot(idx: 1 | 2) {
  activeSlotIndex.value = activeSlotIndex.value === idx ? null : idx
}

const activeTransactions = computed(() => {
  if (!activeSlotIndex.value) return []
  return mockSlotTransactions[`${props.card.id}-${activeSlotIndex.value}`] ?? []
})

const activeSlot = computed(() =>
  activeSlotIndex.value ? props.card.slots[activeSlotIndex.value - 1] : null
)

// 카테고리별 소계
const categoryTotals = computed(() => {
  const map = new Map<string, number>()
  for (const tx of activeTransactions.value) {
    map.set(tx.categoryName, (map.get(tx.categoryName) ?? 0) + tx.amount)
  }
  return [...map.entries()].sort((a, b) => b[1] - a[1])
})
</script>

<template>
  <div class="bg-white rounded-2xl shadow-sm overflow-hidden">
    <!-- 카드 헤더 -->
    <div class="px-4 pt-4 pb-3 border-b border-gray-100">
      <div class="flex items-center justify-between">
        <div class="flex items-center gap-2">
          <!-- 카드 아이콘 -->
          <div class="w-8 h-8 bg-blue-600 rounded-lg flex items-center justify-center">
            <svg class="w-4 h-4 text-white" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2"
                d="M3 10h18M7 15h1m4 0h1m-7 4h12a3 3 0 003-3V8a3 3 0 00-3-3H6a3 3 0 00-3 3v8a3 3 0 003 3z" />
            </svg>
          </div>
          <span class="font-semibold text-gray-800">{{ card.name }}</span>
        </div>
        <span class="text-xs text-gray-400">
          정산일 {{ card.billingCutoffDay }}일 · 결제일 {{ card.paymentDueDay }}일
        </span>
      </div>
    </div>

    <!-- 2슬롯 나란히 -->
    <div class="flex gap-2 p-3">
      <CardBillingSlot
        :slot="card.slots[0]"
        :active="activeSlotIndex === 1"
        @click="toggleSlot(1)"
      />
      <CardBillingSlot
        :slot="card.slots[1]"
        :active="activeSlotIndex === 2"
        @click="toggleSlot(2)"
      />
    </div>

    <!-- 드릴다운 패널 (슬롯 클릭 시 펼침) -->
    <Transition name="slide-down">
      <div v-if="activeSlotIndex && activeSlot" class="border-t border-gray-100">
        <!-- 드릴다운 헤더 -->
        <div class="px-4 py-3 flex items-center justify-between bg-gray-50">
          <div>
            <p class="text-sm font-semibold text-gray-700">{{ activeSlot.label }} 상세</p>
            <p class="text-xs text-gray-400 mt-0.5">
              합계 {{ activeSlot.totalAmount.toLocaleString() }}원 ({{ activeTransactions.length }}건)
            </p>
          </div>
          <button
            class="text-gray-400 p-1"
            @click="activeSlotIndex = null"
          >
            <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 15l7-7 7 7" />
            </svg>
          </button>
        </div>

        <!-- 거래 없음 -->
        <div
          v-if="activeTransactions.length === 0"
          class="py-8 text-center text-gray-400 text-sm"
        >
          이용 내역이 없습니다
        </div>

        <!-- 카테고리 소계 -->
        <div v-if="categoryTotals.length > 0" class="px-4 py-2 flex flex-wrap gap-2 border-b border-gray-50">
          <span
            v-for="[cat, total] in categoryTotals"
            :key="cat"
            class="text-xs bg-blue-50 text-blue-600 px-2 py-0.5 rounded-full"
          >
            {{ cat }} {{ total.toLocaleString() }}원
          </span>
        </div>

        <!-- 거래 목록 -->
        <ul v-if="activeTransactions.length > 0" class="divide-y divide-gray-50">
          <li
            v-for="(tx, i) in activeTransactions"
            :key="i"
            class="flex items-center justify-between px-4 py-2.5"
          >
            <div>
              <p class="text-sm text-gray-800">{{ tx.memo }}</p>
              <p class="text-xs text-gray-400 mt-0.5">
                {{ dayjs(tx.date).format('M/D') }} · {{ tx.categoryName }}
              </p>
            </div>
            <span class="text-sm font-medium text-gray-700">
              {{ tx.amount.toLocaleString() }}원
            </span>
          </li>
        </ul>
      </div>
    </Transition>
  </div>
</template>

<style scoped>
.slide-down-enter-active,
.slide-down-leave-active {
  transition: all 0.2s ease;
  overflow: hidden;
}
.slide-down-enter-from,
.slide-down-leave-to {
  max-height: 0;
  opacity: 0;
}
.slide-down-enter-to,
.slide-down-leave-from {
  max-height: 600px;
  opacity: 1;
}
</style>
