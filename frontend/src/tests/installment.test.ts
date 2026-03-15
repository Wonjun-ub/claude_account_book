import { describe, it, expect } from 'vitest'
import { calcMockInstallment } from '@/mocks/installment.mock'

describe('calcMockInstallment', () => {

  // ── TC-INST-01: 나머지 없이 균등 분할 ────────────────────────────────────
  it('TC-INST-01: 300,000원 / 3개월 → 월 100,000 / 1회차 100,000 (나머지 0)', () => {
    const result = calcMockInstallment(300000, 3)
    expect(result.monthlyAmount).toBe(100000)
    expect(result.firstMonthAmount).toBe(100000)
  })

  // ── TC-INST-02: 나머지 1원 → 1회차에 합산 ────────────────────────────────
  it('TC-INST-02: 100,000원 / 3개월 → 월 33,333 / 1회차 33,334 (나머지 1)', () => {
    const result = calcMockInstallment(100000, 3)
    expect(result.monthlyAmount).toBe(33333)
    expect(result.firstMonthAmount).toBe(33334)
  })

  // ── TC-INST-03: 12개월 분할 ───────────────────────────────────────────────
  it('TC-INST-03: 100,000원 / 12개월 → 월 8,333 / 1회차 8,337 (나머지 4)', () => {
    const result = calcMockInstallment(100000, 12)
    expect(result.monthlyAmount).toBe(8333)
    expect(result.firstMonthAmount).toBe(8337)
    // 검증: 1회차 + 나머지 회차 합계 = 총금액
    expect(result.firstMonthAmount + result.monthlyAmount * 11).toBe(100000)
  })

  // ── TC-INST-04: 총합 일치 검증 (300,000 / 7개월) ─────────────────────────
  it('TC-INST-04: 300,000원 / 7개월 → 1회차 + 나머지 * 6 = 총금액', () => {
    const result = calcMockInstallment(300000, 7)
    expect(result.firstMonthAmount + result.monthlyAmount * 6).toBe(300000)
  })

  // ── TC-INST-05: 2개월 최소 분할 ──────────────────────────────────────────
  it('TC-INST-05: 10,001원 / 2개월 → 월 5,000 / 1회차 5,001 (나머지 1)', () => {
    const result = calcMockInstallment(10001, 2)
    expect(result.monthlyAmount).toBe(5000)
    expect(result.firstMonthAmount).toBe(5001)
    expect(result.firstMonthAmount + result.monthlyAmount).toBe(10001)
  })

  // ── TC-INST-06: 금액이 개월수보다 작은 경우 (1원 / 3개월) ─────────────────
  it('TC-INST-06: 1원 / 3개월 → 월 0 / 1회차 1', () => {
    const result = calcMockInstallment(1, 3)
    expect(result.monthlyAmount).toBe(0)
    expect(result.firstMonthAmount).toBe(1)
  })
})
