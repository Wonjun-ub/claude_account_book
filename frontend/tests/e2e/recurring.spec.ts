/**
 * 시나리오 2: 반복 거래 월 이동 및 중복 방지 (기존 버그 방어)
 *
 * 반복 지출 등록 → 다음 달 이동 → 이번 달 복귀 시
 * 이번 달 해당 반복 거래가 여전히 1건만 존재해야 한다.
 *
 * 방어 대상 버그: ApplyRecurringTransactionsAsync가 loadData() 시마다
 * 멱등성 없이 중복 거래를 생성하는 경우
 *
 * 전제: 백엔드(localhost:5244) + 프론트(localhost:5173) 모두 실행 중
 *       "식비" 카테고리, "현금" 결제수단이 DB에 존재
 */

import { test, expect } from '@playwright/test'
import { HomePage } from './pages/HomePage'
import { TransactionModalPage } from './pages/TransactionModalPage'

test.describe('반복 거래 월 이동 및 중복 방지 (Galaxy S25)', () => {
  test('반복 거래 등록 후 다음 달 이동 → 복귀 시 이번 달 거래가 1건만 존재한다', async ({ page }) => {
    const home = new HomePage(page)
    const modal = new TransactionModalPage(page)
    const testMemo = `반복테스트_${Date.now()}`

    // ── 1. 홈 진입 및 초기 월 레이블 확인 ────────────────────────────────
    await home.goto()
    const initialLabel = await home.getMonthLabel()

    // ── 2. 반복 지출 등록 ─────────────────────────────────────────────────
    await home.openAddModal()
    await modal.selectExpense()
    await modal.fillAmount(50_000)
    await modal.fillMemo(testMemo)
    await modal.enableRecurring()
    await modal.selectCategory('식비')
    await modal.selectPaymentMethod('현금')
    await modal.save()
    await home.waitForLoad()

    // ── 3. 이번 달 반복 거래 1건 확인 ────────────────────────────────────
    const countBefore = await home.getRecurringCountByMemo(testMemo)
    expect(countBefore).toBe(1)

    // ── 4. 다음 달 이동 → 이번 달 복귀 ──────────────────────────────────
    await home.nextMonth()
    const nextLabel = await home.getMonthLabel()
    expect(nextLabel).not.toBe(initialLabel)

    await home.prevMonth()
    const returnLabel = await home.getMonthLabel()
    expect(returnLabel).toBe(initialLabel)

    // ── 5. 복귀 후 동일 메모 거래가 여전히 1건 (중복 생성 없음) ──────────
    const countAfter = await home.getRecurringCountByMemo(testMemo)
    expect(countAfter).toBe(1)
  })
})
