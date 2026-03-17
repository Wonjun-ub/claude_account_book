<script setup lang="ts">
import { ref, computed, watch, onMounted } from 'vue'
import dayjs from 'dayjs'
import { useSettingsStore } from '@/stores/app'
import { useCategoryStore } from '@/stores/category'
import { usePaymentMethodStore } from '@/stores/paymentMethod'
import { useTransactionStore } from '@/stores/transaction'
import { useDialog } from '@/composables/useDialog'
import { useDateFormat } from '@/composables/useDateFormat'
import type { TransactionView, MonthlySummary } from '@/types'
import type { RecurringTransaction } from '@/database/db'
import TransactionModal from '@/components/TransactionModal.vue'
import DeleteOptionSheet from '@/components/DeleteOptionSheet.vue'
import LoadingSpinner from '@/components/LoadingSpinner.vue'

const settingsStore  = useSettingsStore()
const categoryStore  = useCategoryStore()
const pmStore        = usePaymentMethodStore()
const txStore        = useTransactionStore()
const { showConfirm, showAlert } = useDialog()
const { formatDate } = useDateFormat()

const loadingTx           = ref(false)
const showModal           = ref(false)
const editTarget          = ref<TransactionView | null>(null)
const keyword             = ref('')
const selectedCategory    = ref<number | null>(null)
const showPendingBanner   = ref(false)
const pendingRecurring    = ref<RecurringTransaction[]>([])

// 삭제 시트
const deleteSheetTx   = ref<TransactionView | null>(null)
const deleteSheetType = ref<'installment' | 'recurring'>('installment')

// ── 로드 ─────────────────────────────────────────────────────────────────────

async function loadData() {
  loadingTx.value = true
  selectedCategory.value = null
  try {
    await txStore.loadTransactions(
      settingsStore.currentYear,
      settingsStore.currentMonth,
      settingsStore.monthStartDay,
    )
    pendingRecurring.value = await txStore.getPendingRecurring(
      settingsStore.currentYear,
      settingsStore.currentMonth,
      settingsStore.monthStartDay,
    )
  } finally {
    loadingTx.value = false
  }
}

watch(
  [() => settingsStore.currentYear, () => settingsStore.currentMonth],
  () => loadData(),
)

onMounted(async () => {
  await loadData()
})

// ── 필터 ─────────────────────────────────────────────────────────────────────

const categoryChips = computed(() => {
  const seen = new Set<number>()
  const result: { id: number; name: string; type: string }[] = []
  for (const tx of txStore.transactions) {
    if (!seen.has(tx.categoryId)) {
      seen.add(tx.categoryId)
      result.push({ id: tx.categoryId, name: tx.categoryName, type: tx.type })
    }
  }
  return result
})

const filteredTransactions = computed(() => {
  let list = txStore.transactions
  if (keyword.value.trim()) {
    const kw = keyword.value.trim().toLowerCase()
    list = list.filter(t => t.memo?.toLowerCase().includes(kw) || t.categoryName.toLowerCase().includes(kw))
  }
  if (selectedCategory.value !== null) {
    list = list.filter(t => t.categoryId === selectedCategory.value)
  }
  return list
})

// 반복 예정 요약
const pendingSummary = computed(() => {
  const income  = pendingRecurring.value.filter(r => r.type === 'Income').reduce((s, r) => s + r.amount, 0)
  const expense = pendingRecurring.value.filter(r => r.type === 'Expense').reduce((s, r) => s + r.amount, 0)
  return { income, expense }
})

// 기간 표시
const periodRange = computed(() => {
  const s = txStore.summary
  if (!s) return null
  const start = dayjs(s.periodStart)
  const end   = dayjs(s.periodEnd)
  if (start.date() === 1 && end.date() >= 28) return null
  return `${start.format('M/D')} ~ ${end.format('M/D')}`
})

// ── 거래 CRUD ────────────────────────────────────────────────────────────────

function openAdd() {
  editTarget.value = null
  showModal.value = true
}

function openEdit(tx: TransactionView) {
  editTarget.value = tx
  showModal.value = true
}

async function onDeleteClick(tx: TransactionView) {
  if (tx.installmentTransactionId) {
    deleteSheetTx.value = tx
    deleteSheetType.value = 'installment'
  } else if (tx.recurringTransactionId) {
    deleteSheetTx.value = tx
    deleteSheetType.value = 'recurring'
  } else {
    await handleDeleteSingle(tx)
  }
}

async function handleDeleteSingle(tx: TransactionView) {
  if (!await showConfirm('거래를 삭제하시겠습니까?')) return
  try {
    await txStore.deleteTransaction(tx.id)
    await loadData()
  } catch (e: unknown) {
    await showAlert(e instanceof Error ? e.message : '삭제 중 오류가 발생했습니다.')
  }
}

async function handleInstallmentDelete(mode: 'all' | 'fromHere' | 'single') {
  const tx = deleteSheetTx.value
  if (!tx?.installmentTransactionId) return
  deleteSheetTx.value = null
  try {
    await txStore.deleteInstallment(tx.installmentTransactionId, mode, tx.installmentSequence)
    await loadData()
  } catch (e: unknown) {
    await showAlert(e instanceof Error ? e.message : '삭제 중 오류가 발생했습니다.')
  }
}

async function handleRecurringDelete(mode: 'all' | 'fromHere' | 'single') {
  const tx = deleteSheetTx.value
  if (!tx?.recurringTransactionId) return
  deleteSheetTx.value = null
  try {
    if (mode === 'all') {
      await txStore.deleteRecurring(tx.recurringTransactionId, 'all')
    } else if (mode === 'fromHere') {
      await txStore.deleteRecurring(tx.recurringTransactionId, 'fromHere', undefined, undefined, tx.date)
    } else {
      await txStore.deleteRecurring(
        tx.recurringTransactionId,
        'skipMonth',
        settingsStore.currentYear,
        settingsStore.currentMonth,
      )
    }
    await loadData()
  } catch (e: unknown) {
    await showAlert(e instanceof Error ? e.message : '삭제 중 오류가 발생했습니다.')
  }
}

function groupByDate(txs: TransactionView[]) {
  const map = new Map<string, TransactionView[]>()
  for (const tx of txs) {
    const key = dayjs(tx.date).format('YYYY-MM-DD')
    if (!map.has(key)) map.set(key, [])
    map.get(key)!.push(tx)
  }
  return Array.from(map.entries()).sort((a, b) => b[0].localeCompare(a[0]))
}
</script>

<template>
  <div class="h-full flex flex-col max-w-lg mx-auto">

    <!-- ── 고정 헤더 ── -->
    <div class="flex-shrink-0">
      <div class="bg-blue-600 text-white px-4 pt-10 pb-6">
        <div class="flex items-center justify-between mb-4">
          <button @click="settingsStore.prevMonth()" class="p-1 rounded-full hover:bg-blue-500">
            <svg class="w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 19l-7-7 7-7" />
            </svg>
          </button>
          <div class="text-center">
            <div data-testid="month-label" class="text-lg font-semibold">{{ settingsStore.currentMonthLabel }}</div>
            <div v-if="periodRange" class="text-xs text-blue-200 mt-0.5">{{ periodRange }}</div>
          </div>
          <button @click="settingsStore.nextMonth()" class="p-1 rounded-full hover:bg-blue-500">
            <svg class="w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 5l7 7-7 7" />
            </svg>
          </button>
        </div>
        <div v-if="txStore.summary" class="grid grid-cols-3 gap-2 text-center">
          <div class="bg-blue-500 rounded-xl p-3">
            <div class="text-blue-200 text-xs mb-1">수입</div>
            <div class="font-bold text-sm">{{ txStore.summary.totalIncome.toLocaleString() }}원</div>
            <div class="text-blue-300 text-xs mt-0.5">{{ txStore.summary.incomeCount }}건</div>
          </div>
          <div class="bg-blue-500 rounded-xl p-3">
            <div class="text-blue-200 text-xs mb-1">지출</div>
            <div class="font-bold text-sm">{{ txStore.summary.totalExpense.toLocaleString() }}원</div>
            <div class="text-blue-300 text-xs mt-0.5">{{ txStore.summary.expenseCount }}건</div>
          </div>
          <div class="bg-blue-500 rounded-xl p-3">
            <div class="text-blue-200 text-xs mb-1">잔액</div>
            <div class="font-bold text-sm">{{ txStore.summary.balance.toLocaleString() }}원</div>
          </div>
        </div>
      </div>

      <!-- 검색바 -->
      <div class="px-4 py-2 bg-white border-b border-gray-100">
        <input
          v-model="keyword"
          type="text"
          placeholder="메모 또는 카테고리 검색..."
          class="w-full text-sm border border-gray-200 rounded-lg px-3 py-1.5 focus:outline-none focus:border-blue-400"
        />
      </div>

      <!-- 카테고리 필터 칩 -->
      <div v-if="categoryChips.length > 0" class="px-4 py-2 bg-white border-b border-gray-100 flex gap-2 overflow-x-auto">
        <button
          @click="selectedCategory = null"
          :class="selectedCategory === null ? 'bg-blue-600 text-white' : 'bg-gray-100 text-gray-600'"
          class="flex-shrink-0 text-xs px-3 py-1 rounded-full"
        >전체</button>
        <button
          v-for="chip in categoryChips"
          :key="chip.id"
          @click="selectedCategory = chip.id"
          :class="selectedCategory === chip.id ? 'bg-blue-600 text-white' : 'bg-gray-100 text-gray-600'"
          class="flex-shrink-0 text-xs px-3 py-1 rounded-full"
        >{{ chip.name }}</button>
      </div>

      <!-- 반복 예정 배너 -->
      <div v-if="pendingRecurring.length > 0" class="bg-teal-50 border-b border-teal-100">
        <button
          @click="showPendingBanner = !showPendingBanner"
          class="w-full px-4 py-2.5 flex items-center justify-between"
        >
          <span class="text-xs font-semibold text-teal-700">반복 예정 {{ pendingRecurring.length }}건</span>
          <svg :class="showPendingBanner ? 'rotate-180' : ''" class="w-4 h-4 text-teal-500 transition-transform" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 9l-7 7-7-7"/>
          </svg>
        </button>
        <div v-if="showPendingBanner" class="px-4 pb-3 space-y-2">
          <div v-for="r in pendingRecurring" :key="r.id" class="flex items-center justify-between bg-white rounded-xl px-3 py-2.5 border border-teal-100">
            <div class="flex items-center gap-2 min-w-0">
              <span class="w-1.5 h-1.5 rounded-full flex-shrink-0" :class="r.type === 'Income' ? 'bg-blue-400' : 'bg-red-400'" />
              <span class="text-sm text-gray-700 truncate">매월 {{ r.dayOfMonth }}일</span>
            </div>
            <span class="text-sm font-medium text-gray-800">{{ r.amount.toLocaleString() }}원</span>
          </div>
        </div>
      </div>
    </div>

    <!-- ── 스크롤 가능한 거래 목록 ── -->
    <div class="flex-1 overflow-y-auto">
      <LoadingSpinner v-if="loadingTx" />
      <div v-else-if="filteredTransactions.length === 0" class="flex flex-col items-center justify-center h-full gap-2 text-gray-400">
        <svg class="w-12 h-12 opacity-30" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.5" d="M9 5H7a2 2 0 00-2 2v12a2 2 0 002 2h10a2 2 0 002-2V7a2 2 0 00-2-2h-2M9 5a2 2 0 002 2h2a2 2 0 002-2M9 5a2 2 0 012-2h2a2 2 0 012 2"/>
        </svg>
        <p class="text-sm">거래 내역이 없습니다</p>
        <p class="text-xs">+ 버튼을 눌러 거래를 추가하세요</p>
      </div>
      <div v-else>
        <div
          v-for="[date, txs] in groupByDate(filteredTransactions)"
          :key="date"
          class="mb-1"
        >
          <!-- 날짜 헤더 -->
          <div class="px-4 py-2 bg-gray-50 flex items-center justify-between">
            <span class="text-xs font-medium text-gray-500">{{ formatDate(date) }}</span>
            <span class="text-xs text-gray-400">
              <template v-if="txs.filter(t=>t.type==='Income').length > 0">+{{ txs.filter(t=>t.type==='Income').reduce((s,t)=>s+t.amount,0).toLocaleString() }}</template>
              <template v-if="txs.filter(t=>t.type==='Expense').length > 0"> -{{ txs.filter(t=>t.type==='Expense').reduce((s,t)=>s+t.amount,0).toLocaleString() }}</template>
            </span>
          </div>
          <!-- 거래 항목 -->
          <div
            v-for="tx in txs"
            :key="tx.id"
            class="flex items-center px-4 py-3 bg-white border-b border-gray-50 gap-3"
            :class="{ 'opacity-50': !tx.isIncludedInTotal }"
          >
            <div class="flex-1 min-w-0" @click="openEdit(tx)">
              <div class="flex items-center gap-1.5">
                <span class="text-sm font-medium text-gray-800 truncate">{{ tx.categoryName }}</span>
                <span v-if="tx.installmentSequence" class="text-[10px] text-orange-600 bg-orange-50 px-1.5 py-0.5 rounded flex-shrink-0">
                  {{ tx.installmentSequence }}/{{ tx.installmentTotalInstallments }}회
                </span>
                <span v-if="tx.recurringTransactionId && !tx.installmentTransactionId" class="text-[10px] text-teal-600 bg-teal-50 px-1.5 py-0.5 rounded flex-shrink-0">반복</span>
              </div>
              <div class="flex items-center gap-1.5 mt-0.5">
                <span v-if="tx.memo" class="text-xs text-gray-400 truncate">{{ tx.memo }}</span>
                <span v-if="tx.paymentMethodName && tx.type === 'Expense'" class="text-xs text-gray-300 flex-shrink-0">{{ tx.paymentMethodName }}</span>
              </div>
            </div>
            <div class="flex items-center gap-2 flex-shrink-0">
              <span
                class="text-sm font-semibold"
                :class="tx.type === 'Income' ? 'text-blue-600' : tx.type === 'Expense' ? 'text-red-500' : 'text-emerald-600'"
              >
                {{ tx.type === 'Income' ? '+' : '-' }}{{ tx.amount.toLocaleString() }}
              </span>
              <button @click="onDeleteClick(tx)" class="p-1 text-gray-300 hover:text-red-400">
                <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12"/>
                </svg>
              </button>
            </div>
          </div>
        </div>
      </div>
    </div>

    <!-- ── FAB ── -->
    <button
      @click="openAdd"
      class="fixed bottom-20 right-4 w-14 h-14 bg-blue-600 text-white rounded-full shadow-lg flex items-center justify-center text-2xl hover:bg-blue-500 z-40"
    >+</button>

    <!-- ── 모달 / 시트 ── -->
    <TransactionModal
      v-if="showModal"
      :edit-target="editTarget"
      :categories="categoryStore.categories"
      :payment-methods="pmStore.paymentMethods"
      :savings-methods="pmStore.savingsMethods"
      @close="showModal = false"
      @saved="showModal = false; loadData()"
    />

    <DeleteOptionSheet
      v-if="deleteSheetTx"
      :type="deleteSheetType"
      :transaction="deleteSheetTx"
      @close="deleteSheetTx = null"
      @delete-all="deleteSheetType === 'installment' ? handleInstallmentDelete('all') : handleRecurringDelete('all')"
      @delete-from-here="deleteSheetType === 'installment' ? handleInstallmentDelete('fromHere') : handleRecurringDelete('fromHere')"
      @delete-single="deleteSheetType === 'installment' ? handleInstallmentDelete('single') : handleRecurringDelete('single')"
    />
  </div>
</template>
