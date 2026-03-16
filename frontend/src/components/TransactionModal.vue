<script setup lang="ts">
import { ref, computed, watch } from 'vue'
import dayjs from 'dayjs'
import { useAppStore } from '@/stores/app'
import { transactionsApi, recurringApi, installmentApi } from '@/api'
import type { Transaction, TransactionType } from '@/types'

const props = defineProps<{
  transaction: Transaction | null
}>()

const emit = defineEmits<{
  close: []
  saved: []
}>()

const store = useAppStore()

const form = ref({
  type: 'Expense' as TransactionType,
  amount: '',        // 표시용 (콤마 포함 문자열)
  date: dayjs().format('YYYY-MM-DD'),
  paymentMethodId: 0,
  memo: '',
  isIncludedInTotal: true,
})

// 수입/지출 탭 전환 시 각 타입의 카테고리 선택값을 독립적으로 보존
const categoryIds = ref<Record<TransactionType, number>>({ Income: 0, Expense: 0 })

const activeCategoryId = computed({
  get: () => categoryIds.value[form.value.type],
  set: (val: number) => { categoryIds.value[form.value.type] = val },
})

// 할부 관련 (지출 전용, 신규 등록만)
const isInstallment = ref(false)
const installmentMonths = ref<number | undefined>(undefined)

// 반복 관련 (신규 등록만)
const isRecurring = ref(false)
const recurringDay = ref(dayjs().date())
const recurringEndDate = ref('')

const saving = ref(false)
const error = ref('')

const isEdit = computed(() => !!props.transaction)

const categories = computed(() =>
  form.value.type === 'Income' ? store.incomeCategories : store.expenseCategories
)

// 수입 선택 시 할부 토글 리셋
watch(() => form.value.type, (newType) => {
  if (newType === 'Income') {
    isInstallment.value = false
    installmentMonths.value = undefined
  }
})

// 콤마 제거 후 숫자 반환
function rawAmount(): number {
  return parseInt(form.value.amount.replace(/,/g, ''), 10) || 0
}

// 숫자 문자열에 천단위 콤마 삽입
function formatWithComma(digits: string): string {
  return digits.replace(/\B(?=(\d{3})+(?!\d))/g, ',')
}

// 입력 시 콤마 자동 포맷 (최대 9자리 = 999,999,999원)
function onAmountInput(e: Event) {
  const input = e.target as HTMLInputElement
  const digits = input.value.replace(/[^0-9]/g, '').slice(0, 9)
  const formatted = digits ? formatWithComma(digits) : ''
  form.value.amount = formatted
  input.value = formatted
}

// 날짜 변경 시 반복 일자 동기화
watch(() => form.value.date, (date) => {
  if (isRecurring.value && !isEdit.value) {
    recurringDay.value = dayjs(date).date()
  }
})

// 할부 미리보기 계산
const installmentPreview = computed(() => {
  const total = rawAmount()
  const months = installmentMonths.value
  if (!total || !months || months < 2) return null
  const monthly = Math.floor(total / months)
  const first = monthly + (total - monthly * months)
  return { monthly, first }
})

// props.transaction 변경 시 폼 동기화
watch(() => props.transaction, (tx) => {
  isInstallment.value = false
  installmentMonths.value = undefined
  isRecurring.value = false
  recurringDay.value = dayjs().date()
  recurringEndDate.value = ''

  if (tx) {
    form.value = {
      type: tx.type,
      amount: formatWithComma(String(tx.amount)),
      date: dayjs(tx.date).format('YYYY-MM-DD'),
      paymentMethodId: tx.paymentMethodId,
      memo: tx.memo ?? '',
      isIncludedInTotal: tx.isIncludedInTotal,
    }

    categoryIds.value = { Income: 0, Expense: 0 }
    categoryIds.value[tx.type] = tx.categoryId

    // 할부 거래 수정 모드 표시
    if (tx.installmentTransactionId) {
      isInstallment.value = true
    }
    // 반복 거래 수정 모드 표시
    if (tx.recurringTransactionId) {
      isRecurring.value = true
    }
  } else {
    form.value = {
      type: 'Expense',
      amount: '',
      date: dayjs().format('YYYY-MM-DD'),
      paymentMethodId: 0,
      memo: '',
      isIncludedInTotal: true,
    }
    categoryIds.value = { Income: 0, Expense: 0 }
    recurringDay.value = dayjs().date()
  }
}, { immediate: true })

async function save() {
  error.value = ''

  const amount = rawAmount()
  if (!amount || amount <= 0) { error.value = '금액을 입력해주세요.'; return }
  if (!activeCategoryId.value) { error.value = '카테고리를 선택해주세요.'; return }
  if (form.value.type === 'Expense' && !form.value.paymentMethodId) { error.value = '결제수단을 선택해주세요.'; return }
  if (isInstallment.value && (!installmentMonths.value || installmentMonths.value < 2)) {
    error.value = '할부 개월수를 2개월 이상 입력해주세요.'
    return
  }

  saving.value = true

  try {
    if (isEdit.value && props.transaction) {
      // 수정 모드: 일반 거래 수정만 지원
      const payload = {
        amount,
        date: form.value.date,
        memo: form.value.memo || undefined,
        type: form.value.type,
        categoryId: activeCategoryId.value,
        paymentMethodId: form.value.type === 'Expense' ? form.value.paymentMethodId : props.transaction.paymentMethodId,
        isIncludedInTotal: form.value.isIncludedInTotal,
      }
      await transactionsApi.update(props.transaction.id, payload)
    } else if (isInstallment.value && form.value.type === 'Expense') {
      // 할부 신규 등록: installmentApi.create 호출
      await installmentApi.create({
        totalAmount: amount,
        totalInstallments: installmentMonths.value!,
        startDate: form.value.date,
        categoryId: activeCategoryId.value,
        paymentMethodId: form.value.paymentMethodId,
        memo: form.value.memo || undefined,
        isIncludedInTotal: form.value.isIncludedInTotal,
      })
    } else if (isRecurring.value) {
      // 반복 신규 등록: recurring 원부 생성 + 거래 1건 등록 (원부 ID 연결)
      const recurringMaster = await recurringApi.create({
        amount,
        categoryId: activeCategoryId.value,
        paymentMethodId: form.value.type === 'Expense' ? form.value.paymentMethodId : 1,
        dayOfMonth: recurringDay.value,
        memo: form.value.memo || undefined,
        startDate: form.value.date,
        endDate: recurringEndDate.value || undefined,
      })
      await transactionsApi.create({
        amount,
        date: form.value.date,
        memo: form.value.memo || undefined,
        type: form.value.type,
        categoryId: activeCategoryId.value,
        paymentMethodId: form.value.type === 'Expense' ? form.value.paymentMethodId : 1,
        isIncludedInTotal: form.value.isIncludedInTotal,
        recurringTransactionId: recurringMaster.id,
      })
    } else {
      // 일반 신규 등록
      await transactionsApi.create({
        amount,
        date: form.value.date,
        memo: form.value.memo || undefined,
        type: form.value.type,
        categoryId: activeCategoryId.value,
        paymentMethodId: form.value.type === 'Expense' ? form.value.paymentMethodId : 1,
        isIncludedInTotal: form.value.isIncludedInTotal,
      })
    }

    emit('saved')
  } catch (e: unknown) {
    error.value = e instanceof Error ? e.message : '저장 중 오류가 발생했습니다.'
  } finally {
    saving.value = false
  }
}
</script>

<template>
  <!-- 배경 오버레이 -->
  <div class="fixed inset-0 bg-black/50 z-[60] flex items-center justify-center px-4" @click.self="emit('close')">
    <div class="bg-white rounded-2xl w-full max-w-lg flex flex-col" style="max-height: 90dvh">
      <!-- 헤더 (고정) -->
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

        <!-- ── 수입 / 지출 탭 ── -->
        <div class="px-4 pt-4 pb-3">
          <div class="flex rounded-xl bg-gray-100 p-1">
            <button
              @click="form.type = 'Expense'"
              :class="form.type === 'Expense' ? 'bg-white text-red-500 shadow-sm' : 'text-gray-500'"
              class="flex-1 py-2 text-sm font-medium rounded-lg transition-all"
            >지출</button>
            <button
              @click="form.type = 'Income'"
              :class="form.type === 'Income' ? 'bg-white text-blue-600 shadow-sm' : 'text-gray-500'"
              class="flex-1 py-2 text-sm font-medium rounded-lg transition-all"
            >수입</button>
          </div>
        </div>

        <!-- ── 기본 정보: 금액 · 날짜 ── -->
        <div class="px-4 py-3 space-y-3 border-t border-gray-100">
          <p class="text-xs font-medium text-gray-400 uppercase tracking-wide">기본 정보</p>

          <div>
            <div class="flex items-center justify-between mb-1">
              <label class="text-xs text-gray-500">금액</label>
              <!-- 할부 토글: 신규 + 지출만 -->
              <template v-if="!isEdit">
                <button
                  v-if="form.type === 'Expense'"
                  data-testid="installment-toggle"
                  @click="isInstallment = !isInstallment"
                  :class="isInstallment ? 'bg-orange-500 text-white' : 'bg-gray-100 text-gray-500'"
                  class="flex items-center gap-1 text-xs px-2.5 py-1 rounded-full font-medium transition-colors"
                >
                  <svg class="w-3 h-3" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M3 10h18M7 15h1m4 0h1m-7 4h12a3 3 0 003-3V8a3 3 0 00-3-3H6a3 3 0 00-3 3v8a3 3 0 003 3z"/>
                  </svg>
                  할부
                </button>
              </template>
              <!-- 할부 배지: 수정 모드 + 할부 거래 -->
              <span v-else-if="isInstallment" class="flex items-center gap-1 text-xs px-2.5 py-1 rounded-full bg-orange-100 text-orange-600">
                <svg class="w-3 h-3" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M3 10h18M7 15h1m4 0h1m-7 4h12a3 3 0 003-3V8a3 3 0 00-3-3H6a3 3 0 00-3 3v8a3 3 0 003 3z"/>
                </svg>
                할부
              </span>
            </div>
            <input
              data-testid="amount-input"
              :value="form.amount"
              @input="onAmountInput"
              type="text"
              inputmode="numeric"
              placeholder="0"
              class="w-full border border-gray-200 rounded-xl px-3 py-2.5 text-lg font-semibold focus:outline-none focus:border-blue-400"
            />
          </div>

          <!-- 할부 개월수 + 미리보기 (신규 + 지출 + 할부 ON) -->
          <template v-if="isInstallment && form.type === 'Expense' && !isEdit">
            <div>
              <label class="block text-xs text-gray-500 mb-1">총 개월수</label>
              <div class="flex items-center gap-2">
                <input
                  data-testid="installment-months"
                  v-model.number="installmentMonths"
                  type="number" min="2" max="60" placeholder="12"
                  class="w-24 border border-gray-200 rounded-xl px-3 py-2.5 text-sm text-center focus:outline-none focus:border-orange-400"
                />
                <span class="text-sm text-gray-500">개월</span>
              </div>
            </div>
            <div v-if="installmentPreview" class="bg-orange-50 rounded-xl px-3 py-2.5 space-y-1">
              <div class="flex justify-between text-sm">
                <span class="text-gray-600">1회차</span>
                <span class="font-semibold text-orange-700">{{ installmentPreview.first.toLocaleString() }}원</span>
              </div>
              <div class="flex justify-between text-sm">
                <span class="text-gray-600">2회차 이후</span>
                <span class="font-semibold text-orange-700">{{ installmentPreview.monthly.toLocaleString() }}원</span>
              </div>
            </div>
          </template>

          <div>
            <label class="block text-xs text-gray-500 mb-1">날짜</label>
            <input
              v-model="form.date"
              type="date"
              class="w-full border border-gray-200 rounded-xl px-3 py-2.5 text-sm focus:outline-none focus:border-blue-400"
            />
          </div>
        </div>

        <!-- ── 분류: 카테고리 · 결제수단 ── -->
        <div class="px-4 py-3 space-y-3 border-t border-gray-100">
          <p class="text-xs font-medium text-gray-400 uppercase tracking-wide">분류</p>

          <div>
            <label class="block text-xs text-gray-500 mb-1">카테고리</label>
            <select
              data-testid="category-select"
              v-model="activeCategoryId"
              class="w-full border border-gray-200 rounded-xl px-3 py-2.5 text-sm focus:outline-none focus:border-blue-400"
            >
              <option :value="0">선택하세요</option>
              <option v-for="c in categories" :key="c.id" :value="c.id">{{ c.name }}</option>
            </select>
          </div>

          <!-- 결제수단: 지출만 표시 -->
          <div v-if="form.type === 'Expense'">
            <label class="block text-xs text-gray-500 mb-1">결제수단</label>
            <select
              data-testid="payment-method-select"
              v-model="form.paymentMethodId"
              class="w-full border border-gray-200 rounded-xl px-3 py-2.5 text-sm focus:outline-none focus:border-blue-400"
            >
              <option :value="0">선택하세요</option>
              <option v-for="m in store.paymentMethods" :key="m.id" :value="m.id">
                {{ m.name }}
                <template v-if="m.type === 'Point' && m.pointBudget">
                  (잔액 {{ m.pointBudget.remainingAmount.toLocaleString() }}원)
                </template>
              </option>
            </select>
          </div>
        </div>

        <!-- ── 부가 정보: 메모 · 반복 · 합산 포함 ── -->
        <div class="px-4 py-3 space-y-3 border-t border-gray-100">
          <p class="text-xs font-medium text-gray-400 uppercase tracking-wide">부가 정보</p>

          <div>
            <label class="block text-xs text-gray-500 mb-1">메모 (선택)</label>
            <input
              data-testid="memo-input"
              v-model="form.memo"
              type="text"
              placeholder="메모를 입력하세요"
              class="w-full border border-gray-200 rounded-xl px-3 py-2.5 text-sm focus:outline-none focus:border-blue-400"
            />
          </div>

          <!-- 반복 설정 (신규 + 할부 아닐 때) -->
          <div v-if="!isEdit && !isInstallment">
            <label class="flex items-center gap-3 cursor-pointer">
              <div
                data-testid="recurring-toggle"
                @click="isRecurring = !isRecurring"
                :class="isRecurring ? 'bg-teal-500' : 'bg-gray-200'"
                class="w-11 h-6 rounded-full transition-colors relative flex-shrink-0"
              >
                <div :class="isRecurring ? 'translate-x-5' : 'translate-x-0.5'" class="absolute top-0.5 w-5 h-5 bg-white rounded-full shadow transition-transform" />
              </div>
              <span class="text-sm text-gray-700">매월 반복</span>
            </label>
            <template v-if="isRecurring">
              <div class="mt-3 flex items-center gap-2">
                <span class="text-sm text-gray-600">매월</span>
                <input
                  data-testid="recurring-day"
                  v-model.number="recurringDay"
                  type="number" min="1" max="28"
                  class="w-16 border border-gray-200 rounded-xl px-3 py-2 text-sm text-center focus:outline-none focus:border-teal-400"
                />
                <span class="text-sm text-gray-600">일 반복</span>
              </div>
              <div class="mt-2">
                <label class="block text-xs text-gray-500 mb-1">종료일 <span class="text-gray-400">(없으면 무기한)</span></label>
                <input
                  v-model="recurringEndDate"
                  type="date"
                  class="w-full border border-gray-200 rounded-xl px-3 py-2.5 text-sm focus:outline-none focus:border-teal-400"
                />
              </div>
            </template>
          </div>

          <label class="flex items-center gap-3 cursor-pointer">
            <div
              @click="form.isIncludedInTotal = !form.isIncludedInTotal"
              :class="form.isIncludedInTotal ? 'bg-blue-600' : 'bg-gray-200'"
              class="w-11 h-6 rounded-full transition-colors relative"
            >
              <div :class="form.isIncludedInTotal ? 'translate-x-5' : 'translate-x-0.5'" class="absolute top-0.5 w-5 h-5 bg-white rounded-full shadow transition-transform" />
            </div>
            <span class="text-sm text-gray-700">합산에 포함</span>
          </label>
        </div>

        <!-- 오류 메시지 -->
        <p v-if="error" class="px-4 pb-2 text-red-500 text-sm">{{ error }}</p>

      </div>

      <!-- 저장 버튼 (하단 고정) -->
      <div class="flex-shrink-0 px-4 py-4 border-t border-gray-100">
        <button
          data-testid="save-btn"
          @click="save"
          :disabled="saving"
          :class="isInstallment && form.type === 'Expense' && !isEdit ? 'bg-orange-500 hover:bg-orange-600' : 'bg-blue-600 hover:bg-blue-700'"
          class="w-full text-white py-3 rounded-xl font-semibold text-sm disabled:opacity-50"
        >
          {{ saving ? '저장 중...' : isEdit ? '수정' : isInstallment ? '할부 등록' : isRecurring ? '반복 등록' : '추가' }}
        </button>
      </div>
    </div>
  </div>
</template>
