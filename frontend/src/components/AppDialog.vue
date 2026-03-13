<script setup lang="ts">
import { useDialog } from '@/composables/useDialog'

const { visible, message, isConfirm, handleOk, handleCancel } = useDialog()
</script>

<template>
  <Teleport to="body">
    <div v-if="visible" class="fixed inset-0 z-50 flex items-center justify-center px-6">
      <!-- 배경 오버레이 -->
      <div class="absolute inset-0 bg-black/40" @click="isConfirm ? handleCancel() : handleOk()" />

      <!-- 다이얼로그 -->
      <div class="relative bg-white rounded-2xl shadow-xl w-full max-w-xs overflow-hidden">
        <div class="px-6 pt-6 pb-4">
          <p class="text-sm text-gray-800 text-center leading-relaxed">{{ message }}</p>
        </div>
        <div class="flex border-t border-gray-100">
          <button
            v-if="isConfirm"
            @click="handleCancel"
            class="flex-1 py-3 text-sm text-gray-500 font-medium hover:bg-gray-50 transition-colors"
          >
            취소
          </button>
          <button
            @click="handleOk"
            class="flex-1 py-3 text-sm font-semibold transition-colors"
            :class="isConfirm ? 'text-red-500 hover:bg-red-50 border-l border-gray-100' : 'text-blue-600 hover:bg-blue-50'"
          >
            확인
          </button>
        </div>
      </div>
    </div>
  </Teleport>
</template>
