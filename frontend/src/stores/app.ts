import { defineStore } from 'pinia'
import { ref, computed } from 'vue'
import dayjs from 'dayjs'
import { db } from '@/database/db'

export const useSettingsStore = defineStore('settings', () => {
  // ── 현재 월 네비게이션 ────────────────────────────────────────────────────
  const currentYear  = ref(dayjs().year())
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

  const currentMonthLabel = computed(
    () => `${currentYear.value}년 ${currentMonth.value}월`,
  )

  // ── 설정 데이터 ───────────────────────────────────────────────────────────
  const monthStartDay = ref(1)
  const settingsId    = ref<number | undefined>(undefined)

  async function loadSettings(): Promise<void> {
    const all = await db.userSettings.toArray()
    if (all.length > 0) {
      const s = all[0]!
      settingsId.value    = s.id
      monthStartDay.value = s.monthStartDay
    }
  }

  async function saveMonthStartDay(day: number): Promise<void> {
    if (settingsId.value !== undefined) {
      await db.userSettings.update(settingsId.value, { monthStartDay: day })
    } else {
      const id = await db.userSettings.add({ monthStartDay: day })
      settingsId.value = id as number
    }
    monthStartDay.value = day
  }

  return {
    currentYear,
    currentMonth,
    currentMonthLabel,
    prevMonth,
    nextMonth,
    monthStartDay,
    loadSettings,
    saveMonthStartDay,
  }
})

// 기존 이름으로도 import 가능하도록 alias export
export { useSettingsStore as useAppStore }
