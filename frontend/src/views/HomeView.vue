<script setup lang="ts">
import { ref, computed, watch, onMounted } from 'vue'
import dayjs from 'dayjs'
import { useAppStore } from '@/stores/app'
import { useDialog } from '@/composables/useDialog'
import { transactionsApi, summaryApi, recurringApi, installmentApi } from '@/api'
import type { Transaction, MonthlySummary, RecurringTransaction } from '@/types'
import TransactionModal from '@/components/TransactionModal.vue'
import DeleteOptionSheet from '@/components/DeleteOptionSheet.vue'

const store = useAppStore()
const { showConfirm, showAlert } = useDialog()

const transactions = ref<Transaction[]>([])
const summary = ref<MonthlySummary | null>(null)
const pendingRecurring = ref<RecurringTransaction[]>([])
const loadingTx = ref(false)
const showModal = ref(false)
const editTarget = ref<Transaction | null>(null)
const keyword = ref('')
const selectedCategory = ref<number | null>(null)

// 삭제 시트
const deleteSheetTx = ref<Transaction | null>(null)
const deleteSheetType = ref<'installment' | 'recurring'>('installment')

// 반복 예정 배너 펼치기/접기
const showPendingBanner = ref(false)

async function loadData() {
  loadingTx.value = true
  selectedCategory.value = null  // 월 변경 시 필터 초기화

  try {
    const params: Record<string, string | number> = {
      year: store.currentYear,
      month: store.currentMonth,
    }
    if (keyword.value) params['keyword'] = keyword.value

    const [txList, sum, pending] = await Promise.all([
      transactionsApi.getAll(params),
      summaryApi.getMonthly(store.currentYear, store.currentMonth),
      recurringApi.getPending(store.currentYear, store.currentMonth),
    ])

    transactions.value = txList
    summary.value = sum
    pendingRecurring.value = pending
  } finally {
    loadingTx.value = false
  }
}

// store에서 카테고리 이름 조회
function getCategoryName(categoryId: number, fallback: string): string {
  return store.categories.find(c => c.id === categoryId)?.name ?? fallback
}

// 현재 거래 목록에서 유니크 카테고리 추출
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

// 카테고리 필터 적용
const filteredTransactions = computed(() => {
  if (selectedCategory.value === null) return transactions.value
  return transactions.value.filter(tx => tx.categoryId === selectedCategory.value)
})

// 반복 예정 요약 (수입/지출 합계)
const pendingSummary = computed(() => {
  const income = pendingRecurring.value
    .filter(r => {
      const cat = store.categories.find(c => c.id === r.categoryId)
      return cat?.type === 'Income'
    })
    .reduce((s, r) => s + r.amount, 0)
  const expense = pendingRecurring.value
    .filter(r => {
      const cat = store.categories.find(c => c.id === r.categoryId)
      return cat?.type === 'Expense'
    })
    .reduce((s, r) => s + r.amount, 0)
  return { income, expense }
})

// 기간 표시 (monthStartDay != 1일 때)
const periodRange = computed(() => {
  if (!summary.value) return null
  const start = dayjs(summary.value.periodStart)
  const end = dayjs(summary.value.periodEnd)
  // 1일 시작이면 표시 안 함
  if (start.date() === 1 && end.date() >= 28) return null
  return `${start.format('M/D')} ~ ${end.format('M/D')}`
})

watch([() => store.currentYear, () => store.currentMonth], loadData)
onMounted(async () => {
  if (store.categories.length === 0) {
    await store.loadMasterData()
  }
  loadData()
})

function openAdd() {
  editTarget.value = null
  showModal.value = true
}

function openEdit(tx: Transaction) {
  editTarget.value = tx
  showModal.value = true
}

// 삭제 클릭: 종류에 따라 분기
async function onDeleteClick(tx: Transaction) {
  if (tx.installmentTransactionId) {
    // 할부 → bottom sheet
    deleteSheetTx.value = tx
    deleteSheetType.value = 'installment'
  } else if (tx.recurringTransactionId) {
    // 반복 → bottom sheet
    deleteSheetTx.value = tx
    deleteSheetType.value = 'recurring'
  } else {
    // 일반 → confirm
    await handleDeleteSingle(tx)
  }
}

// 반복 예정 배너 X 클릭
async function onPendingDeleteClick(master: RecurringTransaction) {
  // synthetic 거래 객체 생성 → deleteSheet 재사용
  const syntheticTx: Transaction = {
    id: -1,
    amount: master.amount,
    date: `${store.currentYear}-${String(store.currentMonth).padStart(2, '0')}-${String(master.dayOfMonth).padStart(2, '0')}`,
    type: store.categories.find(c => c.id === master.categoryId)?.type === 'Income' ? 'Income' : 'Expense',
    categoryId: master.categoryId,
    categoryName: master.categoryName,
    paymentMethodId: master.paymentMethodId,
    paymentMethodName: master.paymentMethodName,
    isIncludedInTotal: true,
    recurringTransactionId: master.id,
  }
  deleteSheetTx.value = syntheticTx
  deleteSheetType.value = 'recurring'
}

async function handleDeleteSingle(tx: Transaction) {
  if (!await showConfirm('거래를 삭제하시겠습니까?')) return

  // 낙관적 UI 업데이트
  transactions.value = transactions.value.filter(t => t.id !== tx.id)
  if (summary.value && tx.isIncludedInTotal) {
    if (tx.type === 'Income') {
      summary.value = { ...summary.value, totalIncome: summary.value.totalIncome - tx.amount, balance: summary.value.balance - tx.amount }
    } else {
      summary.value = { ...summary.value, totalExpense: summary.value.totalExpense - tx.amount, balance: summary.value.balance + tx.amount }
    }
  }

  await transactionsApi.delete(tx.id)
}

// 할부 삭제 핸들러
async function handleInstallmentDelete(mode: 'all' | 'fromHere' | 'single') {
  const tx = deleteSheetTx.value
  if (!tx?.installmentTransactionId) return
  deleteSheetTx.value = null

  try {
    await installmentApi.deleteWithMode(
      tx.installmentTransactionId,
      mode,
      mode !== 'all' ? tx.installmentSequence : undefined
    )
    await loadData()
  } catch (e: unknown) {
    await showAlert(e instanceof Error ? e.message : '삭제 중 오류가 발생했습니다.')
  }
}

// 반복 삭제 핸들러
async function handleRecurringDelete(mode: 'all' | 'fromHere' | 'single') {
  const tx = deleteSheetTx.value
  if (!tx?.recurringTransactionId) return
  deleteSheetTx.value = null

  try {
    if (mode === 'all') {
      await recurringApi.deleteWithMode(tx.recurringTransactionId, 'all')
    } else if (mode === 'fromHere') {
      await recurringApi.deleteWithMode(tx.recurringTransactionId, 'fromHere', { date: tx.date })
    } else {
      // single = skipMonth (이번 달 스킵)
      await recurringApi.deleteWithMode(tx.recurringTransactionId, 'skipMonth', {
        year: store.currentYear,
        month: store.currentMonth,
      })
      // 실제 거래가 있으면 삭제
      if (tx.id !== -1) {
        await transactionsApi.delete(tx.id)
      }
    }
    await loadData()
  } catch (e: unknown) {
    await showAlert(e instanceof Error ? e.message : '삭제 중 오류가 발생했습니다.')
  }
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
          <div class="text-center">
            <div class="text-lg font-semibold">{{ store.currentMonthLabel }}</div>
            <div v-if="periodRange" class="text-xs text-blue-200 mt-0.5">{{ periodRange }}</div>
          </div>
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
            <div class="text-blue-300 text-xs mt-0.5">{{ summary.incomeCount }}건</div>
          </div>
          <div class="bg-blue-500 rounded-xl p-3">
            <div class="text-blue-200 text-xs mb-1">지출</div>
            <div class="font-bold text-sm">{{ summary.totalExpense.toLocaleString() }}원</div>
            <div class="text-blue-300 text-xs mt-0.5">{{ summary.expenseCount }}건</div>
          </div>
          <div class="bg-blue-500 rounded-xl p-3">
            <div class="text-blue-200 text-xs mb-1">잔액</div>
            <div class="font-bold text-sm">{{ summary.balance.toLocaleString() }}원</div>
            <div class="text-blue-300 text-xs mt-0.5">합산 기준</div>
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

      <!-- 반복 예정 배너 -->
      <div v-if="pendingRecurring.length > 0" class="flex-shrink-0 bg-teal-50 border-b border-teal-100">
        <button
          @click="showPendingBanner = !showPendingBanner"
          class="w-full px-4 py-2.5 flex items-center justify-between"
        >
          <div class="flex items-center gap-2 min-w-0">
            <svg class="w-3.5 h-3.5 text-teal-600 flex-shrink-0" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 4v5h.582m15.356 2A8.001 8.001 0 004.582 9m0 0H9m11 11v-5h-.581m0 0a8.003 8.003 0 01-15.357-2m15.357 2H15"/>
            </svg>
            <span class="text-xs font-semibold text-teal-700">반복 예정 {{ pendingRecurring.length }}건</span>
            <span class="text-xs text-teal-600 truncate">
              <template v-if="pendingSummary.income > 0">수입 +{{ pendingSummary.income.toLocaleString() }}원</template>
              <template v-if="pendingSummary.income > 0 && pendingSummary.expense > 0"> · </template>
              <template v-if="pendingSummary.expense > 0">지출 -{{ pendingSummary.expense.toLocaleString() }}원</template>
            </span>
          </div>
          <svg
            :class="showPendingBanner ? 'rotate-180' : ''"
            class="w-4 h-4 text-teal-500 flex-shrink-0 transition-transform"
            fill="none" stroke="currentColor" viewBox="0 0 24 24"
          >
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 9l-7 7-7-7"/>
          </svg>
        </button>
        <div v-if="showPendingBanner" class="px-4 pb-3 space-y-2">
          <div
            v-for="master in pendingRecurring"
            :key="master.id"
            class="flex items-center justify-between bg-white rounded-xl px-3 py-2.5 border border-teal-100"
          >
            <div class="flex items-center gap-2 min-w-0">
              <div
                class="w-1.5 h-1.5 rounded-full flex-shrink-0"
                :class="store.categories.find(c => c.id === master.categoryId)?.type === 'Income' ? 'bg-blue-400' : 'bg-red-400'"
              />
              <span class="text-sm text-gray-700 truncate">{{ getCategoryName(master.categoryId, master.categoryName) }}</span>
              <span class="text-xs text-gray-400 flex-shrink-0">매월 {{ master.dayOfMonth }}일</span>
              <span v-if="master.memo" class="text-xs text-gray-400 truncate">· {{ master.memo }}</span>
            </div>
            <div class="flex items-center gap-1 ml-2 flex-shrink-0">
              <span
                :class="store.categories.find(c => c.id === master.categoryId)?.type === 'Income' ? 'text-blue-600' : 'text-red-500'"
                class="text-sm font-semibold"
              >
                {{ store.categories.find(c => c.id === master.categoryId)?.type === 'Income' ? '+' : '-' }}{{ master.amount.toLocaleString() }}원
              </span>
              <button @click="onPendingDeleteClick(master)" class="p-1 text-gray-300 hover:text-red-400">
                <svg class="w-3.5 h-3.5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12"/>
                </svg>
              </button>
            </div>
          </div>
        </div>
      </div>

      <!-- 카테고리 필터 칩 -->
      <div v-if="categoryChips.length > 0" class="flex gap-2 px-4 py-2 overflow-x-auto bg-white border-b border-gray-100 scrollbar-hide">
        <button
          @click="selectedCategory = null"
          class="flex-shrink-0 text-xs px-3 py-1.5 rounded-full font-medium transition-colors"
          :class="selectedCategory === null ? 'bg-blue-600 text-white' : 'bg-gray-100 text-gray-600'"
        >전체</button>
        <button
          v-for="cat in categoryChips"
          :key="cat.id"
          @click="selectedCategory = selectedCategory === cat.id ? null : cat.id"
          class="flex-shrink-0 text-xs px-3 py-1.5 rounded-full font-medium transition-colors"
          :class="selectedCategory === cat.id
            ? (cat.type === 'Income' ? 'bg-blue-600 text-white' : 'bg-red-500 text-white')
            : 'bg-gray-100 text-gray-600'"
        >{{ cat.name }}</button>
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
            class="bg-white rounded-xl p-3 mb-2 flex items-center shadow-sm cursor-pointer active:bg-gray-50"
            :class="!tx.isIncludedInTotal ? 'opacity-50' : ''"
            @click="openEdit(tx)"
          >
            <div class="flex-1 min-w-0">
              <div class="flex items-center gap-1.5 flex-wrap">
                <span class="text-sm font-medium truncate">{{ getCategoryName(tx.categoryId, tx.categoryName) }}</span>
                <span v-if="!tx.isIncludedInTotal" class="text-xs bg-gray-100 text-gray-500 px-1.5 py-0.5 rounded">제외</span>
                <!-- 할부 뱃지 -->
                <span
                  v-if="tx.installmentTransactionId"
                  class="text-xs bg-orange-50 text-orange-500 px-1.5 py-0.5 rounded"
                >할부 {{ tx.installmentSequence }}회</span>
                <!-- 반복 뱃지 -->
                <span
                  v-else-if="tx.recurringTransactionId"
                  class="text-xs bg-blue-50 text-blue-500 px-1.5 py-0.5 rounded"
                >반복</span>
              </div>
              <div class="text-xs text-gray-400 mt-0.5">
                <!-- 지출만 결제수단 표시 -->
                <template v-if="tx.type === 'Expense'">
                  {{ tx.paymentMethodName }}<span v-if="tx.memo"> · {{ tx.memo }}</span>
                </template>
                <span v-else-if="tx.memo">{{ tx.memo }}</span>
              </div>
            </div>
            <div class="flex items-center gap-2">
              <span :class="tx.type === 'Income' ? 'text-blue-600' : 'text-red-500'" class="font-semibold text-sm">
                {{ formatAmount(tx.amount, tx.type) }}
              </span>
              <button @click.stop="onDeleteClick(tx)" class="text-gray-300 hover:text-red-400 p-1">
                <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12" />
                </svg>
              </button>
            </div>
          </div>
        </div>
      </template>
    </div>

    <!-- ── FAB ── -->
    <button
      @click="openAdd"
      class="fixed bottom-20 right-4 w-14 h-14 bg-blue-600 text-white rounded-full shadow-lg flex items-center justify-center z-40"
    >
      <svg class="w-7 h-7" fill="none" stroke="currentColor" viewBox="0 0 24 24">
        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4v16m8-8H4" />
      </svg>
    </button>

    <!-- 거래 모달 -->
    <TransactionModal
      v-if="showModal"
      :transaction="editTarget"
      @close="showModal = false"
      @saved="() => { showModal = false; loadData() }"
    />

    <!-- 삭제 옵션 시트 -->
    <DeleteOptionSheet
      v-if="deleteSheetTx"
      :type="deleteSheetType"
      :transaction="deleteSheetTx"
      @delete-all="deleteSheetType === 'installment' ? handleInstallmentDelete('all') : handleRecurringDelete('all')"
      @delete-from-here="deleteSheetType === 'installment' ? handleInstallmentDelete('fromHere') : handleRecurringDelete('fromHere')"
      @delete-single="deleteSheetType === 'installment' ? handleInstallmentDelete('single') : handleRecurringDelete('single')"
      @close="deleteSheetTx = null"
    />
  </div>
</template>
