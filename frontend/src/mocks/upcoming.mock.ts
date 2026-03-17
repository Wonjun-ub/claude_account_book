// Sprint 6 Step 1 목업용 — Step 3에서 실제 API로 교체
// 목업 기준: 정산일=15, 결제일=25 (전월 16일 ~ 당월 15일 → 당월 25일 결제)
// 기준일: 2026-03-16 기준 하드코딩된 더미 데이터

export interface MockCardBilling {
  id: number          // PaymentMethod.id에 대응
  name: string        // 카드명
  dueDay: number      // 결제일
  dueDate: string     // 결제 예정일 (YYYY-MM-DD)
  periodFrom: string  // 청구 기간 시작 (전월 16일)
  periodTo: string    // 청구 기간 종료 (당월 15일)
  amount: number      // 청구 예정 금액
  txCount: number     // 거래 건수
}

// 오늘(2026-03-16) 기준: 청구 기간 02/16 ~ 03/15, 결제일 03/25
export const mockCardBillings: MockCardBilling[] = [
  {
    id: 1,
    name: '신한카드',
    dueDay: 25,
    dueDate: '2026-03-25',
    periodFrom: '2026-02-16',
    periodTo: '2026-03-15',
    amount: 287000,
    txCount: 6,
  },
  {
    id: 4,
    name: '국민카드',
    dueDay: 25,
    dueDate: '2026-03-25',
    periodFrom: '2026-02-16',
    periodTo: '2026-03-15',
    amount: 142000,
    txCount: 3,
  },
]
