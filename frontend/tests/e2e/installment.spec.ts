/**
 * 시나리오 1: 할부 거래 등록 및 금액 자동 분할
 *
 * 100,000원 / 3개월 할부 등록 시:
 * - 모달 미리보기: 1회차 33,334원 / 2회차 이후 33,333원
 * - 등록 후 거래 목록: 1회차(이번 달) → 2회차(다음 달) → 3회차(다다음 달)
 *
 * 전제: 백엔드(localhost:5244) + 프론트(localhost:5173) 모두 실행 중
 *       "식비" 카테고리, "현금" 결제수단이 DB에 존재
 */

import { test, expect } from '@playwright/test'
import { HomePage } from './pages/HomePage'
import { TransactionModalPage } from './pages/TransactionModalPage'

test.describe('할부 거래 등록 및 금액 자동 분할 (Galaxy S25)', () => {
  test('100,000원 3개월 할부: 미리보기 확인 후 회차별 금액이 목록에 렌더링된다', async ({ page }) => {
    const home = new HomePage(page)
    const modal = new TransactionModalPage(page)
    // 각 테스트 실행을 구분하기 위한 고유 메모
    const testMemo = `할부테스트_${Date.now()}`

    // ── 1. 홈 진입 ──────────────────────────────────────────────────────────
    await home.goto()

    // ── 2. 거래 추가 모달 열기 ─────────────────────────────────────────────
    await home.openAddModal()

    // ── 3. 지출 / 금액 / 할부 토글 ────────────────────────────────────────
    await modal.selectExpense()
    await modal.fillAmount(100_000)
    await modal.enableInstallment()
    await modal.setInstallmentMonths(3)

    // ── 4. 저장 전 미리보기 UI 검증 ────────────────────────────────────────
    const preview = await modal.getInstallmentPreview()
    expect(preview.first).toBe('33,334원')
    expect(preview.monthly).toBe('33,333원')

    // ── 5. 카테고리·결제수단·메모 입력 후 저장 ────────────────────────────
    await modal.selectCategory('식비')
    await modal.selectPaymentMethod('현금')
    await modal.fillMemo(testMemo)
    await modal.save()
    await home.waitForLoad()

    // ── 6. 이번 달: 1회차 (33,334원) ──────────────────────────────────────
    const firstItem = home.getTransactionItemsByMemo(testMemo).first()
    await expect(firstItem).toContainText('33,334원')
    await expect(firstItem).toContainText('할부 1회')

    // ── 7. 다음 달 이동: 2회차 (33,333원) ─────────────────────────────────
    await home.nextMonth()
    const secondItem = home.getTransactionItemsByMemo(testMemo).first()
    await expect(secondItem).toContainText('33,333원')
    await expect(secondItem).toContainText('할부 2회')

    // ── 8. 다다음 달 이동: 3회차 (33,333원) ───────────────────────────────
    await home.nextMonth()
    const thirdItem = home.getTransactionItemsByMemo(testMemo).first()
    await expect(thirdItem).toContainText('33,333원')
    await expect(thirdItem).toContainText('할부 3회')
  })
})
