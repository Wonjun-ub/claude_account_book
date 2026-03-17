<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { useSettingsStore } from '@/stores/app'
import { useCategoryStore } from '@/stores/category'
import { usePaymentMethodStore } from '@/stores/paymentMethod'
import { useDialog } from '@/composables/useDialog'
import type { TransactionType, PaymentMethodType } from '@/database/db'

const settingsStore = useSettingsStore()
const categoryStore = useCategoryStore()
const pmStore       = usePaymentMethodStore()
const { showConfirm, showAlert } = useDialog()

// ── 섹션 토글 ────────────────────────────────────────────────────────────────
const openSections = ref<Set<string>>(new Set())
function toggleSection(key: string) {
  if (openSections.value.has(key)) openSections.value.delete(key)
  else openSections.value.add(key)
}
function isSectionOpen(key: string) { return openSections.value.has(key) }

// ── 월 시작일 ─────────────────────────────────────────────────────────────────
const editingMonthStartDay = ref(settingsStore.monthStartDay)
async function saveMonthStartDay() {
  await settingsStore.saveMonthStartDay(editingMonthStartDay.value)
}

// ── 카테고리 ──────────────────────────────────────────────────────────────────
const catFilter   = ref<TransactionType>('Expense')
const newCatName  = ref('')
const catError    = ref('')

async function addCategory() {
  catError.value = ''
  if (!newCatName.value.trim()) { catError.value = '이름을 입력하세요.'; return }
  await categoryStore.addCategory(newCatName.value.trim(), catFilter.value)
  newCatName.value = ''
}

async function deleteCategory(id: number, isDefault: boolean) {
  if (isDefault) { await showAlert('기본 카테고리는 삭제할 수 없습니다.'); return }
  if (!await showConfirm('카테고리를 삭제하시겠습니까?')) return
  try {
    await categoryStore.deleteCategory(id)
  } catch (e: unknown) {
    await showAlert(e instanceof Error ? e.message : '삭제할 수 없습니다.')
  }
}

// ── 결제수단 ──────────────────────────────────────────────────────────────────
const newMethodName = ref('')
const newMethodType = ref<PaymentMethodType>('Cash')
const methodError   = ref('')
const expandedMethodId = ref<number | null>(null)

// 카드 청구 설정 편집용
const editingBilling = ref<{ id: number; cutoffDay: number; dueDay: number } | null>(null)
// 포인트 예산 편집용
const editingPoint   = ref<{ id: number; totalBudget: number; remainingBudget: number } | null>(null)

async function addMethod() {
  methodError.value = ''
  if (!newMethodName.value.trim()) { methodError.value = '이름을 입력하세요.'; return }
  try {
    await pmStore.addPaymentMethod({ name: newMethodName.value.trim(), type: newMethodType.value })
    newMethodName.value = ''
  } catch (e: unknown) {
    methodError.value = e instanceof Error ? e.message : '저장 중 오류가 발생했습니다.'
  }
}

async function deleteMethod(id: number, isDefault: boolean) {
  if (isDefault) { await showAlert('기본 결제수단은 삭제할 수 없습니다.'); return }
  if (!await showConfirm('결제수단을 삭제하시겠습니까?')) return
  try {
    await pmStore.deletePaymentMethod(id)
  } catch (e: unknown) {
    await showAlert(e instanceof Error ? e.message : '연결된 거래가 있어 삭제할 수 없습니다.')
  }
}

function openBillingEdit(id: number) {
  const pm = pmStore.paymentMethods.find(m => m.id === id)
  if (!pm) return
  editingBilling.value = {
    id,
    cutoffDay: pm.billingCutoffDay ?? 15,
    dueDay:    pm.paymentDueDay    ?? 25,
  }
}

async function saveBillingSettings() {
  if (!editingBilling.value) return
  await pmStore.updateBillingSettings(
    editingBilling.value.id,
    editingBilling.value.cutoffDay,
    editingBilling.value.dueDay,
  )
  editingBilling.value = null
}

function openPointEdit(id: number) {
  const pm = pmStore.paymentMethods.find(m => m.id === id)
  if (!pm) return
  editingPoint.value = {
    id,
    totalBudget:     pm.totalBudget     ?? 0,
    remainingBudget: pm.remainingBudget ?? 0,
  }
}

async function savePointBudget() {
  if (!editingPoint.value) return
  await pmStore.updatePointBudget(
    editingPoint.value.id,
    editingPoint.value.totalBudget,
    editingPoint.value.remainingBudget,
  )
  editingPoint.value = null
}

// ── 저축 수단 ─────────────────────────────────────────────────────────────────
const newSavingsName = ref('')
const savingsError   = ref('')

async function addSavingsMethod() {
  savingsError.value = ''
  if (!newSavingsName.value.trim()) { savingsError.value = '이름을 입력하세요.'; return }
  await pmStore.addSavingsMethod(newSavingsName.value.trim())
  newSavingsName.value = ''
}

async function deleteSavingsMethod(id: number, isDefault: boolean) {
  if (isDefault) { await showAlert('기본 저축 수단은 삭제할 수 없습니다.'); return }
  if (!await showConfirm('저축 수단을 삭제하시겠습니까?')) return
  try {
    await pmStore.deleteSavingsMethod(id)
  } catch (e: unknown) {
    await showAlert(e instanceof Error ? e.message : '연결된 거래가 있어 삭제할 수 없습니다.')
  }
}

onMounted(() => {
  editingMonthStartDay.value = settingsStore.monthStartDay
})
</script>

<template>
  <div class="h-full flex flex-col max-w-lg mx-auto bg-gray-900">

    <!-- 헤더 -->
    <div class="flex-shrink-0 bg-gray-900 px-4 py-3 border-b border-gray-700/50 pt-12">
      <h2 class="text-base font-semibold text-gray-100">설정</h2>
    </div>

    <!-- 스크롤 영역 -->
    <div class="flex-1 overflow-y-auto px-4 py-4 space-y-4">

      <!-- 월 시작일 -->
      <div class="bg-gray-800 rounded-2xl overflow-hidden">
        <button @click="toggleSection('monthStart')" class="w-full flex items-center justify-between px-4 py-3.5">
          <div class="flex items-center gap-3">
            <span class="text-sm font-semibold text-gray-100">월 시작일</span>
            <span class="text-xs text-gray-400">{{ settingsStore.monthStartDay }}일 기준</span>
          </div>
          <svg class="w-4 h-4 text-gray-400 transition-transform duration-200" :class="isSectionOpen('monthStart') ? 'rotate-180' : ''" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 9l-7 7-7-7"/></svg>
        </button>
        <div v-show="isSectionOpen('monthStart')" class="px-4 pb-4 border-t border-gray-700/50">
          <div class="flex items-center gap-3 pt-3">
            <select v-model.number="editingMonthStartDay" class="bg-gray-700 text-gray-100 text-sm rounded-xl px-3 py-2 outline-none">
              <option v-for="d in 28" :key="d" :value="d">{{ d }}일</option>
            </select>
            <span class="text-xs text-gray-400 flex-1">매월 {{ editingMonthStartDay }}일 기준</span>
            <button @click="saveMonthStartDay" class="bg-blue-600 text-white text-xs px-3 py-2 rounded-xl">저장</button>
          </div>
        </div>
      </div>

      <!-- 카테고리 관리 -->
      <div class="bg-gray-800 rounded-2xl overflow-hidden">
        <button @click="toggleSection('category')" class="w-full flex items-center justify-between px-4 py-3.5">
          <div class="flex items-center gap-3">
            <span class="text-sm font-semibold text-gray-100">카테고리</span>
            <span class="text-xs text-gray-400">{{ categoryStore.categories.length }}개</span>
          </div>
          <svg class="w-4 h-4 text-gray-400 transition-transform duration-200" :class="isSectionOpen('category') ? 'rotate-180' : ''" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 9l-7 7-7-7"/></svg>
        </button>
        <div v-show="isSectionOpen('category')" class="px-4 pb-4 border-t border-gray-700/50">
          <!-- 탭 -->
          <div class="flex rounded-xl bg-gray-700 p-1 mt-3 mb-3">
            <button @click="catFilter = 'Expense'" :class="catFilter === 'Expense' ? 'bg-red-500 text-white shadow-sm' : 'text-gray-400'" class="flex-1 py-1 text-xs font-medium rounded-lg transition-all">지출</button>
            <button @click="catFilter = 'Income'"  :class="catFilter === 'Income'  ? 'bg-blue-500 text-white shadow-sm' : 'text-gray-400'" class="flex-1 py-1 text-xs font-medium rounded-lg transition-all">수입</button>
            <button @click="catFilter = 'Savings'" :class="catFilter === 'Savings' ? 'bg-emerald-500 text-white shadow-sm' : 'text-gray-400'" class="flex-1 py-1 text-xs font-medium rounded-lg transition-all">저축</button>
          </div>
          <!-- 목록 -->
          <div class="space-y-1.5 mb-3">
            <div
              v-for="cat in categoryStore.categories.filter(c => c.type === catFilter)"
              :key="cat.id"
              class="flex items-center justify-between bg-gray-700/60 rounded-xl px-3 py-2"
            >
              <span class="text-sm text-gray-200">{{ cat.name }}</span>
              <span v-if="cat.isDefault" class="text-[10px] text-gray-500 bg-gray-700 px-1.5 py-0.5 rounded">기본</span>
              <button v-else @click="deleteCategory(cat.id, cat.isDefault)" class="text-gray-500 hover:text-red-400 p-1">
                <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12"/></svg>
              </button>
            </div>
            <p v-if="categoryStore.categories.filter(c => c.type === catFilter).length === 0" class="text-xs text-gray-500 text-center py-2">항목 없음</p>
          </div>
          <!-- 추가 폼 -->
          <div class="flex gap-2">
            <input v-model="newCatName" placeholder="카테고리명" @keyup.enter="addCategory"
              class="flex-1 bg-gray-700 text-gray-100 text-sm rounded-xl px-3 py-2 placeholder-gray-500 outline-none" />
            <button @click="addCategory" class="bg-blue-600 text-white text-xs px-3 py-2 rounded-xl">추가</button>
          </div>
          <p v-if="catError" class="text-red-400 text-xs mt-1.5">{{ catError }}</p>
        </div>
      </div>

      <!-- 결제수단 관리 -->
      <div class="bg-gray-800 rounded-2xl overflow-hidden">
        <button @click="toggleSection('paymentMethod')" class="w-full flex items-center justify-between px-4 py-3.5">
          <div class="flex items-center gap-3">
            <span class="text-sm font-semibold text-gray-100">결제수단</span>
            <span class="text-xs text-gray-400">{{ pmStore.paymentMethods.length }}개</span>
          </div>
          <svg class="w-4 h-4 text-gray-400 transition-transform duration-200" :class="isSectionOpen('paymentMethod') ? 'rotate-180' : ''" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 9l-7 7-7-7"/></svg>
        </button>
        <div v-show="isSectionOpen('paymentMethod')" class="px-4 pb-4 border-t border-gray-700/50">
          <div class="space-y-1.5 mt-3 mb-3">
            <div v-for="m in pmStore.paymentMethods" :key="m.id" class="bg-gray-700/60 rounded-xl overflow-hidden">
              <!-- 항목 행 -->
              <div class="flex items-center justify-between px-3 py-2">
                <button
                  v-if="m.type === 'CreditCard' || m.type === 'DebitCard' || m.type === 'Point'"
                  @click="expandedMethodId = expandedMethodId === m.id ? null : m.id"
                  class="flex items-center gap-2 flex-1 min-w-0"
                >
                  <span class="text-sm text-gray-200">{{ m.name }}</span>
                  <span class="text-[10px] px-1.5 py-0.5 rounded"
                    :class="m.type === 'Point' ? 'bg-yellow-900/50 text-yellow-300' : 'bg-blue-900/50 text-blue-300'">
                    {{ m.type === 'CreditCard' ? '신용카드' : m.type === 'DebitCard' ? '체크카드' : '포인트' }}
                  </span>
                  <svg class="w-3.5 h-3.5 text-gray-400 ml-auto transition-transform" :class="expandedMethodId === m.id ? 'rotate-180' : ''" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 9l-7 7-7-7"/></svg>
                </button>
                <div v-else class="flex items-center gap-2 flex-1">
                  <span class="text-sm text-gray-200">{{ m.name }}</span>
                  <span class="text-[10px] px-1.5 py-0.5 rounded bg-gray-600/60 text-gray-300">현금</span>
                </div>
                <button @click="deleteMethod(m.id, m.isDefault)" class="text-gray-500 hover:text-red-400 p-1 ml-1 flex-shrink-0">
                  <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12"/></svg>
                </button>
              </div>

              <!-- 카드 청구 설정 인라인 -->
              <div v-if="expandedMethodId === m.id && (m.type === 'CreditCard' || m.type === 'DebitCard')" class="border-t border-gray-700 px-3 pb-3 pt-2">
                <div v-if="editingBilling?.id === m.id" class="space-y-2">
                  <div class="flex items-center gap-2">
                    <span class="text-xs text-gray-400 w-16 flex-shrink-0">정산일</span>
                    <select v-model.number="editingBilling.cutoffDay" class="bg-gray-600 text-gray-100 text-xs rounded-lg px-2 py-1.5 outline-none">
                      <option v-for="d in 28" :key="d" :value="d">{{ d }}일</option>
                    </select>
                    <span class="text-xs text-gray-500">이용 마감일</span>
                  </div>
                  <div class="flex items-center gap-2">
                    <span class="text-xs text-gray-400 w-16 flex-shrink-0">결제일</span>
                    <select v-model.number="editingBilling.dueDay" class="bg-gray-600 text-gray-100 text-xs rounded-lg px-2 py-1.5 outline-none">
                      <option v-for="d in 28" :key="d" :value="d">{{ d }}일</option>
                    </select>
                    <span class="text-xs text-gray-500">출금일</span>
                  </div>
                  <p class="text-[10px] text-gray-500">
                    전월 {{ editingBilling.cutoffDay + 1 }}일 ~ 당월 {{ editingBilling.cutoffDay }}일 이용분 → {{ editingBilling.dueDay }}일 출금
                  </p>
                  <div class="flex gap-2 pt-1">
                    <button @click="saveBillingSettings" class="flex-1 bg-blue-600 text-white text-xs py-2 rounded-xl">저장</button>
                    <button @click="editingBilling = null" class="flex-1 bg-gray-600 text-gray-300 text-xs py-2 rounded-xl">취소</button>
                  </div>
                </div>
                <div v-else>
                  <p class="text-xs text-gray-400 mb-2">
                    <template v-if="m.billingCutoffDay && m.paymentDueDay">
                      전월 {{ m.billingCutoffDay + 1 }}일 ~ 당월 {{ m.billingCutoffDay }}일 → {{ m.paymentDueDay }}일 출금
                    </template>
                    <template v-else>청구 설정이 없습니다</template>
                  </p>
                  <button @click="openBillingEdit(m.id)" class="w-full bg-gray-600 text-gray-300 text-xs py-2 rounded-xl">청구 설정</button>
                </div>
              </div>

              <!-- 포인트 예산 설정 인라인 -->
              <div v-if="expandedMethodId === m.id && m.type === 'Point'" class="border-t border-gray-700 px-3 pb-3 pt-2">
                <div v-if="editingPoint?.id === m.id" class="space-y-2">
                  <div class="flex items-center gap-2">
                    <span class="text-xs text-gray-400 w-16 flex-shrink-0">총 예산</span>
                    <input v-model.number="editingPoint.totalBudget" type="number" class="flex-1 bg-gray-600 text-gray-100 text-xs rounded-lg px-2 py-1.5 outline-none" />
                  </div>
                  <div class="flex items-center gap-2">
                    <span class="text-xs text-gray-400 w-16 flex-shrink-0">현재 잔액</span>
                    <input v-model.number="editingPoint.remainingBudget" type="number" class="flex-1 bg-gray-600 text-gray-100 text-xs rounded-lg px-2 py-1.5 outline-none" />
                  </div>
                  <div class="flex gap-2 pt-1">
                    <button @click="savePointBudget" class="flex-1 bg-blue-600 text-white text-xs py-2 rounded-xl">저장</button>
                    <button @click="editingPoint = null" class="flex-1 bg-gray-600 text-gray-300 text-xs py-2 rounded-xl">취소</button>
                  </div>
                </div>
                <div v-else>
                  <p class="text-xs text-gray-400 mb-2">
                    <template v-if="m.totalBudget !== undefined">
                      잔액 {{ (m.remainingBudget ?? 0).toLocaleString() }}원 / 총 {{ m.totalBudget.toLocaleString() }}원
                    </template>
                    <template v-else>예산 설정이 없습니다</template>
                  </p>
                  <button @click="openPointEdit(m.id)" class="w-full bg-gray-600 text-gray-300 text-xs py-2 rounded-xl">예산 설정</button>
                </div>
              </div>
            </div>
          </div>
          <!-- 추가 폼 -->
          <div class="flex gap-2">
            <select v-model="newMethodType" class="bg-gray-700 text-gray-100 text-xs rounded-xl px-2 py-2 outline-none">
              <option value="Cash">현금</option>
              <option value="CreditCard">신용카드</option>
              <option value="DebitCard">체크카드</option>
              <option value="Point">포인트</option>
            </select>
            <input v-model="newMethodName" placeholder="결제수단 이름" @keyup.enter="addMethod"
              class="flex-1 bg-gray-700 text-gray-100 text-sm rounded-xl px-3 py-2 placeholder-gray-500 outline-none" />
            <button @click="addMethod" class="bg-blue-600 text-white text-xs px-3 py-2 rounded-xl">추가</button>
          </div>
          <p v-if="methodError" class="text-red-400 text-xs mt-1.5">{{ methodError }}</p>
        </div>
      </div>

      <!-- 저축 수단 관리 -->
      <div class="bg-gray-800 rounded-2xl overflow-hidden">
        <button @click="toggleSection('savings')" class="w-full flex items-center justify-between px-4 py-3.5">
          <div class="flex items-center gap-3">
            <span class="text-sm font-semibold text-gray-100">저축 수단</span>
            <span class="text-xs text-gray-400">{{ pmStore.savingsMethods.length }}개</span>
          </div>
          <svg class="w-4 h-4 text-gray-400 transition-transform duration-200" :class="isSectionOpen('savings') ? 'rotate-180' : ''" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 9l-7 7-7-7"/></svg>
        </button>
        <div v-show="isSectionOpen('savings')" class="px-4 pb-4 border-t border-gray-700/50">
          <div class="space-y-1.5 mt-3 mb-3">
            <div
              v-for="s in pmStore.savingsMethods"
              :key="s.id"
              class="flex items-center justify-between bg-gray-700/60 rounded-xl px-3 py-2"
            >
              <span class="text-sm text-gray-200">{{ s.name }}</span>
              <button @click="deleteSavingsMethod(s.id, s.isDefault)" class="text-gray-500 hover:text-red-400 p-1">
                <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12"/></svg>
              </button>
            </div>
            <p v-if="pmStore.savingsMethods.length === 0" class="text-xs text-gray-500 text-center py-2">항목 없음</p>
          </div>
          <div class="flex gap-2">
            <input v-model="newSavingsName" placeholder="저축 수단 이름" @keyup.enter="addSavingsMethod"
              class="flex-1 bg-gray-700 text-gray-100 text-sm rounded-xl px-3 py-2 placeholder-gray-500 outline-none" />
            <button @click="addSavingsMethod" class="bg-blue-600 text-white text-xs px-3 py-2 rounded-xl">추가</button>
          </div>
          <p v-if="savingsError" class="text-red-400 text-xs mt-1.5">{{ savingsError }}</p>
        </div>
      </div>

    </div>
  </div>
</template>
