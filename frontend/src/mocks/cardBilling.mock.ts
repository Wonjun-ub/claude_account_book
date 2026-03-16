// Sprint 6 Step 1 목업용 — Step 3에서 실제 API(/api/card-billing/summary)로 교체
import dayjs from 'dayjs'

export interface MockCardSlot {
  slotIndex: 1 | 2
  label: string               // '3월 청구금액'
  periodFrom: string          // 'YYYY-MM-DD'
  periodTo: string
  paymentDueDate: string
  totalAmount: number
  transactionCount: number
  isConfirmed: boolean        // 정산일 지남 여부
}

export interface MockCard {
  id: number
  name: string
  billingCutoffDay: number    // 정산일 (예: 15)
  paymentDueDay: number       // 결제일 (예: 25)
  slots: [MockCardSlot, MockCardSlot]
}

export interface MockCardTransaction {
  date: string
  memo: string
  amount: number
  categoryName: string
}

// D-day 계산 헬퍼 — 오늘 기준 동적 계산
export function calcDDay(paymentDueDate: string): string {
  const diff = dayjs(paymentDueDate).diff(dayjs().startOf('day'), 'day')
  if (diff > 0) return `D-${diff}`
  if (diff === 0) return 'D-day'
  return `D+${Math.abs(diff)}`
}

// 케이스 A: 정산일=15, 결제일=25 (같은 달 결제) — 신한카드
// 오늘 3/16 기준: 슬롯1=3월 청구(2/15~3/14 확정, 결제 3/25), 슬롯2=4월 청구 누적중
export const mockCards: MockCard[] = [
  {
    id: 1,
    name: '신한카드',
    billingCutoffDay: 15,
    paymentDueDay: 25,
    slots: [
      {
        slotIndex: 1,
        label: '3월 청구금액',
        periodFrom: '2026-02-15',
        periodTo: '2026-03-14',
        paymentDueDate: '2026-03-25',
        totalAmount: 287_000,
        transactionCount: 12,
        isConfirmed: true,
      },
      {
        slotIndex: 2,
        label: '4월 청구금액',
        periodFrom: '2026-03-15',
        periodTo: '2026-04-14',
        paymentDueDate: '2026-04-25',
        totalAmount: 43_000,
        transactionCount: 3,
        isConfirmed: false,
      },
    ],
  },
  // 케이스 B: 정산일=25, 결제일=10 (다음 달 결제) — 국민카드
  // 오늘 3/16 기준: 슬롯1=4월 청구(2/26~3/25 누적중, 결제 4/10), 슬롯2=5월 청구
  {
    id: 2,
    name: '국민카드',
    billingCutoffDay: 25,
    paymentDueDay: 10,
    slots: [
      {
        slotIndex: 1,
        label: '4월 청구금액',
        periodFrom: '2026-02-26',
        periodTo: '2026-03-25',
        paymentDueDate: '2026-04-10',
        totalAmount: 156_500,
        transactionCount: 7,
        isConfirmed: false,
      },
      {
        slotIndex: 2,
        label: '5월 청구금액',
        periodFrom: '2026-03-26',
        periodTo: '2026-04-25',
        paymentDueDate: '2026-05-10',
        totalAmount: 0,
        transactionCount: 0,
        isConfirmed: false,
      },
    ],
  },
]

// 드릴다운 mock 거래 목록 — cardId + slotIndex 조합으로 분기
export const mockSlotTransactions: Record<string, MockCardTransaction[]> = {
  '1-1': [
    { date: '2026-03-12', memo: '스타벅스 역삼점', amount: 6_500, categoryName: '식비' },
    { date: '2026-03-10', memo: '올리브영 강남', amount: 32_000, categoryName: '쇼핑' },
    { date: '2026-03-08', memo: 'GS25 편의점', amount: 4_200, categoryName: '식비' },
    { date: '2026-03-05', memo: '버스 충전', amount: 10_000, categoryName: '교통' },
    { date: '2026-03-01', memo: '요기요 배달', amount: 18_500, categoryName: '식비' },
    { date: '2026-02-28', memo: 'CGV 영화', amount: 14_000, categoryName: '문화/여가' },
    { date: '2026-02-25', memo: '마트 장보기', amount: 52_800, categoryName: '식비' },
    { date: '2026-02-20', memo: '교보문고', amount: 24_000, categoryName: '교육' },
    { date: '2026-02-18', memo: '카카오택시', amount: 8_900, categoryName: '교통' },
    { date: '2026-02-16', memo: '약국', amount: 12_100, categoryName: '의료' },
    { date: '2026-02-15', memo: '점심 회식', amount: 35_000, categoryName: '식비' },
    { date: '2026-02-15', memo: '주유비', amount: 66_000, categoryName: '교통' },
  ],
  '1-2': [
    { date: '2026-03-16', memo: '배달의민족', amount: 22_500, categoryName: '식비' },
    { date: '2026-03-15', memo: '쿠팡 로켓배송', amount: 13_500, categoryName: '쇼핑' },
    { date: '2026-03-15', memo: '지하철 정기권', amount: 7_000, categoryName: '교통' },
  ],
  '2-1': [
    { date: '2026-03-14', memo: '이마트 트레이더스', amount: 87_000, categoryName: '식비' },
    { date: '2026-03-11', memo: '넷플릭스', amount: 17_000, categoryName: '문화/여가' },
    { date: '2026-03-08', memo: '병원비', amount: 15_000, categoryName: '의료' },
    { date: '2026-03-03', memo: '헬스장 월정액', amount: 35_000, categoryName: '문화/여가' },
    { date: '2026-02-28', memo: '주유소', amount: 60_000, categoryName: '교통' },
    { date: '2026-02-27', memo: '카페 투썸', amount: 8_500, categoryName: '식비' },
    { date: '2026-02-26', memo: '편의점', amount: 3_200, categoryName: '식비' },
  ],
  '2-2': [],
}
