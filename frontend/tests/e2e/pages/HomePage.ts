import { type Page, type Locator } from '@playwright/test'

export class HomePage {
  constructor(readonly page: Page) {}

  async goto() {
    await this.page.goto('/')
    await this.waitForLoad()
  }

  // ── 로드 완료 대기 ────────────────────────────────────────────────────────

  async waitForLoad() {
    // "불러오는 중..." 텍스트가 사라질 때까지 대기 (이미 없으면 즉시 통과)
    await this.page
      .locator('text=불러오는 중...')
      .waitFor({ state: 'detached', timeout: 15_000 })
      .catch(() => {})
  }

  // ── 월 네비게이션 ──────────────────────────────────────────────────────────

  async getMonthLabel(): Promise<string> {
    return this.page.locator('[data-testid="month-label"]').innerText()
  }

  async prevMonth() {
    await Promise.all([
      this.page.waitForResponse(r => r.url().includes('/api/summary/monthly')),
      this.page.locator('[data-testid="btn-prev-month"]').tap(),
    ])
    await this.waitForLoad()
  }

  async nextMonth() {
    await Promise.all([
      this.page.waitForResponse(r => r.url().includes('/api/summary/monthly')),
      this.page.locator('[data-testid="btn-next-month"]').tap(),
    ])
    await this.waitForLoad()
  }

  // ── 거래 목록 ──────────────────────────────────────────────────────────────

  /** 전체 거래 항목 */
  getTransactionItems(): Locator {
    return this.page.locator('[data-testid="transaction-item"]')
  }

  /** 특정 메모를 포함하는 거래 항목 (테스트 격리용) */
  getTransactionItemsByMemo(memo: string): Locator {
    return this.getTransactionItems().filter({ hasText: memo })
  }

  /** 할부 뱃지 텍스트 목록 ("할부 N회") */
  async getInstallmentBadges(): Promise<string[]> {
    return this.page.locator('.bg-orange-50.text-orange-500').allInnerTexts()
  }

  /** 특정 메모를 포함하는 거래에서 할부 뱃지 텍스트 반환 */
  async getInstallmentBadgeByMemo(memo: string): Promise<string> {
    return this.getTransactionItemsByMemo(memo)
      .locator('.bg-orange-50.text-orange-500')
      .innerText()
  }

  /** 반복 뱃지 ("반복") 개수 */
  async getRecurringCountByMemo(memo: string): Promise<number> {
    return this.getTransactionItemsByMemo(memo)
      .locator('.bg-blue-50.text-blue-500')
      .count()
  }

  // ── FAB → 거래 추가 모달 열기 ──────────────────────────────────────────────

  async openAddModal() {
    await this.page.locator('[data-testid="fab-add"]').tap()
    await this.page.waitForSelector('text=거래 추가', { timeout: 5_000 })
  }
}
