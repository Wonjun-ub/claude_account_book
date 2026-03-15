import { defineStore } from 'pinia'
import { ref, computed } from 'vue'
import dayjs from 'dayjs'
import type { Category, PaymentMethod, PointBudget, UserSettings } from '@/types'
import { categoriesApi, paymentMethodsApi, pointBudgetsApi, settingsApi } from '@/api'

export const useAppStore = defineStore('app', () => {
  // ── 현재 월 네비게이션 ────────────────────────────────────────────────────
  const currentYear = ref(dayjs().year())
  const currentMonth = ref(dayjs().month() + 1)

  function prevMonth() {
    if (currentMonth.value === 1) {
      currentYear.value--
      currentMonth.value = 12
    } else {
      currentMonth.value--
    }
  }

  function nextMonth() {
    if (currentMonth.value === 12) {
      currentYear.value++
      currentMonth.value = 1
    } else {
      currentMonth.value++
    }
  }

  const currentMonthLabel = computed(() =>
    `${currentYear.value}년 ${currentMonth.value}월`
  )

  // ── 공통 마스터 데이터 ────────────────────────────────────────────────────
  const categories = ref<Category[]>([])
  const paymentMethods = ref<PaymentMethod[]>([])
  const pointBudgets = ref<PointBudget[]>([])
  const settings = ref<UserSettings>({ id: 1, monthStartDay: 1 })
  const loading = ref(false)

  async function loadMasterData() {
    loading.value = true

    try {
      const [cats, methods, points, s] = await Promise.all([
        categoriesApi.getAll(),
        paymentMethodsApi.getAll(),
        pointBudgetsApi.getAll(),
        settingsApi.get(),
      ])

      categories.value = cats
      paymentMethods.value = methods
      pointBudgets.value = points
      settings.value = s
    } finally {
      loading.value = false
    }
  }

  const incomeCategories = computed(() => categories.value.filter(c => c.type === 'Income'))
  const expenseCategories = computed(() => categories.value.filter(c => c.type === 'Expense'))

  return {
    currentYear, currentMonth, currentMonthLabel,
    prevMonth, nextMonth,
    categories, paymentMethods, pointBudgets, settings,
    incomeCategories, expenseCategories,
    loading, loadMasterData,
  }
})
