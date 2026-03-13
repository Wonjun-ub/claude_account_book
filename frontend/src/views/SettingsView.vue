<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { useAppStore } from '@/stores/app'
import { useDialog } from '@/composables/useDialog'
import { categoriesApi, paymentMethodsApi, pointBudgetsApi, settingsApi } from '@/api'
import type { CategoryType, PaymentMethodType } from '@/types'

const store = useAppStore()
const { showConfirm, showAlert } = useDialog()

// ── 월 시작일 ─────────────────────────────────────────────────────────────
const monthStartDay = ref(store.settings.monthStartDay)
async function saveMonthStartDay() {
  await settingsApi.update(monthStartDay.value)
  store.settings.monthStartDay = monthStartDay.value
}

// ── 카테고리 ──────────────────────────────────────────────────────────────
const newCatName = ref('')
const newCatType = ref<CategoryType>('Expense')
const catError = ref('')

async function addCategory() {
  catError.value = ''
  if (!newCatName.value.trim()) { catError.value = '이름을 입력하세요.'; return }
  try {
    await categoriesApi.create({ name: newCatName.value.trim(), type: newCatType.value })
    newCatName.value = ''
    await store.loadMasterData()
  } catch {
    catError.value = '저장 중 오류가 발생했습니다.'
  }
}

async function deleteCategory(id: number, isDefault: boolean) {
  if (isDefault) { await showAlert('기본 카테고리는 삭제할 수 없습니다.'); return }
  if (!await showConfirm('카테고리를 삭제하시겠습니까?')) return
  try {
    await categoriesApi.delete(id)
    await store.loadMasterData()
  } catch {
    await showAlert('연결된 거래가 있어 삭제할 수 없습니다.')
  }
}

// ── 결제수단 ──────────────────────────────────────────────────────────────
const newMethodName = ref('')
const newMethodType = ref<PaymentMethodType>('Cash')
const newPointBudgetId = ref<number | undefined>()
const methodError = ref('')

async function addMethod() {
  methodError.value = ''
  if (!newMethodName.value.trim()) { methodError.value = '이름을 입력하세요.'; return }
  if (newMethodType.value === 'Point' && !newPointBudgetId.value) {
    methodError.value = '포인트 예산을 선택하세요.'; return
  }
  try {
    await paymentMethodsApi.create({
      name: newMethodName.value.trim(),
      type: newMethodType.value,
      pointBudgetId: newMethodType.value === 'Point' ? newPointBudgetId.value : undefined,
    })
    newMethodName.value = ''
    newPointBudgetId.value = undefined
    await store.loadMasterData()
  } catch {
    methodError.value = '저장 중 오류가 발생했습니다.'
  }
}

async function deleteMethod(id: number, isDefault: boolean) {
  if (isDefault) { await showAlert('기본 결제수단은 삭제할 수 없습니다.'); return }
  if (!await showConfirm('결제수단을 삭제하시겠습니까?')) return
  try {
    await paymentMethodsApi.delete(id)
    await store.loadMasterData()
  } catch {
    await showAlert('연결된 거래가 있어 삭제할 수 없습니다.')
  }
}

// ── 포인트 예산 ───────────────────────────────────────────────────────────
const newPointName = ref('')
const newPointAmount = ref('')
const pointError = ref('')

async function addPointBudget() {
  pointError.value = ''
  const amount = parseFloat(newPointAmount.value)
  if (!newPointName.value.trim()) { pointError.value = '이름을 입력하세요.'; return }
  if (!amount || amount <= 0) { pointError.value = '금액을 입력하세요.'; return }
  try {
    await pointBudgetsApi.create({ name: newPointName.value.trim(), totalAmount: amount })
    newPointName.value = ''
    newPointAmount.value = ''
    await store.loadMasterData()
  } catch {
    pointError.value = '저장 중 오류가 발생했습니다.'
  }
}

onMounted(() => {
  monthStartDay.value = store.settings.monthStartDay
})
</script>

<template>
  <!-- 화면 전체를 채우는 flex 컬럼 컨테이너 -->
  <div class="h-full flex flex-col max-w-lg mx-auto">

    <!-- ── 고정 헤더 ── -->
    <div class="flex-shrink-0 bg-blue-600 text-white px-4 pt-10 pb-4">
      <h1 class="text-lg font-semibold">설정</h1>
    </div>

    <!-- ── 스크롤 가능한 설정 목록 ── -->
    <div class="flex-1 overflow-y-auto px-4 py-4 space-y-6">

      <!-- 월 시작일 -->
      <section class="bg-white rounded-2xl p-4 shadow-sm">
        <h2 class="font-semibold text-gray-700 mb-3">월 시작일</h2>
        <div class="flex items-center gap-3">
          <input
            v-model.number="monthStartDay"
            type="number" min="1" max="28"
            class="w-20 border border-gray-200 rounded-xl px-3 py-2 text-center text-sm focus:outline-none focus:border-blue-400"
          />
          <span class="text-sm text-gray-500">일부터 한 달로 계산</span>
          <button @click="saveMonthStartDay" class="ml-auto text-sm bg-blue-600 text-white px-4 py-2 rounded-xl">저장</button>
        </div>
      </section>

      <!-- 포인트 예산 -->
      <section class="bg-white rounded-2xl p-4 shadow-sm">
        <h2 class="font-semibold text-gray-700 mb-3">포인트 예산</h2>
        <div v-for="p in store.pointBudgets" :key="p.id" class="flex items-center justify-between py-2 border-b border-gray-50">
          <div>
            <span class="text-sm text-gray-800">{{ p.name }}</span>
            <span class="text-xs text-gray-400 ml-2">잔액 {{ p.remainingAmount.toLocaleString() }}원 / {{ p.totalAmount.toLocaleString() }}원</span>
          </div>
        </div>
        <div class="mt-3 flex gap-2">
          <input v-model="newPointName" placeholder="이름" class="flex-1 border border-gray-200 rounded-xl px-3 py-2 text-sm focus:outline-none focus:border-blue-400" />
          <input v-model="newPointAmount" type="number" placeholder="총액" class="w-28 border border-gray-200 rounded-xl px-3 py-2 text-sm focus:outline-none focus:border-blue-400" />
          <button @click="addPointBudget" class="text-sm bg-blue-600 text-white px-3 py-2 rounded-xl">추가</button>
        </div>
        <p v-if="pointError" class="text-red-500 text-xs mt-1">{{ pointError }}</p>
      </section>

      <!-- 카테고리 -->
      <section class="bg-white rounded-2xl p-4 shadow-sm">
        <h2 class="font-semibold text-gray-700 mb-3">카테고리</h2>
        <div class="mb-2">
          <p class="text-xs text-gray-400 mb-1">지출</p>
          <div class="flex flex-wrap gap-2">
            <div v-for="c in store.expenseCategories" :key="c.id"
              class="flex items-center gap-1 bg-red-50 text-red-700 text-xs px-2.5 py-1.5 rounded-full">
              {{ c.name }}
              <button v-if="!c.isDefault" @click="deleteCategory(c.id, c.isDefault)" class="text-red-400 hover:text-red-600">✕</button>
            </div>
          </div>
        </div>
        <div class="mb-3">
          <p class="text-xs text-gray-400 mb-1">수입</p>
          <div class="flex flex-wrap gap-2">
            <div v-for="c in store.incomeCategories" :key="c.id"
              class="flex items-center gap-1 bg-blue-50 text-blue-700 text-xs px-2.5 py-1.5 rounded-full">
              {{ c.name }}
              <button v-if="!c.isDefault" @click="deleteCategory(c.id, c.isDefault)" class="text-blue-400 hover:text-blue-600">✕</button>
            </div>
          </div>
        </div>
        <div class="flex gap-2">
          <select v-model="newCatType" class="border border-gray-200 rounded-xl px-2 py-2 text-sm focus:outline-none focus:border-blue-400">
            <option value="Expense">지출</option>
            <option value="Income">수입</option>
          </select>
          <input v-model="newCatName" placeholder="카테고리 이름" class="flex-1 border border-gray-200 rounded-xl px-3 py-2 text-sm focus:outline-none focus:border-blue-400" />
          <button @click="addCategory" class="text-sm bg-blue-600 text-white px-3 py-2 rounded-xl">추가</button>
        </div>
        <p v-if="catError" class="text-red-500 text-xs mt-1">{{ catError }}</p>
      </section>

      <!-- 결제수단 -->
      <section class="bg-white rounded-2xl p-4 shadow-sm">
        <h2 class="font-semibold text-gray-700 mb-3">결제수단</h2>
        <div class="flex flex-wrap gap-2 mb-3">
          <div v-for="m in store.paymentMethods" :key="m.id"
            class="flex items-center gap-1 bg-gray-100 text-gray-700 text-xs px-2.5 py-1.5 rounded-full">
            {{ m.name }}
            <span class="text-gray-400">({{ m.type }})</span>
            <button v-if="!m.isDefault" @click="deleteMethod(m.id, m.isDefault)" class="text-gray-400 hover:text-red-500">✕</button>
          </div>
        </div>
        <div class="space-y-2">
          <div class="flex gap-2">
            <select v-model="newMethodType" class="border border-gray-200 rounded-xl px-2 py-2 text-sm focus:outline-none focus:border-blue-400">
              <option value="Cash">현금</option>
              <option value="Card">카드</option>
              <option value="Point">포인트</option>
            </select>
            <input v-model="newMethodName" placeholder="결제수단 이름" class="flex-1 border border-gray-200 rounded-xl px-3 py-2 text-sm focus:outline-none focus:border-blue-400" />
            <button @click="addMethod" class="text-sm bg-blue-600 text-white px-3 py-2 rounded-xl">추가</button>
          </div>
          <div v-if="newMethodType === 'Point'">
            <select v-model="newPointBudgetId" class="w-full border border-gray-200 rounded-xl px-2 py-2 text-sm focus:outline-none focus:border-blue-400">
              <option :value="undefined">포인트 예산 선택</option>
              <option v-for="p in store.pointBudgets" :key="p.id" :value="p.id">{{ p.name }}</option>
            </select>
          </div>
        </div>
        <p v-if="methodError" class="text-red-500 text-xs mt-1">{{ methodError }}</p>
      </section>

    </div>
  </div>
</template>
