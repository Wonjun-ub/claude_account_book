<script setup lang="ts">
import { ref, onMounted, watch, nextTick } from 'vue'
import { Chart, ArcElement, Tooltip, Legend, CategoryScale, LinearScale, BarElement, Title } from 'chart.js'
import { useAppStore } from '@/stores/app'
import { summaryApi } from '@/api'
import type { CategorySummary, MonthlyTrend } from '@/types'

Chart.register(ArcElement, Tooltip, Legend, CategoryScale, LinearScale, BarElement, Title)

const store = useAppStore()
const categorySummary = ref<CategorySummary[]>([])
const trend = ref<MonthlyTrend[]>([])
const loading = ref(false)

let pieChart: Chart | null = null
let barChart: Chart | null = null

const pieCanvas = ref<HTMLCanvasElement>()
const barCanvas = ref<HTMLCanvasElement>()

const COLORS = [
  '#3B82F6','#EF4444','#10B981','#F59E0B','#8B5CF6',
  '#EC4899','#14B8A6','#F97316','#6366F1','#84CC16',
]

async function loadData() {
  loading.value = true
  try {
    const [cats, tr] = await Promise.all([
      summaryApi.getCategory(store.currentYear, store.currentMonth),
      summaryApi.getTrend(6),
    ])
    categorySummary.value = cats
    trend.value = tr
    await nextTick()
    renderCharts()
  } finally {
    loading.value = false
  }
}

function renderCharts() {
  // 파이차트
  if (pieCanvas.value) {
    pieChart?.destroy()
    const expenses = categorySummary.value.filter(c => c.categoryType === 'Expense')
    if (expenses.length > 0) {
      pieChart = new Chart(pieCanvas.value, {
        type: 'doughnut',
        data: {
          labels: expenses.map(c => c.categoryName),
          datasets: [{
            data: expenses.map(c => c.amount),
            backgroundColor: COLORS.slice(0, expenses.length),
            borderWidth: 0,
          }],
        },
        options: {
          responsive: true,
          plugins: { legend: { position: 'bottom', labels: { font: { size: 12 } } } },
          cutout: '60%',
        },
      })
    }
  }

  // 막대그래프
  if (barCanvas.value && trend.value.length > 0) {
    barChart?.destroy()
    const labels = trend.value.map(t => `${t.month}월`)
    barChart = new Chart(barCanvas.value, {
      type: 'bar',
      data: {
        labels,
        datasets: [
          {
            label: '수입',
            data: trend.value.map(t => t.income),
            backgroundColor: '#93C5FD',
            borderRadius: 4,
          },
          {
            label: '지출',
            data: trend.value.map(t => t.expense),
            backgroundColor: '#FCA5A5',
            borderRadius: 4,
          },
        ],
      },
      options: {
        responsive: true,
        plugins: { legend: { position: 'top' } },
        scales: { y: { beginAtZero: true } },
      },
    })
  }
}

watch([() => store.currentYear, () => store.currentMonth], loadData)
onMounted(loadData)
</script>

<template>
  <div class="max-w-lg mx-auto">
    <!-- 헤더 -->
    <div class="bg-blue-600 text-white px-4 pt-10 pb-4">
      <div class="flex items-center justify-between">
        <button @click="store.prevMonth()" class="p-1 rounded-full hover:bg-blue-500">
          <svg class="w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 19l-7-7 7-7" />
          </svg>
        </button>
        <span class="text-lg font-semibold">{{ store.currentMonthLabel }}</span>
        <button @click="store.nextMonth()" class="p-1 rounded-full hover:bg-blue-500">
          <svg class="w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 5l7 7-7 7" />
          </svg>
        </button>
      </div>
    </div>

    <div v-if="loading" class="py-20 text-center text-gray-400 text-sm">불러오는 중...</div>

    <template v-else>
      <!-- 카테고리별 지출 파이차트 -->
      <div class="mx-4 mt-4 bg-white rounded-2xl p-4 shadow-sm">
        <h3 class="font-semibold text-gray-700 mb-3">카테고리별 지출</h3>
        <div v-if="categorySummary.filter(c => c.categoryType === 'Expense').length === 0"
          class="py-8 text-center text-gray-400 text-sm">지출 내역이 없습니다</div>
        <template v-else>
          <canvas ref="pieCanvas" class="max-h-56" />
          <!-- 카테고리 리스트 -->
          <div class="mt-3 space-y-2">
            <div v-for="(c, i) in categorySummary.filter(x => x.categoryType === 'Expense')" :key="c.categoryId"
              class="flex items-center justify-between text-sm">
              <div class="flex items-center gap-2">
                <div class="w-3 h-3 rounded-full flex-shrink-0" :style="{ backgroundColor: COLORS[i] }" />
                <span class="text-gray-700">{{ c.categoryName }}</span>
              </div>
              <div class="flex items-center gap-2">
                <span class="text-gray-400 text-xs">{{ c.percentage.toFixed(1) }}%</span>
                <span class="font-medium text-gray-800">{{ c.amount.toLocaleString() }}원</span>
              </div>
            </div>
          </div>
        </template>
      </div>

      <!-- 월별 추이 막대그래프 -->
      <div class="mx-4 mt-4 mb-4 bg-white rounded-2xl p-4 shadow-sm">
        <h3 class="font-semibold text-gray-700 mb-3">최근 6개월 추이</h3>
        <div v-if="trend.length === 0" class="py-8 text-center text-gray-400 text-sm">데이터가 없습니다</div>
        <canvas v-else ref="barCanvas" class="max-h-48" />
      </div>
    </template>
  </div>
</template>
