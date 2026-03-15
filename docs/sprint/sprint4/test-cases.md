# Sprint 4 테스트 케이스 문서

**작성일**: 2026-03-15
**테스트 대상**: MockupView.vue 핵심 로직
**테스트 파일**: `frontend/src/tests/`
**테스트 환경**: Vitest 0.34.6 / Node 16.20.0
**실행 결과**: **54 passed / 0 failed** ✅

---

## 실행 방법

```bash
cd frontend
npm test          # 1회 실행
npm run test:watch  # watch 모드
```

---

## 모듈별 테스트 현황

| 파일 | 테스트 케이스 수 | 결과 |
|------|---------------|------|
| `monthPeriod.test.ts` | 11 | ✅ 전체 통과 |
| `installment.test.ts` | 6 | ✅ 전체 통과 |
| `mockupLogic.test.ts` | 37 | ✅ 전체 통과 |
| **합계** | **54** | ✅ |

---

## 1. 월 기간 계산 (`getMonthPeriod`)

> 파일: `src/utils/monthPeriod.ts` → `src/tests/monthPeriod.test.ts`

### 1-1. startDay=1 (표준 월 기준)

| TC | 입력 | 기댓값 | 결과 |
|----|------|--------|------|
| TC-MP-01 | year=2026, month=3, startDay=1 | start=2026-03-01, end=2026-03-31 | ✅ |
| TC-MP-02 | year=2026, month=2, startDay=1 (평년) | start=2026-02-01, end=2026-02-28 | ✅ |
| TC-MP-03 | year=2024, month=2, startDay=1 (윤년) | start=2024-02-01, end=2024-02-29 | ✅ |

### 1-2. startDay=25 (25일 시작)

| TC | 입력 | 기댓값 | 결과 |
|----|------|--------|------|
| TC-MP-04 | year=2026, month=3, startDay=25 | start=2026-02-25, end=2026-03-24 | ✅ |
| TC-MP-05 | year=2026, month=2, startDay=25 | start=2026-01-25, end=2026-02-24 | ✅ |
| TC-MP-06 | year=2026, month=1, startDay=25 (연도 경계) | start=2025-12-25, end=2026-01-24 | ✅ |
| TC-MP-07 | year=2026, month=12, startDay=25 | start=2026-11-25, end=2026-12-24 | ✅ |
| TC-MP-08 | 2/25 거래 → 2월(startDay=25) 범위 포함 여부 | 포함되지 않음 (false) | ✅ |
| TC-MP-09 | 2/25 거래 → 3월(startDay=25) 범위 포함 여부 | 포함됨 (true) | ✅ |

### 1-3. startDay=10 (10일 시작)

| TC | 입력 | 기댓값 | 결과 |
|----|------|--------|------|
| TC-MP-10 | year=2026, month=3, startDay=10 | start=2026-02-10, end=2026-03-09 | ✅ |
| TC-MP-11 | year=2026, month=1, startDay=10 (연도 경계) | start=2025-12-10, end=2026-01-09 | ✅ |

---

## 2. 할부 금액 계산 (`calcMockInstallment`)

> 파일: `src/mocks/installment.mock.ts` → `src/tests/installment.test.ts`

| TC | 입력 | 기댓값 | 결과 |
|----|------|--------|------|
| TC-INST-01 | 300,000원 / 3개월 | monthly=100,000 / first=100,000 (나머지 0) | ✅ |
| TC-INST-02 | 100,000원 / 3개월 | monthly=33,333 / first=33,334 (나머지 1) | ✅ |
| TC-INST-03 | 100,000원 / 12개월 | monthly=8,333 / first=8,337 (나머지 4), 총합 일치 | ✅ |
| TC-INST-04 | 300,000원 / 7개월 | 1회차 + 나머지×6 = 300,000 (총합 일치) | ✅ |
| TC-INST-05 | 10,001원 / 2개월 | monthly=5,000 / first=5,001 (나머지 1) | ✅ |
| TC-INST-06 | 1원 / 3개월 | monthly=0 / first=1 (극단값) | ✅ |

**검증 원칙**: `firstMonthAmount + monthlyAmount × (months - 1) = totalAmount`

---

## 3. 월별 거래 필터링 (`txMonthFiltered`)

> 파일: `src/tests/mockupLogic.test.ts`

| TC | 시나리오 | 기댓값 | 결과 |
|----|---------|--------|------|
| TC-FILTER-01 | startDay=1, 3월 — 2월·4월 거래 제외 | id=1(3/10)만 포함 | ✅ |
| TC-FILTER-02 | startDay=25, 3월 — 2/25(포함), 2/24(제외), 3/24(포함), 3/25(제외) | id=1,3 포함 / id=2,4 제외 | ✅ |
| TC-FILTER-03 | startDay=25, 2월 — 2/24 포함 여부 | 포함 | ✅ |
| TC-FILTER-04 | 빈 거래 목록 | 빈 결과 | ✅ |
| TC-FILTER-05 | 월 첫째날·마지막날 경계값 | 2건 모두 포함 | ✅ |

---

## 4. 월 요약 계산 (`mockSummary`)

> 파일: `src/tests/mockupLogic.test.ts`

| TC | 시나리오 | 기댓값 | 결과 |
|----|---------|--------|------|
| TC-SUM-01 | 수입 3,000,000 + 지출 50,000 | income=3,000,000 / expense=50,000 / balance=2,950,000 | ✅ |
| TC-SUM-02 | isIncludedInTotal=false 수입 제외 검증 | totalIncome=0 (제외 거래 합산 안 됨) | ✅ |
| TC-SUM-03 | 수입 2건 + 지출 1건 건수 계산 | incomeCount=2 / expenseCount=1 | ✅ |
| TC-SUM-04 | 빈 거래 목록 | 모두 0 | ✅ |

---

## 5. 반복 예정 배너 (`recurringPending`)

> 파일: `src/tests/mockupLogic.test.ts`

| TC | 시나리오 | 기댓값 | 결과 |
|----|---------|--------|------|
| TC-PEND-01 | 활성 마스터 + 해당 월 거래 없음 | pending 1건 | ✅ |
| TC-PEND-02 | 활성 마스터 + 해당 월 거래 있음 | pending 0건 (이미 등록됨) | ✅ |
| TC-PEND-03 | isActive=false 마스터 | pending 0건 (비활성) | ✅ |
| TC-PEND-04 | skip 키 `1-2026-03` 존재 | pending 0건 (스킵됨) | ✅ |
| TC-PEND-05 | 다른 달 skip 키 `1-2026-02` | pending 1건 (현재 달과 무관) | ✅ |
| TC-PEND-06 | startDate > 월 end (아직 시작 안 됨) | pending 0건 | ✅ |
| TC-PEND-07 | endDate < 월 start (이미 종료됨) | pending 0건 | ✅ |
| TC-PEND-08 | 수입·지출 마스터 각 1건 | pending 2건 | ✅ |

---

## 6. 반복 skip 키 메커니즘

> 파일: `src/tests/mockupLogic.test.ts`

| TC | 시나리오 | 기댓값 | 결과 |
|----|---------|--------|------|
| TC-SKIP-01 | deleteRecurSingle(2/25 거래, 3월 화면) | skip 키 `1-2026-03` 추가 (표시 월 기준) | ✅ |
| TC-SKIP-02 | deleteRecurSingle 중복 호출 | skip 키 1개만 유지 (중복 방지) | ✅ |
| TC-SKIP-03 | deleteRecurAll(masterId=1) — skip 키 `1-*` 2건 + 다른 마스터 `2-*` 1건 | `1-*` 전체 삭제, `2-2026-03` 유지 | ✅ |
| TC-SKIP-04 | pending 컨텍스트(id=-1) 단건 삭제 | 거래 삭제 없이 skip 키만 추가, pending 0건 | ✅ |

---

## 7. 할부 삭제 로직

> 파일: `src/tests/mockupLogic.test.ts` (3회차 샘플 데이터)

| TC | 시나리오 | 기댓값 | 결과 |
|----|---------|--------|------|
| TC-INST-DEL-01 | deleteInstAll(masterId=99) | 전체 3건 삭제 | ✅ |
| TC-INST-DEL-02 | deleteInstFromHere(2회차) | 1회차 유지, 2·3회차 삭제 | ✅ |
| TC-INST-DEL-03 | deleteInstFromHere(1회차) | 전체 삭제 | ✅ |
| TC-INST-DEL-04 | deleteInstFromHere(3회차) | 1·2회차 유지, 3회차 삭제 | ✅ |
| TC-INST-DEL-05 | deleteInstSingle(2회차 id=2) | 1·3회차 유지 | ✅ |

---

## 8. 반복 삭제 로직

> 파일: `src/tests/mockupLogic.test.ts` (1·2·3월 3건 샘플)

| TC | 시나리오 | 기댓값 | 결과 |
|----|---------|--------|------|
| TC-RECUR-DEL-01 | deleteRecurAll(masterId=100) | 거래 전체 삭제 + 마스터 삭제 | ✅ |
| TC-RECUR-DEL-02 | deleteRecurAll — skip 키 정리 | `100-*` 삭제, 다른 마스터 `200-*` 유지 | ✅ |
| TC-RECUR-DEL-03 | deleteRecurFromHere(3월) | 1·2월 유지, 3월 삭제, 마스터 isActive=false | ✅ |
| TC-RECUR-DEL-04 | deleteRecurFromHere(1월) | 전체 삭제, 마스터 isActive=false | ✅ |

---

## 9. 폼 유효성 검사 (`formIsValid`)

> 파일: `src/tests/mockupLogic.test.ts`

| TC | 시나리오 | 기댓값 | 결과 |
|----|---------|--------|------|
| TC-VALID-01 | 금액=0, 나머지 입력 완료 | invalid (false) | ✅ |
| TC-VALID-02 | 금액 미입력(''), 나머지 입력 완료 | invalid (false) | ✅ |
| TC-VALID-03 | 카테고리=0(미선택), 나머지 입력 완료 | invalid (false) | ✅ |
| TC-VALID-04 | 지출 + 결제수단=0(미선택) | invalid (false) | ✅ |
| TC-VALID-05 | 지출 + 금액·카테고리·결제수단 모두 입력 | valid (true) | ✅ |
| TC-VALID-06 | 수입 + 결제수단 미선택 | valid (true) — 수입은 결제수단 불필요 | ✅ |
| TC-VALID-07 | 금액='1,000' (쉼표 포함) | valid (true) — 쉼표 파싱 정상 | ✅ |

---

## 커버리지 미대상 항목

아래 항목은 Vue 컴포넌트 렌더링/이벤트에 의존하므로 단위 테스트 범위에서 제외합니다. 수동 검증으로 대체합니다.

| 항목 | 이유 |
|------|------|
| 모달 열기/닫기 | Vue Template 이벤트 의존 |
| 할부 토글 UI (지출 전용 표시) | Vue v-if 렌더링 의존 |
| 반복 예정 배너 접기/펼치기 | showPendingBanner ref 상태 의존 |
| 금액 입력 쉼표 포맷팅 | input 이벤트 핸들러 의존 |
| bottom sheet 표시/숨김 | 사용자 클릭 이벤트 의존 |
| localStorage 영속성 | 브라우저 환경 의존 |

---

## 관련 파일

| 파일 | 역할 |
|------|------|
| `src/utils/monthPeriod.ts` | 월 기간 계산 유틸 |
| `src/mocks/installment.mock.ts` | 할부 금액 계산 |
| `src/tests/monthPeriod.test.ts` | 월 기간 테스트 |
| `src/tests/installment.test.ts` | 할부 계산 테스트 |
| `src/tests/mockupLogic.test.ts` | 목업 비즈니스 로직 테스트 |
| `src/tests/setup.ts` | Vitest 환경 설정 |
| `vitest.preload.cjs` | Node 16 crypto 폴리필 |
| `vite.config.ts` | Vitest 설정 포함 |
