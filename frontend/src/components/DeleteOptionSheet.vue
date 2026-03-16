<script setup lang="ts">
import type { Transaction } from '@/types'
import { useDateFormat } from '@/composables/useDateFormat'

const props = defineProps<{
  type: 'installment' | 'recurring'
  transaction: Transaction
}>()

const emit = defineEmits<{
  deleteAll: []
  deleteFromHere: []
  deleteSingle: []
  close: []
}>()

const { formatYearMonth } = useDateFormat()
</script>

<template>
  <div
    class="fixed inset-0 bg-black/40 z-[60] flex items-end justify-center"
    @click.self="emit('close')"
  >
    <div class="bg-white rounded-t-2xl w-full max-w-lg" @click.stop>
      <!-- 핸들 -->
      <div class="flex justify-center pt-3 pb-2">
        <div class="w-10 h-1 bg-gray-200 rounded-full" />
      </div>

      <!-- 할부 삭제 -->
      <template v-if="type === 'installment'">
        <div class="px-5 pb-2">
          <p class="text-sm font-semibold text-gray-800">할부 삭제</p>
          <p class="text-xs text-gray-400 mt-0.5">
            {{ transaction.categoryName }} ·
            {{ transaction.installmentSequence }}/{{ transaction.installmentTotalInstallments }}회차
          </p>
        </div>
        <div class="px-4 pb-6 space-y-2">
          <button
            @click="emit('deleteAll')"
            class="w-full py-3.5 bg-red-500 text-white rounded-xl text-sm font-semibold hover:bg-red-600 text-left px-4"
          >
            전체 삭제
            <span class="block text-xs font-normal text-red-100 mt-0.5">모든 회차 데이터 삭제</span>
          </button>
          <button
            @click="emit('deleteFromHere')"
            class="w-full py-3.5 border border-red-200 text-red-500 rounded-xl text-sm font-semibold hover:bg-red-50 text-left px-4"
          >
            이후 삭제
            <span class="block text-xs font-normal text-red-400 mt-0.5">
              {{ transaction.installmentSequence }}회차부터 이후 회차 삭제
            </span>
          </button>
          <button
            @click="emit('deleteSingle')"
            class="w-full py-3.5 border border-gray-200 text-gray-700 rounded-xl text-sm font-semibold hover:bg-gray-50 text-left px-4"
          >
            단건 삭제
            <span class="block text-xs font-normal text-gray-400 mt-0.5">
              {{ transaction.installmentSequence }}회차만 삭제
            </span>
          </button>
          <button @click="emit('close')" class="w-full py-2.5 text-sm text-gray-400">취소</button>
        </div>
      </template>

      <!-- 반복 삭제 -->
      <template v-else>
        <div class="px-5 pb-2">
          <p class="text-sm font-semibold text-gray-800">반복 삭제</p>
          <p class="text-xs text-gray-400 mt-0.5">
            {{ transaction.categoryName }}
          </p>
        </div>
        <div class="px-4 pb-6 space-y-2">
          <button
            @click="emit('deleteAll')"
            class="w-full py-3.5 bg-red-500 text-white rounded-xl text-sm font-semibold hover:bg-red-600 text-left px-4"
          >
            전체 삭제
            <span class="block text-xs font-normal text-red-100 mt-0.5">등록된 모든 반복 거래 삭제 + 반복 중단</span>
          </button>
          <button
            @click="emit('deleteFromHere')"
            class="w-full py-3.5 border border-red-200 text-red-500 rounded-xl text-sm font-semibold hover:bg-red-50 text-left px-4"
          >
            이후 삭제
            <span class="block text-xs font-normal text-red-400 mt-0.5">
              {{ formatYearMonth(transaction.date) }}부터 이후 거래 삭제 + 반복 중단
            </span>
          </button>
          <button
            @click="emit('deleteSingle')"
            class="w-full py-3.5 border border-gray-200 text-gray-700 rounded-xl text-sm font-semibold hover:bg-gray-50 text-left px-4"
          >
            단건 삭제
            <span class="block text-xs font-normal text-gray-400 mt-0.5">이번 달 거래만 삭제 (반복 유지)</span>
          </button>
          <button @click="emit('close')" class="w-full py-2.5 text-sm text-gray-400">취소</button>
        </div>
      </template>
    </div>
  </div>
</template>
