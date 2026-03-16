# Sprint 7 — 통계 화면 목업

## 개요

| 항목 | 내용 |
|------|------|
| 스프린트 번호 | Sprint 7 |
| 유형 | 목업 |
| 브랜치 | `sprint7` |
| 기간 | 2026-03-16 시작 |
| 상태 | 🔄 Step 1 완료 / Step 2 승인 대기 |
| 대상 브랜치 (PR) | `develop` |
| DB/백엔드 변경 | **없음** — 목업 전용 |
| 선행 조건 | Sprint 6 Step 2 승인 ✅ |

---

## 스프린트 목표

MockupView에 **통계 탭** UI/UX를 목업으로 구현한다.
다크모드 팔레트, 수입/지출/저축 색상 체계를 홈 탭과 통일하며,
카테고리별 도넛 차트 + 전월 대비 요약 카드 + 최근 6개월 막대 추이를 제공한다.

---

## Step 1 — 목업 ✅ 완료 (2026-03-16)

### 구현 파일

| 파일 | 설명 |
|------|------|
| `frontend/src/views/MockupView.vue` | 통계 탭 섹션 추가 + 하단 탭바 2탭(통계/설정) 추가 |

### 구현 상세

**하단 탭바 업데이트**
- 기존 1탭(가계부) → 3탭(가계부/통계/설정)
- 활성 탭 하이라이트: 가계부=`text-white`, 통계=`text-blue-400`, 설정=`text-blue-400`
- 설정 탭은 Sprint 8 목업 전까지 빈 화면 placeholder

**통계 탭 구성 (`activePage === 'stats'`)**

1. **헤더 (sticky)**
   - 홈 탭과 동일한 월 네비게이션 (prevMonth/nextMonth 공유)
   - 커스텀 시작일 적용 시 기간 문자열 표시

2. **이번 달 요약 카드 (2×2 grid)**
   - 수입(blue) / 지출(red) / 저축(emerald) / 잔액(white) 4열
   - 전월 대비 증감률 표시 (▲/▼ + %)
   - 지출 감소 시 blue(긍정), 증가 시 red(부정) 색상 구분
   - `prevMonthSummary` computed → `txTransactions`에서 직접 계산

3. **카테고리별 도넛 차트**
   - 지출/수입/저축 탭 전환 (`statsType` ref)
   - Chart.js doughnut, cutout 65%, `borderWidth: 0`
   - 차트 중앙: 합계 금액(만원 단위)
   - 우측 범례: 색상 점 + 카테고리명 + 금액
   - 데이터 없을 때 "해당 유형의 거래가 없습니다" 안내

4. **최근 6개월 막대 추이**
   - Chart.js bar, grouped (수입/지출/저축)
   - 현재 mockYear/mockMonth 기준 최근 6개월 자동 계산
   - 다크모드 그리드: `rgba(255,255,255,0.05)`, 눈금 색상 `#9CA3AF`

**Chart.js 등록**
```typescript
import {
  Chart, ArcElement, DoughnutController, Tooltip, Legend,
  CategoryScale, LinearScale, BarElement, BarController,
} from 'chart.js'
Chart.register(ArcElement, DoughnutController, Tooltip, Legend, CategoryScale, LinearScale, BarElement, BarController)
```

**차트 생명주기 관리**
- `watch(activePage)` → stats 탭 전환 시 `nextTick` 후 차트 렌더
- `watch([mockYear, mockMonth, statsType])` → 데이터 변경 시 재렌더
- `onUnmounted` → `donutChart?.destroy()`, `trendChart?.destroy()`

**색상 체계**
- 수입: `rgba(96,165,250,0.85)` (blue-400)
- 지출: `rgba(248,113,113,0.85)` (red-400)
- 저축: `rgba(52,211,153,0.85)` (emerald-400)
- 도넛 팔레트: `CHART_COLORS` 10색 배열

**데이터 소스**
- `txMonthFiltered` (홈 탭과 공유) → 카테고리 집계
- `txTransactions` → 6개월 추이 계산 (getMonthPeriod 적용)

---

## Step 2 — UI/UX 확정 ⬜ 승인 대기

> Step 1 목업을 로컬에서 `/mock-up` → 통계 탭으로 접속하여 확인 후 승인해주세요.

### 확인 항목

| # | 항목 | 확인 포인트 |
|---|------|------------|
| 1 | 전월 비교 카드 | 수입/지출/저축 전월 대비 증감률이 올바르게 표시되는가? |
| 2 | 도넛 차트 | 지출/수입/저축 탭 전환 시 차트가 올바르게 업데이트되는가? |
| 3 | 도넛 중앙 금액 | 합계 금액(만원)이 범례 합계와 일치하는가? |
| 4 | 6개월 막대 | 수입/지출/저축 3색 구분이 명확한가? 가독성 적절한가? |
| 5 | 월 네비게이션 | 다른 월로 이동 시 차트가 해당 월 데이터로 갱신되는가? |

### 승인 방법

> "진행해" 또는 "Step 2 완료, Sprint 8 진행해"라고 말씀해주세요.

---

## Step 3 — 실제 구현 ⬜ 보류 (Sprint 9에서 진행)

> **전략**: 설정 화면 목업(Sprint 8) 완료 후 Sprint 9에서 전체 실서비스 구현.

---

## 완료 기준

- ✅ T38: 통계 탭 다크모드 목업 구현 완료 (카테고리 도넛 차트, 전월 비교)
- ✅ T39: 최근 6개월 막대 추이 구현 완료
- ✅ `npm run build` 성공 (TypeScript 오류 0건)
- ⬜ Step 2: 사용자 UI/UX 승인
- ⬜ `sprint7 → develop` PR 생성

---

## 스프린트 회고 (완료 후 작성)

> 스프린트 완료 후 sprint-close 에이전트가 작성합니다.
