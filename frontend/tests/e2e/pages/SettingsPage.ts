import { type Page } from '@playwright/test'

export class SettingsPage {
  constructor(readonly page: Page) {}

  async goto() {
    await this.page.goto('/settings')
    await this.page.waitForLoadState('domcontentloaded')
    // 설정 헤더가 보일 때까지 대기
    await this.page.waitForSelector('text=설정', { timeout: 5_000 })
  }

  /**
   * 월 시작일 변경 및 저장
   * API 응답 대기 후 반환하여 저장 완료를 보장
   */
  async setMonthStartDay(day: number) {
    const input = this.page.locator('[data-testid="month-start-day-input"]')
    await input.fill(String(day))
    await Promise.all([
      this.page.waitForResponse(r => r.url().includes('/api/settings')),
      this.page.locator('[data-testid="save-month-start-day"]').tap(),
    ])
  }
}
