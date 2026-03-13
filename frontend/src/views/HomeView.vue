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
const keyword = ref('')
const selectedCategory = ref<string | null>(null)

async function loadData() {
  loadingTx.value = true
  selectedCategory.value = null
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

// 현재 거래 목록에서 유니크 카테고리 추출
const categoryChips = computed(() => {
  const seen = new Set<string>()
  const result: { name: string; type: string }[] = []
  for (const tx of transactions.value) {
    if (!seen.has(tx.categoryName)) {
      seen.add(tx.categoryName)
      result.push({ name: tx.categoryName, type: tx.type })
    }
  }
  return result
})

// 카테고리 필터 적용
const filteredTransactions = computed(() => {
  if (!selectedCategory.value) return transactions.value
  return transactions.value.filter(tx => tx.categoryName === selectedCategory.value)
})

watch([() => store.currentYear, () => store.currentMonth], loadData)
onMounted(loadData)

function openAdd() {
  editTarget.value = null
  showModal.value = true
}

function openEdit(tx: Transaction) {
  editTarget.value = tx
  showModal.value = true
}

async function handleDelete(id: number) {
  if (!await showConfirm('거래를 삭제하시겠습니까?')) return
  await transactionsApi.delete(id)
  await loadData()
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
          :key="cat.name"
          @click="selectedCategory = selectedCategory === cat.name ? null : cat.name"
          class="flex-shrink-0 text-xs px-3 py-1.5 rounded-full font-medium transition-colors"
          :class="selectedCategory === cat.name
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
                <span class="text-sm font-medium truncate">{{ tx.categoryName }}</span>
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

    <!-- ── 거래 추가 버튼 (고정) ── -->
    <button
      @click="openAdd"
      class="fixed bottom-20 right-4 w-14 h-14 bg-blue-600 text-white rounded-full shadow-lg flex items-center justify-center text-2xl hover:bg-blue-700 z-40"
    >
      +
    </button>

    <!-- 거래 모달 -->
    <TransactionModal
      v-if="showModal"
      :transaction="editTarget"
      @close="showModal = false"
      @saved="() => { showModal = false; loadData() }"
    />
  </div>
</template>
