<script setup lang="ts">
import { ref, computed, watch, onMounted, onUnmounted } from 'vue'
import LoadingSpinner from '@/components/LoadingSpinner.vue'
import {
  Chart, ArcElement, Tooltip, Legend,
  CategoryScale, LinearScale,
  BarElement, BarController,
  LineElement, PointElement, Filler, Title,
  DoughnutController,
} from 'chart.js'
import { useSettingsStore } from '@/stores/app'
import { useTransactionStore } from '@/stores/transaction'
import type { CategorySummary, MonthlyTrend, TransactionView } from '@/types'

Chart.register(
  ArcElement, DoughnutController, Tooltip, Legend,
  CategoryScale, LinearScale,
  BarElement, BarController,
  LineElement, PointElement, Filler, Title,
)

const settingsStore = useSettingsStore()
const txStore       = useTransactionStore()

const loading     = ref(false)
const statsType   = ref<'Expense' | 'Income' | 'Savings'>('Expense')
const monthTxs    = ref<TransactionView[]>([])
const trend       = ref<MonthlyTrend[]>([])

const donutCanvas = ref<HTMLCanvasElement | null>(null)
const trendCanvas = ref<HTMLCanvasElement | null>(null)
let donutChart: Chart | null = null
let trendChart: Chart | null = null

const CHART_COLORS = [
  '#3B82F6','#EF4444','#10B981','#F59E0B','#8B5CF6',
  '#EC4899','#14B8A6','#F97316','#6366F1','#84CC16',
]

// ── 로드 ─────────────────────────────────────────────────────────────────────

async function loadData() {
  loading.value = true
  try {
    const [txs, tr] = await Promise.all([
      txStore.getTransactionsForMonth(
        settingsStore.currentYear,
        settingsStore.currentMonth,
        settingsStore.monthStartDay,
      ),
      txStore.getMonthlyTrend(
        settingsStore.currentYear,
        settingsStore.currentMonth,
        6,
        settingsStore.monthStartDay,
      ),
    ])
    monthTxs.value = txs
    trend.value    = tr
  } finally {
    loading.value = false
  }
}

watch([() => settingsStore.currentYear, () => settingsStore.currentMonth], loadData)
onMounted(loadData)

// ── 카테고리별 집계 ───────────────────────────────────────────────────────────

const categorySummary = computed<CategorySummary[]>(() => {
  const filtered = monthTxs.value.filter(t => t.type === statsType.value && t.isIncludedInTotal)
  const total    = filtered.reduce((s, t) => s + t.amount, 0)
  if (total === 0) return []

  const map = new Map<number, { name: string; amount: number }>()
  for (const t of filtered) {
    const prev = map.get(t.categoryId)
    map.set(t.categoryId, {
      name:   t.categoryName,
      amount: (prev?.amount ?? 0) + t.amount,
    })
  }

  return Array.from(map.entries())
    .map(([id, { name, amount }]) => ({
      categoryId:   id,
      categoryName: name,
      categoryType: statsType.value,
      amount,
      percentage: (amount / total) * 100,
    }))
    .sort((a, b) => b.amount - a.amount)
})

// ── 차트 렌더링 ───────────────────────────────────────────────────────────────

watch([categorySummary, loading], ([_, isLoading]) => {
  if (!isLoading) renderDonut()
}, { flush: 'post' })

watch([trend, loading], ([_, isLoading]) => {
  if (!isLoading) renderTrend()
}, { flush: 'post' })

function renderDonut() {
  if (!donutCanvas.value) return
  donutChart?.destroy()

  if (categorySummary.value.length === 0) return

  donutChart = new Chart(donutCanvas.value, {
    type: 'doughnut',
    data: {
      labels: categorySummary.value.map(c => c.categoryName),
      datasets: [{
        data: categorySummary.value.map(c => c.amount),
        backgroundColor: CHART_COLORS.slice(0, categorySummary.value.length),
        borderWidth: 0,
      }],
    },
    options: {
      responsive: true,
      plugins: {
        legend: { display: false },
        tooltip: { callbacks: { label: ctx => ` ${ctx.label}: ${(ctx.raw as number).toLocaleString()}원` } },
      },
      cutout: '65%',
    },
  })
}

function renderTrend() {
  if (!trendCanvas.value || trend.value.length === 0) return
  trendChart?.destroy()

  const labels = trend.value.map(t => `${t.month}월`)

  trendChart = new Chart(trendCanvas.value, {
    type: 'bar',
    data: {
      labels,
      datasets: [
        { label: '수입', data: trend.value.map(t => t.income),  backgroundColor: 'rgba(59,130,246,0.7)',  borderRadius: 4 },
        { label: '지출', data: trend.value.map(t => t.expense), backgroundColor: 'rgba(239,68,68,0.7)',  borderRadius: 4 },
        { label: '저축', data: trend.value.map(t => t.savings), backgroundColor: 'rgba(16,185,129,0.7)', borderRadius: 4 },
      ],
    },
    options: {
      responsive: true,
      interaction: { mode: 'index', intersect: false },
      plugins: {
        legend: { position: 'top', labels: { font: { size: 11 }, boxWidth: 10 } },
        tooltip: { callbacks: { label: ctx => ` ${ctx.dataset.label}: ${(ctx.raw as number).toLocaleString()}원` } },
      },
      scales: {
        y: {
          beginAtZero: true,
          ticks: { callback: v => `${((v as number) / 10000).toFixed(0)}만`, font: { size: 10 } },
          grid: { color: 'rgba(255,255,255,0.05)' },
        },
        x: { ticks: { font: { size: 11 } }, grid: { display: false } },
      },
    },
  })
}

onUnmounted(() => {
  donutChart?.destroy()
  trendChart?.destroy()
})
</script>

<template>
  <div class="h-full flex flex-col max-w-lg mx-auto bg-gray-900">
    <!-- 고정 헤더 -->
    <div class="flex-shrink-0 bg-gray-900 px-4 pt-12 pb-4 border-b border-gray-700/50">
      <div class="flex items-center justify-between">
        <button @click="settingsStore.prevMonth()" class="p-1 rounded-full hover:bg-gray-700">
          <svg class="w-6 h-6 text-gray-300" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 19l-7-7 7-7" />
          </svg>
        </button>
        <span class="text-base font-semibold text-gray-100">{{ settingsStore.currentMonthLabel }}</span>
        <button @click="settingsStore.nextMonth()" class="p-1 rounded-full hover:bg-gray-700">
          <svg class="w-6 h-6 text-gray-300" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 5l7 7-7 7" />
          </svg>
        </button>
      </div>
    </div>

    <!-- 스크롤 가능한 콘텐츠 -->
    <div class="flex-1 overflow-y-auto">
      <LoadingSpinner v-if="loading" />
      <template v-else>

        <!-- 유형 탭 -->
        <div class="px-4 pt-4">
          <div class="flex rounded-xl bg-gray-800 p-1">
            <button @click="statsType = 'Expense'" :class="statsType === 'Expense' ? 'bg-red-500 text-white shadow-sm' : 'text-gray-400'" class="flex-1 py-1.5 text-xs font-medium rounded-lg transition-all">지출</button>
            <button @click="statsType = 'Income'"  :class="statsType === 'Income'  ? 'bg-blue-500 text-white shadow-sm' : 'text-gray-400'" class="flex-1 py-1.5 text-xs font-medium rounded-lg transition-all">수입</button>
            <button @click="statsType = 'Savings'" :class="statsType === 'Savings' ? 'bg-emerald-500 text-white shadow-sm' : 'text-gray-400'" class="flex-1 py-1.5 text-xs font-medium rounded-lg transition-all">저축</button>
          </div>
        </div>

        <!-- 카테고리별 도넛 차트 -->
        <div class="mx-4 mt-4 bg-gray-800 rounded-2xl p-4">
          <h3 class="text-sm font-semibold text-gray-100 mb-3">카테고리별 분석</h3>

          <div v-if="categorySummary.length === 0" class="py-8 text-center text-gray-400 text-sm">
            해당 유형의 거래가 없습니다
          </div>
          <div v-else class="flex gap-4 items-center">
            <div class="flex-shrink-0" style="width:120px;height:120px">
              <canvas ref="donutCanvas"></canvas>
            </div>
            <div class="flex-1 space-y-2 min-w-0">
              <div v-for="(item, i) in categorySummary" :key="item.categoryId" class="flex items-center gap-2">
                <div class="w-2.5 h-2.5 rounded-full flex-shrink-0" :style="`background-color:${CHART_COLORS[i % CHART_COLORS.length]}`"></div>
                <span class="text-xs text-gray-300 truncate flex-1">{{ item.categoryName }}</span>
                <span class="text-xs text-gray-100 font-medium flex-shrink-0">{{ item.amount.toLocaleString() }}</span>
                <span class="text-[10px] text-gray-400 flex-shrink-0">{{ Math.round(item.percentage) }}%</span>
              </div>
            </div>
          </div>
        </div>

        <!-- 최근 6개월 추이 -->
        <div class="mx-4 mt-4 mb-8 bg-gray-800 rounded-2xl p-4">
          <h3 class="text-sm font-semibold text-gray-100 mb-4">최근 6개월 추이</h3>
          <div v-if="trend.length === 0" class="py-8 text-center text-gray-400 text-sm">데이터가 없습니다</div>
          <canvas v-else ref="trendCanvas" style="max-height:200px"></canvas>
        </div>

      </template>
    </div>
  </div>
</template>
