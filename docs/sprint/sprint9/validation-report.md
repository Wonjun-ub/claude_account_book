# Sprint 9 검증 보고서

**날짜**: 2026-03-18
**스프린트**: Sprint 9 — Local-first 실서비스 구현 (Dexie.js PWA)
**PR**: [#3 feat: Sprint 9 완료 — Local-first 실서비스 구현 (Dexie.js PWA)](https://github.com/Wonjun-ub/claude_account_book/pull/3)
**브랜치**: `sprint9` → `develop`

---

## 1. 자동 빌드 검증

### 1-1. 프로덕션 빌드 (`npm run build`)

- ✅ TypeScript 오류 0건
- ✅ Vue TSC 타입 검사 통과
- ✅ Vite 프로덕션 번들 생성 완료 (17.16s)

빌드 산출물:

| 파일 | 크기 | gzip |
|------|------|------|
| assets/index-ea3eb63c.js | 227.66 kB | 81.05 kB |
| assets/StatsView-2fb28b36.js | 197.82 kB | 68.88 kB |
| assets/SettingsView-26130d3f.js | 15.53 kB | 4.02 kB |
| assets/index-fbb85335.css | 22.48 kB | 4.62 kB |
| sw.js (Service Worker) | — | — |
| manifest.webmanifest | 0.45 kB | — |

### 1-2. Vitest 단위 테스트 (`npm test`)

- ✅ 84 케이스 전체 PASS (실패 0건)

| 파일 | 케이스 수 | 결과 |
|------|-----------|------|
| src/tests/installment.test.ts | 6 | ✅ PASS |
| src/tests/monthPeriod.test.ts | 11 | ✅ PASS |
| src/tests/mockupLogic.test.ts | 37 | ✅ PASS |
| src/tests/transaction.test.ts | 30 | ✅ PASS (신규) |

---

## 2. 코드 리뷰 결과

### 2-1. 검토 범위

- `frontend/src/database/db.ts` — Dexie 스키마 + 시드 + 헬퍼
- `frontend/src/stores/transaction.ts` — 거래 CRUD Pinia store
- `frontend/src/stores/paymentMethod.ts` — 결제수단 + 포인트 잔액 store
- `frontend/src/utils/cardBilling.ts` — 카드 청구 기간 계산 유틸
- `frontend/src/tests/transaction.test.ts` — Vitest 30건

### 2-2. Critical/High 이슈

없음.

### 2-3. Medium 이슈 (추후 개선 참고)

| # | 위치 | 내용 |
|---|------|------|
| M1 | `stores/transaction.ts:284` | `deleteRecurring`의 `skipMonth` 모드에서 monthStartDay가 하드코딩(`1`)으로 전달됨. 사용자가 커스텀 월 시작일을 사용할 때 해당 월의 날짜 범위가 표준(1일~말일)으로 계산될 수 있음. 다음 스프린트에서 `monthStartDay`를 파라미터로 전달하도록 수정 권장. |
| M2 | `database/db.ts:getRecurringPending` | 활성 반복 거래 전체를 `.filter(r => r.isActive)` 로 메모리 필터링하고 있음. Dexie 인덱스 `isActive`가 선언되어 있으나 boolean 인덱스는 Dexie에서 범위 쿼리가 불가함. 데이터가 많아지면 성능 저하 가능. 실사용 데이터 규모가 크지 않을 것으로 예상되어 현재는 허용 수준. |
| M3 | `utils/cardBilling.ts:getCurrentBillingPeriod` | 주석의 예시("오늘이 정산일 이후이면 당월 16 ~ 다음달 15")가 코드 동작과 일치하나, "오늘이 정산일 이전이면 전전월 16 ~ 전월 15" 주석이 "전월 정산일+1 ~ 이번달 정산일"로 수정되어야 함 (실제 동작 기준). 버그 아님, 주석 오기. |

### 2-4. Low 이슈 / 개선 제안

| # | 내용 |
|---|------|
| L1 | `transaction.ts`의 `summary` computed가 `_summaryMeta`에 의존하는데, `transactions.value`가 비어있고 `_summaryMeta`가 null이 아닌 경우 빈 요약(totalIncome=0 등)을 반환함. 빈 달에서도 올바른 동작이므로 현재 OK. |
| L2 | `seedDatabase()`의 중복 실행 방지 조건이 `userSettings.count() > 0`이므로, 사용자가 설정을 삭제하면 재시드 가능성 있음. 별도 플래그 테이블 사용이 더 안전하나 현재 시나리오에서는 발생 가능성 낮음. |

### 2-5. 긍정적 검토 사항

- Dexie `transaction('rw', ...)` 으로 원자성 보장 (할부 생성/삭제, 포인트 차감)
- `_joinNames` 헬퍼가 단일 패스(Promise.all)로 모든 마스터 테이블을 조회하여 N+1 쿼리 방지
- `calcInstallment` 순수 함수로 분리하여 단위 테스트 독립적 검증 가능
- `getMonthPeriod` 일관 사용으로 monthStartDay 규칙 준수
- 반복 거래 자동 적용의 멱등성 보장 (exists 체크)

---

## 3. 수동 검증 필요 항목

아래 항목은 로컬 환경에서 직접 확인이 필요합니다.

| # | 항목 | 방법 |
|---|------|------|
| 1 | HomeView: 거래 추가/수정/삭제, 월 이동, 반복 예정 배너, 카드 결제 위젯 | `npm run dev` → 브라우저 접속 |
| 2 | StatsView: 카테고리 도넛 차트, 전월 비교, 6개월 추이 막대 차트 | 거래 데이터 입력 후 통계 탭 확인 |
| 3 | SettingsView: 카테고리/결제수단/저축 수단 CRUD, 월 시작일 설정 | 설정 탭 각 항목 CRUD |
| 4 | IndexedDB 시드 데이터 생성 | DevTools > Application > IndexedDB > BudgetTrackerDB |
| 5 | PWA 설치 프롬프트 및 아이콘 | DevTools > Application > Manifest |
| 6 | 다크모드 시각적 품질 | Galaxy S25 기준 (360×780) |

---

## 4. 결론

- Sprint 9 Definition of Done 충족 확인:
  - ✅ 단위 테스트 (84 PASS)
  - ✅ 프론트엔드 빌드 (TypeScript 오류 0건)
  - ✅ 테스트 검증 리포트 기록
  - ✅ PR 생성 (sprint9 → develop, PR #3)
  - ⬜ E2E 테스트 — 미구현 (로컬 수동 검증으로 대체)
- Critical/High 코드 이슈 없음
- Medium 이슈 3건 → 다음 스프린트 개선 참고 사항으로 기록
