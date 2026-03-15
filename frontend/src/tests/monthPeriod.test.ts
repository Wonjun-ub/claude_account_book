import { describe, it, expect } from 'vitest'
import { getMonthPeriod } from '@/utils/monthPeriod'

describe('getMonthPeriod', () => {

  // ── TC-MP-01~03: startDay=1 기본 케이스 ───────────────────────────────────
  describe('startDay=1 (표준 월 기준)', () => {
    it('TC-MP-01: 3월 → 3/1 ~ 3/31', () => {
      const { start, end } = getMonthPeriod(2026, 3, 1)
      expect(start).toBe('2026-03-01')
      expect(end).toBe('2026-03-31')
    })

    it('TC-MP-02: 2월(평년) → 2/1 ~ 2/28', () => {
      const { start, end } = getMonthPeriod(2026, 2, 1)
      expect(start).toBe('2026-02-01')
      expect(end).toBe('2026-02-28')
    })

    it('TC-MP-03: 2월(윤년 2024) → 2/1 ~ 2/29', () => {
      const { start, end } = getMonthPeriod(2024, 2, 1)
      expect(start).toBe('2024-02-01')
      expect(end).toBe('2024-02-29')
    })
  })

  // ── TC-MP-04~09: startDay=25 케이스 ─────────────────────────────────────
  describe('startDay=25 (25일 시작)', () => {
    it('TC-MP-04: 3월 → 2/25 ~ 3/24', () => {
      const { start, end } = getMonthPeriod(2026, 3, 25)
      expect(start).toBe('2026-02-25')
      expect(end).toBe('2026-03-24')
    })

    it('TC-MP-05: 2월 → 1/25 ~ 2/24', () => {
      const { start, end } = getMonthPeriod(2026, 2, 25)
      expect(start).toBe('2026-01-25')
      expect(end).toBe('2026-02-24')
    })

    it('TC-MP-06: 1월 → 전년도 12/25 ~ 1/24 (연도 경계)', () => {
      const { start, end } = getMonthPeriod(2026, 1, 25)
      expect(start).toBe('2025-12-25')
      expect(end).toBe('2026-01-24')
    })

    it('TC-MP-07: 12월 → 11/25 ~ 12/24', () => {
      const { start, end } = getMonthPeriod(2026, 12, 25)
      expect(start).toBe('2026-11-25')
      expect(end).toBe('2026-12-24')
    })

    it('TC-MP-08: 2/25 거래는 2월 범위에 포함되지 않음', () => {
      const { start, end } = getMonthPeriod(2026, 2, 25)
      expect('2026-02-25' >= start && '2026-02-25' <= end).toBe(false)
    })

    it('TC-MP-09: 2/25 거래는 3월 범위에 포함됨', () => {
      const { start, end } = getMonthPeriod(2026, 3, 25)
      expect('2026-02-25' >= start && '2026-02-25' <= end).toBe(true)
    })
  })

  // ── TC-MP-10~11: startDay=10 케이스 ──────────────────────────────────────
  describe('startDay=10 (10일 시작)', () => {
    it('TC-MP-10: 3월 → 2/10 ~ 3/9', () => {
      const { start, end } = getMonthPeriod(2026, 3, 10)
      expect(start).toBe('2026-02-10')
      expect(end).toBe('2026-03-09')
    })

    it('TC-MP-11: 1월 → 전년도 12/10 ~ 1/9', () => {
      const { start, end } = getMonthPeriod(2026, 1, 10)
      expect(start).toBe('2025-12-10')
      expect(end).toBe('2026-01-09')
    })
  })
})
