<script setup lang="ts">
import { ref, computed, watch } from 'vue'
import dayjs from 'dayjs'
import { useAppStore } from '@/stores/app'
import { transactionsApi } from '@/api'
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
  amount: '',
  date: dayjs().format('YYYY-MM-DD'),
  categoryId: 0,
  paymentMethodId: 0,
  memo: '',
  isIncludedInTotal: true,
})

const saving = ref(false)
const error = ref('')

const isEdit = computed(() => !!props.transaction)

const categories = computed(() =>
  form.value.type === 'Income' ? store.incomeCategories : store.expenseCategories
)

watch(() => props.transaction, (tx) => {
  if (tx) {
    form.value = {
      type: tx.type,
      amount: String(tx.amount),
      date: dayjs(tx.date).format('YYYY-MM-DD'),
      categoryId: tx.categoryId,
      paymentMethodId: tx.paymentMethodId,
      memo: tx.memo ?? '',
      isIncludedInTotal: tx.isIncludedInTotal,
    }
  } else {
    form.value = {
      type: 'Expense',
      amount: '',
      date: dayjs().format('YYYY-MM-DD'),
      categoryId: 0,
      paymentMethodId: 0,
      memo: '',
      isIncludedInTotal: true,
    }
  }
}, { immediate: true })

// 타입 변경 시 카테고리 초기화
watch(() => form.value.type, () => {
  form.value.categoryId = 0
})

async function save() {
  error.value = ''
  const amount = parseFloat(form.value.amount)
  if (!amount || amount <= 0) { error.value = '금액을 입력해주세요.'; return }
  if (!form.value.categoryId) { error.value = '카테고리를 선택해주세요.'; return }
  if (!form.value.paymentMethodId) { error.value = '결제수단을 선택해주세요.'; return }

  saving.value = true
  try {
    const payload = {
      amount,
      date: form.value.date,
      memo: form.value.memo || undefined,
      type: form.value.type,
      categoryId: form.value.categoryId,
      paymentMethodId: form.value.paymentMethodId,
      isIncludedInTotal: form.value.isIncludedInTotal,
    }
    if (isEdit.value && props.transaction) {
      await transactionsApi.update(props.transaction.id, payload)
    } else {
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
  <div class="fixed inset-0 bg-black/50 z-50 flex items-end justify-center" @click.self="emit('close')">
    <div class="bg-white rounded-t-2xl w-full max-w-lg max-h-[90vh] overflow-y-auto">
      <!-- 헤더 -->
      <div class="flex items-center justify-between px-4 py-4 border-b border-gray-100">
        <h2 class="font-semibold text-gray-800">{{ isEdit ? '거래 수정' : '거래 추가' }}</h2>
        <button @click="emit('close')" class="text-gray-400 hover:text-gray-600">
          <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12" />
          </svg>
        </button>
      </div>

      <div class="px-4 py-4 space-y-4">
        <!-- 수입/지출 탭 -->
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

        <!-- 금액 -->
        <div>
          <label class="block text-xs text-gray-500 mb-1">금액</label>
          <input
            v-model="form.amount"
            type="number"
            placeholder="0"
            class="w-full border border-gray-200 rounded-xl px-3 py-2.5 text-lg font-semibold focus:outline-none focus:border-blue-400"
          />
        </div>

        <!-- 날짜 -->
        <div>
          <label class="block text-xs text-gray-500 mb-1">날짜</label>
          <input
            v-model="form.date"
            type="date"
            class="w-full border border-gray-200 rounded-xl px-3 py-2.5 text-sm focus:outline-none focus:border-blue-400"
          />
        </div>

        <!-- 카테고리 -->
        <div>
          <label class="block text-xs text-gray-500 mb-1">카테고리</label>
          <select
            v-model="form.categoryId"
            class="w-full border border-gray-200 rounded-xl px-3 py-2.5 text-sm focus:outline-none focus:border-blue-400"
          >
            <option :value="0">선택하세요</option>
            <option v-for="c in categories" :key="c.id" :value="c.id">{{ c.name }}</option>
          </select>
        </div>

        <!-- 결제수단 -->
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

        <!-- 메모 -->
        <div>
          <label class="block text-xs text-gray-500 mb-1">메모 (선택)</label>
          <input
            v-model="form.memo"
            type="text"
            placeholder="메모를 입력하세요"
            class="w-full border border-gray-200 rounded-xl px-3 py-2.5 text-sm focus:outline-none focus:border-blue-400"
          />
        </div>

        <!-- 합산 포함 여부 -->
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

        <!-- 오류 메시지 -->
        <p v-if="error" class="text-red-500 text-sm">{{ error }}</p>

        <!-- 저장 버튼 -->
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
