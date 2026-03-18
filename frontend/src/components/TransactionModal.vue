<script setup lang="ts">
import { ref, computed, watch } from 'vue'
import dayjs from 'dayjs'
import { useTransactionStore } from '@/stores/transaction'
import { useDialog } from '@/composables/useDialog'
import type { TransactionView, Category, PaymentMethod, SavingsMethod } from '@/types'
import type { TransactionType } from '@/database/db'

const props = defineProps<{
  editTarget:     TransactionView | null
  categories:     (Category & { id: number })[]
  paymentMethods: (PaymentMethod & { id: number })[]
  savingsMethods: (SavingsMethod & { id: number })[]
}>()

const emit = defineEmits<{
  close: []
  saved: []
}>()

const txStore = useTransactionStore()
const { showAlert } = useDialog()

// ── 폼 상태 ───────────────────────────────────────────────────────────────────

const txType  = ref<TransactionType>('Expense')
const amount  = ref('')
const date    = ref(dayjs().format('YYYY-MM-DD'))
const memo    = ref('')
const isIncludedInTotal = ref(true)

// 카테고리: 타입별 독립 보관
const categoryIds = ref<Record<TransactionType, number>>({ Income: 0, Expense: 0, Savings: 0 })
const activeCategoryId = computed({
  get: () => categoryIds.value[txType.value],
  set: (v: number) => { categoryIds.value[txType.value] = v },
})

// 결제수단 / 저축 수단
const paymentMethodId = ref(0)
const savingsMethodId = ref(0)

// 할부
const isInstallment     = ref(false)
const installmentMonths = ref<number | undefined>(undefined)

// 반복
const isRecurring      = ref(false)
const recurringDay     = ref(dayjs().date())
const recurringEndDate = ref('')

const saving = ref(false)
const error  = ref('')

const isEdit = computed(() => !!props.editTarget)

// ── 카테고리 필터 ─────────────────────────────────────────────────────────────

const filteredCategories = computed(() =>
  props.categories.filter(c => c.type === txType.value)
)

// ── 할부 미리보기 ──────────────────────────────────────────────────────────────

const installmentPreview = computed(() => {
  const total  = rawAmount()
  const months = installmentMonths.value
  if (!total || !months || months < 2) return null
  const monthly = Math.floor(total / months)
  const first   = monthly + (total - monthly * months)
  return { monthly, first }
})

// ── 초기화 ────────────────────────────────────────────────────────────────────

watch(() => props.editTarget, (tx) => {
  isInstallment.value     = false
  installmentMonths.value = undefined
  isRecurring.value       = false
  recurringEndDate.value  = ''
  error.value = ''

  if (tx) {
    txType.value              = tx.type
    amount.value              = formatWithComma(String(tx.amount))
    date.value                = tx.date
    memo.value                = tx.memo ?? ''
    isIncludedInTotal.value   = tx.isIncludedInTotal
    categoryIds.value         = { Income: 0, Expense: 0, Savings: 0 }
    categoryIds.value[tx.type] = tx.categoryId
    paymentMethodId.value     = tx.paymentMethodId ?? 0
    savingsMethodId.value     = tx.savingsMethodId ?? 0
    if (tx.installmentTransactionId) isInstallment.value = true
    if (tx.recurringTransactionId)   isRecurring.value   = true
  } else {
    txType.value            = 'Expense'
    amount.value            = ''
    date.value              = dayjs().format('YYYY-MM-DD')
    memo.value              = ''
    isIncludedInTotal.value = true
    categoryIds.value       = { Income: 0, Expense: 0, Savings: 0 }
    paymentMethodId.value   = props.paymentMethods[0]?.id ?? 0
    savingsMethodId.value   = props.savingsMethods[0]?.id ?? 0
    recurringDay.value      = dayjs().date()
  }
}, { immediate: true })

// 타입 전환 시 처리
watch(txType, (t) => {
  if (t === 'Income') {
    isInstallment.value     = false
    installmentMonths.value = undefined
  }
  if (t === 'Expense' && !paymentMethodId.value) {
    paymentMethodId.value = props.paymentMethods[0]?.id ?? 0
  }
})

// 날짜 변경 시 반복 일자 동기화
watch(date, (d) => {
  if (isRecurring.value && !isEdit.value) {
    recurringDay.value = dayjs(d).date()
  }
})

// ── 유틸 ──────────────────────────────────────────────────────────────────────

function rawAmount(): number {
  return parseInt(amount.value.replace(/,/g, ''), 10) || 0
}

function formatWithComma(digits: string): string {
  return digits.replace(/\B(?=(\d{3})+(?!\d))/g, ',')
}

function onAmountInput(e: Event) {
  const input   = e.target as HTMLInputElement
  const digits  = input.value.replace(/[^0-9]/g, '').slice(0, 9)
  const formatted = digits ? formatWithComma(digits) : ''
  amount.value  = formatted
  input.value   = formatted
}

// ── 저장 ──────────────────────────────────────────────────────────────────────

async function save() {
  error.value = ''
  const amt = rawAmount()
  if (!amt || amt <= 0)           { error.value = '금액을 입력해주세요.'; return }
  if (!activeCategoryId.value)    { error.value = '카테고리를 선택해주세요.'; return }
  if (txType.value === 'Expense' && !paymentMethodId.value) { error.value = '결제수단을 선택해주세요.'; return }
  if (txType.value === 'Savings' && !savingsMethodId.value) { error.value = '저축 수단을 선택해주세요.'; return }
  if (isInstallment.value && (!installmentMonths.value || installmentMonths.value < 2)) {
    error.value = '할부 개월수를 2개월 이상 입력해주세요.'
    return
  }

  saving.value = true
  try {
    if (isEdit.value && props.editTarget) {
      // 수정 모드 (단건)
      await txStore.updateTransaction(props.editTarget.id, {
        amount:            amt,
        date:              date.value,
        memo:              memo.value || undefined,
        type:              txType.value,
        categoryId:        activeCategoryId.value,
        paymentMethodId:   txType.value === 'Expense' ? paymentMethodId.value : undefined,
        savingsMethodId:   txType.value === 'Savings' ? savingsMethodId.value : undefined,
        isIncludedInTotal: isIncludedInTotal.value,
      })
    } else if (isInstallment.value && txType.value === 'Expense') {
      // 할부 신규
      await txStore.createInstallment({
        totalAmount:       amt,
        totalInstallments: installmentMonths.value!,
        startDate:         date.value,
        categoryId:        activeCategoryId.value,
        paymentMethodId:   paymentMethodId.value,
        memo:              memo.value || undefined,
        isIncludedInTotal: isIncludedInTotal.value,
      })
    } else if (isRecurring.value) {
      // 반복 신규
      await txStore.createRecurring({
        amount:           amt,
        type:             txType.value,
        categoryId:       activeCategoryId.value,
        paymentMethodId:  txType.value === 'Expense' ? paymentMethodId.value : undefined,
        savingsMethodId:  txType.value === 'Savings' ? savingsMethodId.value : undefined,
        dayOfMonth:       recurringDay.value,
        startDate:        date.value,
        endDate:          recurringEndDate.value || undefined,
        memo:             memo.value || undefined,
      })
    } else {
      // 단건 신규
      await txStore.createSingle({
        amount:            amt,
        date:              date.value,
        type:              txType.value,
        categoryId:        activeCategoryId.value,
        paymentMethodId:   txType.value === 'Expense' ? paymentMethodId.value : undefined,
        savingsMethodId:   txType.value === 'Savings' ? savingsMethodId.value : undefined,
        memo:              memo.value || undefined,
        isIncludedInTotal: isIncludedInTotal.value,
      })
    }
    emit('saved')
  } catch (e: unknown) {
    error.value = e instanceof Error ? e.message : '저장 중 오류가 발생했습니다.'
    if (e instanceof Error && e.message.includes('잔액')) await showAlert(e.message)
  } finally {
    saving.value = false
  }
}
</script>

<template>
  <div class="fixed inset-0 bg-black/40 z-[60] flex items-center justify-center px-4" @click.self="emit('close')">
    <div class="bg-white rounded-2xl w-full max-w-lg flex flex-col" style="max-height: 90dvh">

      <!-- 헤더 -->
      <div class="flex-shrink-0 flex items-center justify-between px-4 py-4 border-b border-gray-100">
        <h2 class="font-semibold text-gray-800">{{ isEdit ? '거래 수정' : '거래 추가' }}</h2>
        <button @click="emit('close')" class="text-gray-400 hover:text-gray-600">
          <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12" />
          </svg>
        </button>
      </div>

      <!-- 폼 (스크롤) -->
      <div class="flex-1 overflow-y-auto">

        <!-- 거래 유형 탭 -->
        <div class="px-4 pt-4 pb-3">
          <div class="flex rounded-xl bg-gray-100 p-1">
            <button @click="txType = 'Expense'" :class="txType === 'Expense' ? 'bg-white text-red-500 shadow-sm' : 'text-gray-500'" class="flex-1 py-2 text-sm font-medium rounded-lg transition-all">지출</button>
            <button @click="txType = 'Income'"  :class="txType === 'Income'  ? 'bg-white text-blue-600 shadow-sm' : 'text-gray-500'" class="flex-1 py-2 text-sm font-medium rounded-lg transition-all">수입</button>
            <button @click="txType = 'Savings'" :class="txType === 'Savings' ? 'bg-white text-emerald-600 shadow-sm' : 'text-gray-500'" class="flex-1 py-2 text-sm font-medium rounded-lg transition-all">저축</button>
          </div>
        </div>

        <!-- 기본 정보 -->
        <div class="px-4 py-3 space-y-3 border-t border-gray-100">
          <p class="text-xs font-medium text-gray-400 uppercase tracking-wide">기본 정보</p>

          <!-- 금액 -->
          <div>
            <div class="flex items-center justify-between mb-1">
              <label class="text-xs text-gray-500">금액</label>
              <!-- 할부 토글: 신규 + 지출만 -->
              <button
                v-if="!isEdit && txType === 'Expense'"
                data-testid="installment-toggle"
                @click="isInstallment = !isInstallment"
                :class="isInstallment ? 'bg-orange-500 text-white' : 'bg-gray-100 text-gray-500'"
                class="flex items-center gap-1 text-xs px-2.5 py-1 rounded-full font-medium transition-colors"
              >할부</button>
              <!-- 반복 토글: 신규만 -->
              <button
                v-if="!isEdit && !isInstallment"
                data-testid="recurring-toggle"
                @click="isRecurring = !isRecurring"
                :class="isRecurring ? 'bg-teal-500 text-white' : 'bg-gray-100 text-gray-500'"
                class="flex items-center gap-1 text-xs px-2.5 py-1 rounded-full font-medium transition-colors"
              >반복</button>
            </div>
            <div class="flex items-center gap-2">
              <input
                :value="amount"
                @input="onAmountInput"
                type="text"
                inputmode="numeric"
                placeholder="0"
                class="flex-1 border border-gray-200 rounded-xl px-3 py-2.5 text-sm text-right focus:outline-none focus:border-blue-400"
              />
              <span class="text-sm text-gray-500 flex-shrink-0">원</span>
            </div>
            <!-- 할부 개월수 + 미리보기 -->
            <div v-if="isInstallment" class="mt-2 space-y-1.5">
              <div class="flex items-center gap-2">
                <input
                  v-model.number="installmentMonths"
                  type="number" min="2" max="60" placeholder="개월수"
                  class="flex-1 border border-orange-200 rounded-xl px-3 py-2 text-sm focus:outline-none focus:border-orange-400"
                />
                <span class="text-sm text-gray-500 flex-shrink-0">개월 할부</span>
              </div>
              <p v-if="installmentPreview" class="text-xs text-orange-600 bg-orange-50 rounded-lg px-3 py-1.5">
                1회차 {{ installmentPreview.first.toLocaleString() }}원 + 이후 {{ installmentPreview.monthly.toLocaleString() }}원
              </p>
            </div>
          </div>

          <!-- 날짜 -->
          <div>
            <label class="text-xs text-gray-500 block mb-1">날짜</label>
            <input v-model="date" type="date" class="w-full border border-gray-200 rounded-xl px-3 py-2.5 text-sm focus:outline-none focus:border-blue-400" />
          </div>

          <!-- 반복 설정 -->
          <div v-if="isRecurring" class="space-y-2">
            <div class="flex items-center gap-2">
              <label class="text-xs text-gray-500 w-20">매월 반복일</label>
              <select v-model.number="recurringDay" class="flex-1 border border-teal-200 rounded-xl px-3 py-2 text-sm focus:outline-none focus:border-teal-400">
                <option v-for="d in 28" :key="d" :value="d">{{ d }}일</option>
              </select>
            </div>
            <div class="flex items-center gap-2">
              <label class="text-xs text-gray-500 w-20">종료일 (선택)</label>
              <input v-model="recurringEndDate" type="date" class="flex-1 border border-gray-200 rounded-xl px-3 py-2 text-sm focus:outline-none focus:border-blue-400" />
            </div>
          </div>
        </div>

        <!-- 카테고리 -->
        <div class="px-4 py-3 border-t border-gray-100">
          <p class="text-xs font-medium text-gray-400 uppercase tracking-wide mb-3">카테고리</p>
          <div class="flex flex-wrap gap-2">
            <button
              v-for="cat in filteredCategories"
              :key="cat.id"
              @click="activeCategoryId = cat.id"
              :class="activeCategoryId === cat.id
                ? (txType === 'Income' ? 'bg-blue-600 text-white' : txType === 'Expense' ? 'bg-red-500 text-white' : 'bg-emerald-600 text-white')
                : 'bg-gray-100 text-gray-600'"
              class="text-sm px-3 py-1.5 rounded-full transition-colors"
            >{{ cat.name }}</button>
            <p v-if="filteredCategories.length === 0" class="text-xs text-gray-400">카테고리가 없습니다</p>
          </div>
        </div>

        <!-- 결제수단 (지출만) -->
        <div v-if="txType === 'Expense'" class="px-4 py-3 border-t border-gray-100">
          <p class="text-xs font-medium text-gray-400 uppercase tracking-wide mb-3">결제수단</p>
          <div class="flex flex-wrap gap-2">
            <button
              v-for="pm in paymentMethods"
              :key="pm.id"
              @click="paymentMethodId = pm.id"
              :class="paymentMethodId === pm.id ? 'bg-gray-800 text-white' : 'bg-gray-100 text-gray-600'"
              class="text-sm px-3 py-1.5 rounded-full transition-colors"
            >{{ pm.name }}</button>
          </div>
        </div>

        <!-- 저축 수단 (저축만) -->
        <div v-if="txType === 'Savings'" class="px-4 py-3 border-t border-gray-100">
          <p class="text-xs font-medium text-gray-400 uppercase tracking-wide mb-3">저축 수단</p>
          <div class="flex flex-wrap gap-2">
            <button
              v-for="sm in savingsMethods"
              :key="sm.id"
              @click="savingsMethodId = sm.id"
              :class="savingsMethodId === sm.id ? 'bg-emerald-600 text-white' : 'bg-gray-100 text-gray-600'"
              class="text-sm px-3 py-1.5 rounded-full transition-colors"
            >{{ sm.name }}</button>
          </div>
        </div>

        <!-- 메모 + 합산 여부 -->
        <div class="px-4 py-3 border-t border-gray-100 space-y-3">
          <p class="text-xs font-medium text-gray-400 uppercase tracking-wide">추가 정보</p>
          <div>
            <label class="text-xs text-gray-500 block mb-1">메모</label>
            <input v-model="memo" type="text" placeholder="메모 (선택)" class="w-full border border-gray-200 rounded-xl px-3 py-2.5 text-sm focus:outline-none focus:border-blue-400" />
          </div>
          <label class="flex items-center gap-2 cursor-pointer">
            <input v-model="isIncludedInTotal" type="checkbox" class="w-4 h-4 rounded accent-blue-600" />
            <span class="text-sm text-gray-700">합계에 포함</span>
          </label>
        </div>

        <div class="h-4" />
      </div>

      <!-- 저장 버튼 (고정) -->
      <div class="flex-shrink-0 px-4 py-4 border-t border-gray-100">
        <p v-if="error" class="text-red-500 text-xs mb-2 text-center">{{ error }}</p>
        <button
          @click="save"
          :disabled="saving"
          class="w-full py-3.5 bg-blue-600 text-white rounded-xl text-sm font-semibold hover:bg-blue-500 disabled:opacity-50 transition-colors"
        >
          {{ saving ? '저장 중...' : (isEdit ? '수정 완료' : '추가') }}
        </button>
      </div>
    </div>
  </div>
</template>
