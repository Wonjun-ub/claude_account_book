<script setup lang="ts">
import { useCurrencyFormat } from '@/composables/useCurrencyFormat'
import type { TransactionView } from '@/types'

defineProps<{
  transaction: TransactionView
}>()

const emit = defineEmits<{
  edit:   [tx: TransactionView]
  delete: [tx: TransactionView]
}>()

const { formatAmount } = useCurrencyFormat()
</script>

<template>
  <div
    data-testid="transaction-item"
    class="bg-white rounded-xl p-3 mb-2 flex items-center shadow-sm cursor-pointer active:bg-gray-50"
    :class="!transaction.isIncludedInTotal ? 'opacity-50' : ''"
    @click="emit('edit', transaction)"
  >
    <div class="flex-1 min-w-0">
      <div class="flex items-center gap-1.5 flex-wrap">
        <span class="text-sm font-medium truncate">{{ transaction.categoryName }}</span>
        <span v-if="!transaction.isIncludedInTotal" class="text-xs bg-gray-100 text-gray-500 px-1.5 py-0.5 rounded">제외</span>
        <span v-if="transaction.installmentTransactionId" class="text-xs bg-orange-50 text-orange-500 px-1.5 py-0.5 rounded">
          할부 {{ transaction.installmentSequence }}/{{ transaction.installmentTotalInstallments }}회
        </span>
        <span v-else-if="transaction.recurringTransactionId" class="text-xs bg-blue-50 text-blue-500 px-1.5 py-0.5 rounded">반복</span>
      </div>
      <div class="text-xs text-gray-400 mt-0.5">
        <template v-if="transaction.type === 'Expense'">
          {{ transaction.paymentMethodName }}<span v-if="transaction.memo"> · {{ transaction.memo }}</span>
        </template>
        <template v-else-if="transaction.type === 'Savings'">
          {{ transaction.savingsMethodName }}<span v-if="transaction.memo"> · {{ transaction.memo }}</span>
        </template>
        <span v-else-if="transaction.memo">{{ transaction.memo }}</span>
      </div>
    </div>
    <div class="flex items-center gap-2">
      <span
        :class="transaction.type === 'Income' ? 'text-blue-600' : transaction.type === 'Expense' ? 'text-red-500' : 'text-emerald-600'"
        class="font-semibold text-sm"
      >
        {{ formatAmount(transaction.amount, transaction.type) }}
      </span>
      <button @click.stop="emit('delete', transaction)" class="text-gray-300 hover:text-red-400 p-1">
        <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12" />
        </svg>
      </button>
    </div>
  </div>
</template>
