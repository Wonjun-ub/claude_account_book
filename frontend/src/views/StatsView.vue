<script setup lang="ts">
import { ref, watch, onMounted } from 'vue'
import {
  Chart, ArcElement, Tooltip, Legend,
  CategoryScale, LinearScale,
  LineElement, PointElement, Filler, Title,
} from 'chart.js'
import { useAppStore } from '@/stores/app'
import { summaryApi } from '@/api'
import type { CategorySummary, MonthlyTrend } from '@/types'

Chart.register(ArcElement, Tooltip, Legend, CategoryScale, LinearScale, LineElement, PointElement, Filler, Title)

const store = useAppStore()
const categorySummary = ref<CategorySummary[]>([])
const trend = ref<MonthlyTrend[]>([])
const loading = ref(false)

let pieChart: Chart | null = null
let lineChart: Chart | null = null

const pieCanvas = ref<HTMLCanvasElement | null>(null)
const lineCanvas = ref<HTMLCanvasElement | null>(null)

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
  } finally {
    loading.value = false
  }
}

// flush: 'post' — DOM 업데이트 완료 후 실행 보장
watch(categorySummary, renderPie, { flush: 'post' })
watch(trend, renderLine, { flush: 'post' })

function renderPie() {
  if (!pieCanvas.value) return
  pieChart?.destroy()

  const expenses = categorySummary.value.filter(c => c.categoryType === 'Expense')
  if (expenses.length === 0) return

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
      plugins: {
        legend: { display: false },
        tooltip: {
          callbacks: {
            label: ctx => ` ${ctx.label}: ${(ctx.raw as number).toLocaleString()}원`,
          },
        },
      },
      cutout: '65%',
    },
  })
}

function renderLine() {
  if (!lineCanvas.value || trend.value.length === 0) return
  lineChart?.destroy()

  const labels = trend.value.map(t => `${t.month}월`)

  lineChart = new Chart(lineCanvas.value, {
    type: 'line',
    data: {
      labels,
      datasets: [
        {
          label: '수입',
          data: trend.value.map(t => t.income),
          borderColor: '#3B82F6',
          backgroundColor: 'rgba(59,130,246,0.10)',
          fill: true,
          tension: 0.4,
          pointRadius: 4,
          pointBackgroundColor: '#3B82F6',
          borderWidth: 2,
        },
        {
          label: '지출',
          data: trend.value.map(t => t.expense),
          borderColor: '#EF4444',
          backgroundColor: 'rgba(239,68,68,0.08)',
          fill: true,
          tension: 0.4,
          pointRadius: 4,
          pointBackgroundColor: '#EF4444',
          borderWidth: 2,
        },
      ],
    },
    options: {
      responsive: true,
      interaction: { mode: 'index', intersect: false },
      plugins: {
        legend: { position: 'top', labels: { font: { size: 12 }, boxWidth: 12 } },
        tooltip: {
          callbacks: {
            label: ctx => ` ${ctx.dataset.label}: ${(ctx.raw as number).toLocaleString()}원`,
          },
        },
      },
      scales: {
        y: {
          beginAtZero: true,
          ticks: {
            callback: val => `${(val as number / 10000).toFixed(0)}만`,
            font: { size: 11 },
          },
          grid: { color: 'rgba(0,0,0,0.05)' },
        },
        x: {
          ticks: { font: { size: 11 } },
          grid: { display: false },
        },
      },
    },
  })
}

watch([() => store.currentYear, () => store.currentMonth], loadData)
onMounted(loadData)
</script>

<template>
  <div class="h-full flex flex-col max-w-lg mx-auto">
    <!-- 고정 헤더 -->
    <div class="flex-shrink-0 bg-blue-600 text-white px-4 pt-10 pb-4">
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

    <!-- 스크롤 가능한 콘텐츠 -->
    <div class="flex-1 overflow-y-auto">
      <div v-if="loading" class="py-20 text-center text-gray-400 text-sm">불러오는 중...</div>

      <template v-else>
        <!-- 카테고리별 지출 도넛 차트 -->
        <div class="mx-4 mt-4 bg-white rounded-2xl p-4 shadow-sm">
          <h3 class="font-semibold text-gray-700 mb-3">카테고리별 지출</h3>

          <div v-if="categorySummary.filter(c => c.categoryType === 'Expense').length === 0"
            class="py-8 text-center text-gray-400 text-sm">지출 내역이 없습니다</div>

          <template v-else>
            <!-- 도넛 차트 -->
            <div class="flex justify-center">
              <div class="relative w-48 h-48">
                <canvas ref="pieCanvas" />
              </div>
            </div>

            <!-- 카테고리 리스트 -->
            <div class="mt-4 space-y-2">
              <div
                v-for="(c, i) in categorySummary.filter(x => x.categoryType === 'Expense')"
                :key="c.categoryId"
                class="flex items-center justify-between text-sm"
              >
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

        <!-- 최근 6개월 라인 차트 -->
        <div class="mx-4 mt-4 mb-4 bg-white rounded-2xl p-4 shadow-sm">
          <h3 class="font-semibold text-gray-700 mb-3">최근 6개월 추이</h3>
          <div v-if="trend.length === 0" class="py-8 text-center text-gray-400 text-sm">데이터가 없습니다</div>
          <canvas v-else ref="lineCanvas" class="max-h-52" />
        </div>
      </template>
    </div>
  </div>
</template>
