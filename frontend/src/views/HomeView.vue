<script setup lang="ts">
import { ref, computed, watch, onMounted } from 'vue'
import dayjs from 'dayjs'
import { useAppStore } from '@/stores/app'
import { useDialog } from '@/composables/useDialog'
import { transactionsApi, summaryApi } from '@/api'
import type { Transaction, MonthlySummary } from '@/types'
import TransactionModal from '@/components/TransactionModal.vue'

const store = useAppStore()
const { showConfirm } = useDialog()

const transactions = ref<Transaction[]>([])
const summary = ref<MonthlySummary | null>(null)
const loadingTx = ref(false)
const showModal = ref(false)
const editTarget = ref<Transaction | null>(null)
const initialType = ref<'Income' | 'Expense'>('Expense')
const keyword = ref('')
const selectedCategory = ref<number | null>(null)
const fabOpen = ref(false)

async function loadData() {
  loadingTx.value = true
  selectedCategory.value = null  // 월 변경 시 필터 초기화

  try {
    const params: Record<string, string | number> = {
      year: store.currentYear,
      month: store.currentMonth,
    }
    if (keyword.value) params['keyword'] = keyword.value

    const [txList, sum] = await Promise.all([
      transactionsApi.getAll(params),
      summaryApi.getMonthly(store.currentYear, store.currentMonth),
    ])

    transactions.value = txList
    summary.value = sum
  } finally {
    loadingTx.value = false
  }
}

// store에서 카테고리 이름 조회 (API 응답의 영문 테스트 데이터와 무관하게 항상 최신 한글 이름 사용)
function getCategoryName(categoryId: number, fallback: string): string {
  return store.categories.find(c => c.id === categoryId)?.name ?? fallback
}

// 현재 거래 목록에서 유니크 카테고리 추출 (ID 기준 중복 제거, store에서 최신 이름 사용)
const categoryChips = computed(() => {
  const seen = new Set<number>()
  const result: { id: number; name: string; type: string }[] = []

  for (const tx of transactions.value) {
    if (!seen.has(tx.categoryId)) {
      seen.add(tx.categoryId)
      const name = getCategoryName(tx.categoryId, tx.categoryName)
      result.push({ id: tx.categoryId, name, type: tx.type })
    }
  }

  return result
})

// 카테고리 필터 적용 (ID 기준)
const filteredTransactions = computed(() => {
  if (selectedCategory.value === null) return transactions.value
  return transactions.value.filter(tx => tx.categoryId === selectedCategory.value)
})

watch([() => store.currentYear, () => store.currentMonth], loadData)
onMounted(loadData)

function openAdd(type: 'Income' | 'Expense') {
  editTarget.value = null
  initialType.value = type
  fabOpen.value = false

  showModal.value = true
}

function openEdit(tx: Transaction) {
  editTarget.value = tx
  showModal.value = true
}

async function handleDelete(id: number) {
  if (!await showConfirm('거래를 삭제하시겠습니까?')) return

  const tx = transactions.value.find(t => t.id === id)
  if (!tx) return

  // 프론트에서 즉시 제거 (서버 응답 기다리지 않음)
  transactions.value = transactions.value.filter(t => t.id !== id)

  // 요약 즉시 업데이트 (합산 포함 거래만)
  if (summary.value && tx.isIncludedInTotal) {
    if (tx.type === 'Income') {
      summary.value = { ...summary.value, totalIncome: summary.value.totalIncome - tx.amount, balance: summary.value.balance - tx.amount }
    } else {
      summary.value = { ...summary.value, totalExpense: summary.value.totalExpense - tx.amount, balance: summary.value.balance + tx.amount }
    }
  }

  // 서버에 삭제 요청 (백그라운드)
  await transactionsApi.delete(id)
}

function formatAmount(amount: number, type: string) {
  const sign = type === 'Income' ? '+' : '-'
  return `${sign}${amount.toLocaleString()}원`
}

function formatDate(dateStr: string) {
  return dayjs(dateStr).format('MM/DD (ddd)')
}

function groupByDate(txs: Transaction[]) {
  const map = new Map<string, Transaction[]>()

  for (const tx of txs) {
    const key = dayjs(tx.date).format('YYYY-MM-DD')
    if (!map.has(key)) map.set(key, [])
    map.get(key)!.push(tx)
  }

  return Array.from(map.entries()).sort((a, b) => b[0].localeCompare(a[0]))
}
</script>

<template>
  <!-- 화면 전체를 채우는 flex 컬럼 컨테이너 -->
  <div class="h-full flex flex-col max-w-lg mx-auto">

    <!-- ── 고정 헤더 영역 ── -->
    <div class="flex-shrink-0">
      <!-- 월 네비게이터 + 요약 카드 -->
      <div class="bg-blue-600 text-white px-4 pt-10 pb-6">
        <div class="flex items-center justify-between mb-4">
          <button @click="store.prevMonth()" class="p-1 rounded-full hover:bg-blue-500">
            <svg class="w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 19l-7-7 7-7" />
            </svg>
          </button>
          <span class="text-lg font-semibold">{{ store.currentMonthLabel }}</span>
          <button @click="store.nextMonth()" class="p-1 rounded-full hover:bg-blue-500">
            <svg class="w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 5l7 7-7 7" />
            </svg>
          </button>
        </div>
        <div class="grid grid-cols-3 gap-2 text-center" v-if="summary">
          <div class="bg-blue-500 rounded-xl p-3">
            <div class="text-blue-200 text-xs mb-1">수입</div>
            <div class="font-bold text-sm">{{ summary.totalIncome.toLocaleString() }}원</div>
          </div>
          <div class="bg-blue-500 rounded-xl p-3">
            <div class="text-blue-200 text-xs mb-1">지출</div>
            <div class="font-bold text-sm">{{ summary.totalExpense.toLocaleString() }}원</div>
          </div>
          <div class="bg-blue-500 rounded-xl p-3">
            <div class="text-blue-200 text-xs mb-1">잔액</div>
            <div class="font-bold text-sm">{{ summary.balance.toLocaleString() }}원</div>
          </div>
        </div>
      </div>

      <!-- 검색바 -->
      <div class="px-4 py-2 bg-white border-b border-gray-100 flex gap-2">
        <input
          v-model="keyword"
          @keyup.enter="loadData"
          type="text"
          placeholder="메모 검색..."
          class="flex-1 text-sm border border-gray-200 rounded-lg px-3 py-1.5 focus:outline-none focus:border-blue-400"
        />
        <button @click="loadData" class="text-sm text-blue-600 font-medium px-2">검색</button>
      </div>

      <!-- 카테고리 필터 칩 -->
      <div v-if="categoryChips.length > 0" class="flex gap-2 px-4 py-2 overflow-x-auto bg-white border-b border-gray-100 scrollbar-hide">
        <button
          @click="selectedCategory = null"
          class="flex-shrink-0 text-xs px-3 py-1.5 rounded-full font-medium transition-colors"
          :class="selectedCategory === null
            ? 'bg-blue-600 text-white'
            : 'bg-gray-100 text-gray-600'"
        >
          전체
        </button>
        <button
          v-for="cat in categoryChips"
          :key="cat.id"
          @click="selectedCategory = selectedCategory === cat.id ? null : cat.id"
          class="flex-shrink-0 text-xs px-3 py-1.5 rounded-full font-medium transition-colors"
          :class="selectedCategory === cat.id
            ? (cat.type === 'Income' ? 'bg-blue-600 text-white' : 'bg-red-500 text-white')
            : 'bg-gray-100 text-gray-600'"
        >
          {{ cat.name }}
        </button>
      </div>
    </div>

    <!-- ── 스크롤 가능한 거래 목록 ── -->
    <div class="flex-1 overflow-y-auto px-4 py-2">
      <div v-if="loadingTx" class="py-10 text-center text-gray-400 text-sm">불러오는 중...</div>
      <div v-else-if="filteredTransactions.length === 0" class="py-10 text-center text-gray-400 text-sm">
        거래 내역이 없습니다
      </div>

      <template v-else>
        <div v-for="[date, txs] in groupByDate(filteredTransactions)" :key="date" class="mb-4">
          <div class="flex items-center justify-between mb-1">
            <span class="text-xs font-semibold text-gray-500">{{ formatDate(date) }}</span>
            <span class="text-xs text-gray-400">
              {{ txs.filter(t => t.type === 'Income').reduce((s, t) => s + t.amount, 0) > 0
                ? '+' + txs.filter(t => t.type === 'Income').reduce((s, t) => s + t.amount, 0).toLocaleString()
                : '' }}
              {{ txs.filter(t => t.type === 'Expense').reduce((s, t) => s + t.amount, 0) > 0
                ? '-' + txs.filter(t => t.type === 'Expense').reduce((s, t) => s + t.amount, 0).toLocaleString()
                : '' }}
            </span>
          </div>

          <div
            v-for="tx in txs"
            :key="tx.id"
            class="bg-white rounded-xl p-3 mb-2 flex items-center shadow-sm"
            @click="openEdit(tx)"
          >
            <div class="flex-1 min-w-0">
              <div class="flex items-center gap-2">
                <span class="text-sm font-medium truncate">{{ getCategoryName(tx.categoryId, tx.categoryName) }}</span>
                <span v-if="!tx.isIncludedInTotal" class="text-xs bg-gray-100 text-gray-500 px-1.5 py-0.5 rounded">제외</span>
                <span v-if="tx.recurringTransactionId" class="text-xs bg-blue-50 text-blue-500 px-1.5 py-0.5 rounded">반복</span>
              </div>
              <div class="text-xs text-gray-400 mt-0.5">
                {{ tx.paymentMethodName }}
                <span v-if="tx.memo"> · {{ tx.memo }}</span>
              </div>
            </div>
            <div class="flex items-center gap-2">
              <span :class="tx.type === 'Income' ? 'text-blue-600' : 'text-red-500'" class="font-semibold text-sm">
                {{ formatAmount(tx.amount, tx.type) }}
              </span>
              <button @click.stop="handleDelete(tx.id)" class="text-gray-300 hover:text-red-400 p-1">
                <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12" />
                </svg>
              </button>
            </div>
          </div>
        </div>
      </template>
    </div>

    <!-- ── FAB 스피드 다이얼 ── -->
    <div class="fixed bottom-20 right-4 flex flex-col items-end gap-3 z-40">
      <!-- 수입/지출 버튼 (펼쳐졌을 때) -->
      <Transition
        enter-active-class="transition-all duration-200"
        enter-from-class="opacity-0 translate-y-2"
        enter-to-class="opacity-100 translate-y-0"
        leave-active-class="transition-all duration-150"
        leave-from-class="opacity-100 translate-y-0"
        leave-to-class="opacity-0 translate-y-2"
      >
        <div v-if="fabOpen" class="flex flex-col items-end gap-2">
          <button
            @click="openAdd('Income')"
            class="flex items-center gap-2 bg-white text-blue-600 border border-blue-200 shadow-md rounded-full px-4 py-2 text-sm font-semibold"
          >
            수입 +
          </button>
          <button
            @click="openAdd('Expense')"
            class="flex items-center gap-2 bg-white text-red-500 border border-red-200 shadow-md rounded-full px-4 py-2 text-sm font-semibold"
          >
            지출 +
          </button>
        </div>
      </Transition>

      <!-- 메인 FAB -->
      <button
        @click="fabOpen = !fabOpen"
        class="w-14 h-14 bg-blue-600 text-white rounded-full shadow-lg flex items-center justify-center transition-transform duration-200"
        :class="fabOpen ? 'rotate-45' : ''"
      >
        <svg class="w-7 h-7" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4v16m8-8H4" />
        </svg>
      </button>
    </div>

    <!-- 배경 딤 (FAB 열렸을 때) -->
    <div v-if="fabOpen" class="fixed inset-0 z-30" @click="fabOpen = false" />

    <!-- 거래 모달 -->
    <TransactionModal
      v-if="showModal"
      :transaction="editTarget"
      :initialType="initialType"
      @close="showModal = false"
      @saved="() => { showModal = false; loadData() }"
    />
  </div>
</template>
