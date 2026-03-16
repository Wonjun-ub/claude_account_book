import { type Page } from '@playwright/test'

export class TransactionModalPage {
  constructor(readonly page: Page) {}

  // ── 수입/지출 탭 ──────────────────────────────────────────────────────────

  async selectExpense() {
    await this.page.getByRole('button', { name: '지출' }).tap()
  }

  async selectIncome() {
    await this.page.getByRole('button', { name: '수입' }).tap()
  }

  // ── 금액 입력 ──────────────────────────────────────────────────────────────

  async fillAmount(amount: number) {
    const input = this.page.locator('[data-testid="amount-input"]')
    await input.tap()
    await input.fill(String(amount))
  }

  // ── 날짜 입력 (YYYY-MM-DD) ───────────────────────────────────────────────

  async setDate(date: string) {
    await this.page.locator('input[type="date"]').first().fill(date)
  }

  // ── 메모 입력 ──────────────────────────────────────────────────────────────

  async fillMemo(memo: string) {
    await this.page.locator('[data-testid="memo-input"]').fill(memo)
  }

  // ── 카테고리 선택 ─────────────────────────────────────────────────────────

  async selectCategory(name: string) {
    await this.page
      .locator('[data-testid="category-select"]')
      .selectOption({ label: name })
  }

  // ── 결제수단 선택 (지출만 표시) ───────────────────────────────────────────

  async selectPaymentMethod(name: string) {
    await this.page
      .locator('[data-testid="payment-method-select"]')
      .selectOption({ label: name })
  }

  // ── 할부 설정 ─────────────────────────────────────────────────────────────

  async enableInstallment() {
    await this.page.locator('[data-testid="installment-toggle"]').tap()
  }

  async setInstallmentMonths(months: number) {
    const input = this.page.locator('[data-testid="installment-months"]')
    await input.fill(String(months))
  }

  /**
   * 할부 미리보기 금액 반환 (저장 전 UI 검증용)
   * @returns { first: "33,334원", monthly: "33,333원" }
   */
  async getInstallmentPreview(): Promise<{ first: string; monthly: string }> {
    const preview = this.page.locator('.bg-orange-50.rounded-xl')
    await preview.waitFor({ timeout: 5_000 })
    const rows = preview.locator('.flex.justify-between')
    return {
      first: await rows.nth(0).locator('.font-semibold').innerText(),
      monthly: await rows.nth(1).locator('.font-semibold').innerText(),
    }
  }

  // ── 반복 설정 ─────────────────────────────────────────────────────────────

  async enableRecurring() {
    await this.page.locator('[data-testid="recurring-toggle"]').tap()
    // 반복 설정 영역이 펼쳐질 때까지 대기
    await this.page.locator('[data-testid="recurring-day"]').waitFor({ timeout: 3_000 })
  }

  async setRecurringDay(day: number) {
    await this.page.locator('[data-testid="recurring-day"]').fill(String(day))
  }

  // ── 저장 ──────────────────────────────────────────────────────────────────

  /**
   * 저장 버튼을 누르고 모달이 닫힐 때까지 대기
   * (모달 닫힘 = save-btn이 DOM에서 제거됨)
   */
  async save() {
    await this.page.locator('[data-testid="save-btn"]').tap()
    await this.page
      .locator('[data-testid="save-btn"]')
      .waitFor({ state: 'detached', timeout: 15_000 })
  }
}
