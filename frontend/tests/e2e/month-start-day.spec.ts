/**
 * 시나리오 3: MonthStartDay 기반 UI 필터링
 *
 * monthStartDay=25 설정 시, 날짜 경계가 올바르게 적용되는지 검증:
 * - 이번 달 24일 거래 → 이번 달(기간: 전월 25일 ~ 당월 24일)에 표시
 * - 이번 달 25일 거래 → 다음 달(기간: 당월 25일 ~ 다음달 24일)에 표시
 *
 * 테스트 후 monthStartDay=1로 복구 (다른 테스트 영향 방지)
 *
 * 전제: 백엔드(localhost:5244) + 프론트(localhost:5173) 모두 실행 중
 *       "식비" 카테고리, "현금" 결제수단이 DB에 존재
 */

import { test, expect } from '@playwright/test'
import { HomePage } from './pages/HomePage'
import { TransactionModalPage } from './pages/TransactionModalPage'
import { SettingsPage } from './pages/SettingsPage'

test.describe('MonthStartDay 기반 거래 필터링 (Galaxy S25)', () => {
  // 테스트에 사용할 경계 날짜 계산 (현재 달 기준)
  function getBoundaryDates() {
    const now = new Date()
    const year = now.getFullYear()
    const month = String(now.getMonth() + 1).padStart(2, '0')
    // monthStartDay=25 기준:
    //   "이번 달" 범위 = 전월 25일 ~ 당월 24일
    //   당월 24일 → 이번 달에 속함
    //   당월 25일 → 다음 달에 속함
    return {
      inCurrentPeriod: `${year}-${month}-24`,  // 이번 달 기간에 속하는 날짜
      inNextPeriod: `${year}-${month}-25`,      // 다음 달 기간에 속하는 날짜
    }
  }

  test('24일 거래는 이번 달에, 25일 거래는 다음 달 기간에 표시된다', async ({ page }) => {
    const home = new HomePage(page)
    const modal = new TransactionModalPage(page)
    const settings = new SettingsPage(page)
    const { inCurrentPeriod, inNextPeriod } = getBoundaryDates()
    const memoA = `경계전_${Date.now()}`  // 24일 거래
    const memoB = `경계후_${Date.now()}`  // 25일 거래

    // ── 1. monthStartDay=25 설정 ──────────────────────────────────────────
    await settings.goto()
    await settings.setMonthStartDay(25)

    // ── 2. 홈 진입 (이번 달 기간: 전월 25일 ~ 당월 24일) ─────────────────
    await home.goto()

    // 기간 범위 표시가 나타남을 확인 (monthStartDay≠1이면 표시)
    await expect(page.locator('text=/\\d+\\/\\d+ ~ \\d+\\/\\d+/')).toBeVisible()

    // ── 3. 당월 24일(이번 달 기간 내) 거래 등록 ──────────────────────────
    await home.openAddModal()
    await modal.selectExpense()
    await modal.fillAmount(10_000)
    await modal.setDate(inCurrentPeriod)
    await modal.selectCategory('식비')
    await modal.selectPaymentMethod('현금')
    await modal.fillMemo(memoA)
    await modal.save()
    await home.waitForLoad()

    // 이번 달 목록에 24일 거래가 보여야 함
    await expect(home.getTransactionItemsByMemo(memoA)).toHaveCount(1)

    // ── 4. 당월 25일(다음 달 기간) 거래 등록 ─────────────────────────────
    await home.openAddModal()
    await modal.selectExpense()
    await modal.fillAmount(20_000)
    await modal.setDate(inNextPeriod)
    await modal.selectCategory('식비')
    await modal.selectPaymentMethod('현금')
    await modal.fillMemo(memoB)
    await modal.save()
    await home.waitForLoad()

    // 이번 달 목록에 25일 거래는 보이지 않아야 함 (다음 달 기간 소속)
    await expect(home.getTransactionItemsByMemo(memoB)).toHaveCount(0)

    // ── 5. 다음 달로 이동 → 25일 거래가 표시되어야 함 ─────────────────────
    await home.nextMonth()
    await expect(home.getTransactionItemsByMemo(memoB)).toHaveCount(1)

    // 이번 달 기간의 24일 거래는 다음 달 뷰에 보이지 않아야 함
    await expect(home.getTransactionItemsByMemo(memoA)).toHaveCount(0)

    // ── 6. 복구: monthStartDay=1로 원복 ──────────────────────────────────
    await settings.goto()
    await settings.setMonthStartDay(1)
  })
})
