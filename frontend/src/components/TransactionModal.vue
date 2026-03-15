<script setup lang="ts">
import { ref, computed, watch } from 'vue'
import dayjs from 'dayjs'
import { useAppStore } from '@/stores/app'
import { transactionsApi, recurringApi } from '@/api'
import type { Transaction, TransactionType, RecurringType } from '@/types'

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

// 반복 설정 (신규 등록 시에만 사용)
const recurring = ref({
  enabled: false,
  type: 'Fixed' as RecurringType,
  dayOfMonth: dayjs().date(),
  totalInstallments: undefined as number | undefined,
})

const saving = ref(false)
const error = ref('')

const isEdit = computed(() => !!props.transaction)

const categories = computed(() =>
  form.value.type === 'Income' ? store.incomeCategories : store.expenseCategories
)

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
  input.value = formatted  // DOM 실제 값도 덮어써야 초과 입력 차단
}

// 날짜 변경 시 반복 일자 동기화 (사용자가 직접 수정하기 전까지)
watch(() => form.value.date, (date) => {
  if (!recurring.value.enabled) {
    recurring.value.dayOfMonth = dayjs(date).date()
  }
})

// props.transaction 변경 시 폼 동기화
watch(() => props.transaction, (tx) => {
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

    recurring.value = {
      enabled: !!tx.recurringTransactionId,
      type: 'Fixed',
      dayOfMonth: dayjs(tx.date).date(),
      totalInstallments: undefined,
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
    recurring.value = {
      enabled: false,
      type: 'Fixed',
      dayOfMonth: dayjs().date(),
      totalInstallments: undefined,
    }
  }
}, { immediate: true })

async function save() {
  error.value = ''

  const amount = rawAmount()
  if (!amount || amount <= 0) { error.value = '금액을 입력해주세요.'; return }
  if (!activeCategoryId.value) { error.value = '카테고리를 선택해주세요.'; return }
  if (!form.value.paymentMethodId) { error.value = '결제수단을 선택해주세요.'; return }
  if (!isEdit.value && recurring.value.enabled && recurring.value.type === 'Installment' && !recurring.value.totalInstallments) {
    error.value = '총 할부 횟수를 입력해주세요.'
    return
  }

  saving.value = true

  try {
    const payload = {
      amount,
      date: form.value.date,
      memo: form.value.memo || undefined,
      type: form.value.type,
      categoryId: activeCategoryId.value,
      paymentMethodId: form.value.paymentMethodId,
      isIncludedInTotal: form.value.isIncludedInTotal,
    }

    if (isEdit.value && props.transaction) {
      await transactionsApi.update(props.transaction.id, payload)
    } else {
      // 반복 설정이 활성화된 경우 반복 템플릿 먼저 등록
      if (recurring.value.enabled) {
        await recurringApi.create({
          amount,
          categoryId: activeCategoryId.value,
          paymentMethodId: form.value.paymentMethodId,
          type: recurring.value.type,
          dayOfMonth: recurring.value.dayOfMonth,
          totalInstallments: recurring.value.type === 'Installment' ? recurring.value.totalInstallments : undefined,
          memo: form.value.memo || undefined,
        })
      }

      await transactionsApi.create(payload)
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
            <label class="block text-xs text-gray-500 mb-1">금액</label>
            <input
              :value="form.amount"
              @input="onAmountInput"
              type="text"
              inputmode="numeric"
              placeholder="0"
              class="w-full border border-gray-200 rounded-xl px-3 py-2.5 text-lg font-semibold focus:outline-none focus:border-blue-400"
            />
          </div>

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
              v-model="activeCategoryId"
              class="w-full border border-gray-200 rounded-xl px-3 py-2.5 text-sm focus:outline-none focus:border-blue-400"
            >
              <option :value="0">선택하세요</option>
              <option v-for="c in categories" :key="c.id" :value="c.id">{{ c.name }}</option>
            </select>
          </div>

          <div>
            <label class="block text-xs text-gray-500 mb-1">결제수단</label>
            <select
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

        <!-- ── 반복 설정 ── -->
        <div class="px-4 py-3 space-y-3 border-t border-gray-100">
          <div class="flex items-center justify-between">
            <p class="text-xs font-medium text-gray-400 uppercase tracking-wide">반복 설정</p>
            <div
              @click="recurring.enabled = !recurring.enabled"
              :class="recurring.enabled ? 'bg-blue-600' : 'bg-gray-200'"
              class="w-11 h-6 rounded-full transition-colors relative cursor-pointer"
            >
              <div :class="recurring.enabled ? 'translate-x-5' : 'translate-x-0.5'" class="absolute top-0.5 w-5 h-5 bg-white rounded-full shadow transition-transform" />
            </div>
          </div>

          <template v-if="recurring.enabled">
            <div>
              <label class="block text-xs text-gray-500 mb-1">반복 유형</label>
              <div class="flex rounded-xl bg-gray-100 p-1 gap-1">
                <button
                  @click="recurring.type = 'Fixed'"
                  :class="recurring.type === 'Fixed' ? 'bg-white shadow-sm text-gray-800' : 'text-gray-500'"
                  class="flex-1 py-2 text-sm font-medium rounded-lg transition-all"
                >고정</button>
                <button
                  @click="recurring.type = 'Installment'"
                  :class="recurring.type === 'Installment' ? 'bg-white shadow-sm text-gray-800' : 'text-gray-500'"
                  class="flex-1 py-2 text-sm font-medium rounded-lg transition-all"
                >할부</button>
              </div>
            </div>

            <div>
              <label class="block text-xs text-gray-500 mb-1">매월 반복일</label>
              <div class="flex items-center gap-2">
                <input
                  v-model.number="recurring.dayOfMonth"
                  type="number" min="1" max="31"
                  class="w-20 border border-gray-200 rounded-xl px-3 py-2.5 text-sm text-center focus:outline-none focus:border-blue-400"
                />
                <span class="text-sm text-gray-500">일</span>
              </div>
            </div>

            <div v-if="recurring.type === 'Installment'">
              <label class="block text-xs text-gray-500 mb-1">총 할부 횟수</label>
              <div class="flex items-center gap-2">
                <input
                  v-model.number="recurring.totalInstallments"
                  type="number" min="2"
                  placeholder="12"
                  class="w-20 border border-gray-200 rounded-xl px-3 py-2.5 text-sm text-center focus:outline-none focus:border-blue-400"
                />
                <span class="text-sm text-gray-500">회</span>
              </div>
            </div>
          </template>
        </div>

        <!-- ── 부가 정보: 메모 · 합산 포함 ── -->
        <div class="px-4 py-3 space-y-3 border-t border-gray-100">
          <p class="text-xs font-medium text-gray-400 uppercase tracking-wide">부가 정보</p>

          <div>
            <label class="block text-xs text-gray-500 mb-1">메모 (선택)</label>
            <input
              v-model="form.memo"
              type="text"
              placeholder="메모를 입력하세요"
              class="w-full border border-gray-200 rounded-xl px-3 py-2.5 text-sm focus:outline-none focus:border-blue-400"
            />
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
          @click="save"
          :disabled="saving"
          class="w-full bg-blue-600 text-white py-3 rounded-xl font-semibold text-sm hover:bg-blue-700 disabled:opacity-50"
        >
          {{ saving ? '저장 중...' : (isEdit ? '수정' : '추가') }}
        </button>
      </div>
    </div>
  </div>
</template>
