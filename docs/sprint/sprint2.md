# Sprint 2 — 프론트엔드 UI 전체 구현

## 개요

| 항목 | 내용 |
|------|------|
| 스프린트 번호 | Sprint 2 |
| 브랜치 | `sprint2` |
| 기간 | 2026-03-13 ~ 2026-03-27 (2주) |
| 상태 | ⬜ 진행 예정 |
| 대상 브랜치 (PR) | `develop` |
| 백엔드 API | https://budget-tracker-api-51n7.onrender.com |

## 스프린트 목표

Vue3 + Vite + TypeScript 기반의 프론트엔드 UI를 전체 구현하고 Sprint 1에서 완성된 .NET 백엔드 API와 통합한다.
가계부 메인 화면, 거래 입력/수정, 카테고리/결제수단 관리, 포인트 예산, 통계, 검색/필터 등 핵심 화면을 모두 구현하며,
모바일 우선 반응형 UI로 제공한다.
스프린트 종료 시점에 실제 백엔드와 연동하여 전체 사용자 플로우를 브라우저에서 수동 검증 가능한 상태여야 한다.

---

## 구현 범위

### 포함
- Vue3 기본 구조 설정 — 라우터, Pinia 스토어, API 클라이언트 정비 (T11)
- 가계부 메인 화면 — 월별 거래 목록 + 수입/지출/잔액 요약 (T12)
- 거래 입력/수정 화면 — 카테고리, 결제수단, 합산여부, 반복 설정 (T13)
- 카테고리/결제수단 관리 화면 — 설정 화면에서 추가/수정/삭제 (T14)
- 포인트 예산 관리 화면 — 포인트 등록, 잔액 확인 (T15)
- 통계 화면 — 파이차트 + 전월 비교 + 월별 추이 막대그래프 (T16)
- 검색/필터 UI — 필터 패널 + 검색 결과 (T17)
- 반응형 UI 적용 — 모바일 우선 CSS, 전체 화면 최적화 (T18)
- 통합 테스트 — 전체 플로우 검증 (T19)

### 제외 (Backlog 또는 이후 스프린트)
- 사용자 인증 (JWT) — Backlog
- 예산 설정 및 초과 알림 — Backlog
- 영수증 이미지 첨부 — Backlog
- CSV 내보내기 — Backlog
- SummaryController의 DI 개선 — 별도 리팩터링 스프린트

---

## 사전 준비 사항

### 패키지 설치 (frontend/)
```bash
npm install tailwindcss @tailwindcss/vite
npm install chart.js vue-chartjs
npm install dayjs
npm install pinia vue-router
```

### 환경변수 설정
```
VITE_API_URL=https://budget-tracker-api-51n7.onrender.com
```

### 기존 코드 정리 대상
- `frontend/src/stores/counter.ts` — 삭제 후 도메인 스토어로 교체
- `frontend/src/views/HomeView.vue`, `AboutView.vue` — 교체
- `frontend/src/components/HelloWorld.vue` 등 기본 컴포넌트 — 삭제
- `frontend/src/assets/main.css`, `base.css` — Tailwind 기반으로 교체

---

## 태스크 상세

### T11 — Vue3 기본 구조 설정
**우선순위**: 최상 | **예상 소요**: 1일 | **선행 조건**: 없음

#### 목표
라우터, Pinia 스토어, API 클라이언트, Tailwind CSS를 프로젝트에 통합하여 나머지 태스크의 기반을 마련한다.

#### 세부 작업
- ⬜ Tailwind CSS 3 설치 및 `vite.config.ts` / `main.css` 설정
- ⬜ Chart.js + vue-chartjs 설치
- ⬜ Day.js 설치
- ⬜ `frontend/src/router/index.ts` 재설계 — 화면별 라우트 정의 (메인, 거래입력, 설정, 통계, 검색)
- ⬜ 도메인별 Pinia 스토어 파일 생성 (transactions, categories, paymentMethods, pointBudgets, settings)
- ⬜ `frontend/src/api/` 하위에 도메인별 API 모듈 분리 (transactions.ts, categories.ts, paymentMethods.ts, pointBudgets.ts, recurringTransactions.ts, summary.ts, settings.ts)
- ⬜ 기존 불필요 파일(counter.ts, HelloWorld.vue 등) 제거
- ⬜ `frontend/src/types/` 디렉토리 생성 — 도메인 타입 정의 (Transaction, Category, PaymentMethod, PointBudget, RecurringTransaction, UserSettings, Summary)

#### 구현 방향
```
frontend/src/
├── api/
│   ├── client.ts          (기존 유지)
│   ├── transactions.ts
│   ├── categories.ts
│   ├── paymentMethods.ts
│   ├── pointBudgets.ts
│   ├── recurringTransactions.ts
│   ├── summary.ts
│   └── settings.ts
├── stores/
│   ├── transactions.ts
│   ├── categories.ts
│   ├── paymentMethods.ts
│   ├── pointBudgets.ts
│   └── settings.ts
├── types/
│   └── index.ts
├── router/
│   └── index.ts
└── views/
    ├── MainView.vue
    ├── TransactionFormView.vue
    ├── SettingsView.vue
    ├── StatsView.vue
    └── SearchView.vue
```

---

### T12 — 가계부 메인 화면
**우선순위**: 최상 | **예상 소요**: 2일 | **선행 조건**: T11

#### 목표
월별 거래 목록과 수입/지출/잔액 요약을 한눈에 볼 수 있는 메인 화면 구현.
커스텀 월 시작일 기준으로 이전/다음 월 탐색이 가능해야 한다.

#### 세부 작업
- ⬜ `MainView.vue` — 월 탐색 헤더 (이전/다음 월 이동, 현재 월 표시)
- ⬜ 수입/지출/잔액 요약 카드 컴포넌트 (`SummaryCard.vue`) — `/api/summary/monthly` 연동
- ⬜ 거래 목록 컴포넌트 (`TransactionList.vue`) — 날짜별 그룹화, 카테고리 아이콘/색상 표시
- ⬜ 거래 항목 컴포넌트 (`TransactionItem.vue`) — 금액, 카테고리, 결제수단, 합산 여부 표시
- ⬜ 월 이동 시 Day.js를 활용한 커스텀 시작일 기준 날짜 범위 계산
- ⬜ 거래 항목 스와이프/탭으로 수정/삭제 접근 (모바일 고려)
- ⬜ FAB(Floating Action Button) — 거래 입력 화면 이동

#### 연동 API
- `GET /api/summary/monthly?year=&month=`
- `GET /api/transactions?year=&month=`
- `DELETE /api/transactions/{id}`

---

### T13 — 거래 입력/수정 화면
**우선순위**: 최상 | **예상 소요**: 2일 | **선행 조건**: T11, T12

#### 목표
거래를 입력하고 수정할 수 있는 폼 화면 구현.
카테고리, 결제수단 선택, 합산 포함 여부, 반복 지출 설정을 모두 지원한다.

#### 세부 작업
- ⬜ `TransactionFormView.vue` — 입력/수정 모드 공유 폼 (라우트 파라미터로 구분)
- ⬜ 금액 입력 필드 — 숫자 포맷팅, 수입/지출 토글
- ⬜ 날짜 선택 — Day.js 기반 date picker 또는 기본 `<input type="date">`
- ⬜ 카테고리 선택 컴포넌트 — 수입/지출 타입에 따라 필터링, 빠른 선택 그리드
- ⬜ 결제수단 선택 컴포넌트 — 현금/카드/포인트 구분 표시
- ⬜ 합산 포함 여부 토글 (IsIncludedInTotal)
- ⬜ 메모 입력 필드
- ⬜ 반복 지출 설정 섹션 — 반복 없음 / 고정 / 할부 선택, 할부 시 총 횟수 입력
- ⬜ 저장/취소 버튼, 수정 모드에서 삭제 버튼 추가
- ⬜ 폼 유효성 검사 (금액 필수, 카테고리 필수)

#### 연동 API
- `POST /api/transactions`
- `PUT /api/transactions/{id}`
- `GET /api/transactions/{id}` (수정 시 초기값 로드)
- `GET /api/categories?type=Income|Expense`
- `GET /api/payment-methods`
- `POST /api/recurring-transactions` (반복 지출 등록 시)

---

### T14 — 카테고리/결제수단 관리 화면
**우선순위**: 높음 | **예상 소요**: 1.5일 | **선행 조건**: T11

#### 목표
설정 화면에서 카테고리와 결제수단을 추가/수정/삭제할 수 있는 관리 UI 구현.

#### 세부 작업
- ⬜ `SettingsView.vue` — 탭 또는 섹션으로 카테고리/결제수단/포인트/월설정 구분
- ⬜ 카테고리 목록 — 수입/지출 탭 구분, 항목별 수정/삭제 버튼
- ⬜ 카테고리 추가 폼 — 이름 입력, 타입(수입/지출) 선택
- ⬜ 카테고리 수정 인라인 또는 모달
- ⬜ 기본(IsDefault) 카테고리 삭제 방지 처리
- ⬜ 결제수단 목록 — 타입(Cash/Card/Point)별 구분 표시
- ⬜ 결제수단 추가 폼 — 이름 입력, 타입 선택
- ⬜ 결제수단 수정/삭제
- ⬜ 커스텀 월 시작일 설정 — 1~28 사이 숫자 입력, `/api/settings` 연동

#### 연동 API
- `GET/POST/PUT/DELETE /api/categories`
- `GET/POST/PUT/DELETE /api/payment-methods`
- `GET/PUT /api/settings`

---

### T15 — 포인트 예산 관리 화면
**우선순위**: 높음 | **예상 소요**: 1일 | **선행 조건**: T11, T14

#### 목표
포인트/복지 예산 항목을 등록하고 잔액 현황을 확인하는 UI 구현.
포인트 결제수단은 T14의 결제수단 관리와 연계된다.

#### 세부 작업
- ⬜ 포인트 예산 목록 컴포넌트 (`PointBudgetList.vue`) — 포인트명, 총액, 잔액, 사용률 표시
- ⬜ 잔액 프로그레스 바 — 사용 비율 시각화
- ⬜ 포인트 예산 추가 폼 — 포인트명, 총액 입력
- ⬜ SettingsView.vue에 포인트 예산 섹션 통합
- ⬜ 포인트 결제수단(Type=Point)과 연계 설명 안내 UI

#### 연동 API
- `GET/POST /api/point-budgets`

---

### T16 — 통계 화면
**우선순위**: 높음 | **예상 소요**: 2일 | **선행 조건**: T11, T12

#### 목표
카테고리별 파이차트, 전월 대비 지출 비교, 월별 추이 막대그래프를 통합한 통계 화면 구현.

#### 세부 작업
- ⬜ `StatsView.vue` — 월 탐색 헤더 (메인 화면과 동기화)
- ⬜ 카테고리별 파이차트 (`CategoryPieChart.vue`) — Chart.js Doughnut 또는 Pie, 범례 포함
- ⬜ 카테고리별 금액 리스트 — 차트 아래 항목별 금액/비율 표시
- ⬜ 전월 비교 카드 — 이번 달 vs 지난 달 지출 금액, 증감률 표시
- ⬜ 월별 추이 막대그래프 (`MonthlyTrendChart.vue`) — Chart.js Bar, 최근 6개월
- ⬜ 수입/지출 탭 전환으로 파이차트 데이터 전환
- ⬜ 차트 로딩 스켈레톤 처리

#### 연동 API
- `GET /api/summary/category?year=&month=&type=Income|Expense`
- `GET /api/summary/trend?months=6`
- `GET /api/summary/monthly?year=&month=` (전월 비교용으로 2회 호출)

---

### T17 — 검색/필터 UI
**우선순위**: 보통 | **예상 소요**: 1.5일 | **선행 조건**: T11, T12

#### 목표
날짜, 카테고리, 결제수단, 금액 범위, 키워드 기준으로 거래를 검색·필터링하는 UI 구현.

#### 세부 작업
- ⬜ `SearchView.vue` — 검색 화면 또는 메인 화면 내 필터 패널
- ⬜ 키워드 검색 입력창 — 실시간 또는 검색 버튼 트리거
- ⬜ 필터 패널 (`FilterPanel.vue`) — 접기/펼치기 가능
  - 날짜 범위 (from ~ to)
  - 카테고리 다중 선택
  - 결제수단 다중 선택
  - 금액 범위 (최소 ~ 최대)
- ⬜ 검색 결과 목록 — TransactionList 컴포넌트 재사용
- ⬜ 활성 필터 칩(chip) 표시 — 필터 조건 시각화, 개별 제거 가능
- ⬜ 결과 없음 상태 처리

#### 연동 API
- `GET /api/transactions?keyword=&categoryId=&paymentMethodId=&from=&to=&minAmount=&maxAmount=`

---

### T18 — 반응형 UI 적용
**우선순위**: 높음 | **예상 소요**: 1일 | **선행 조건**: T12~T17 (병행 가능)

#### 목표
모바일 우선으로 설계된 CSS를 전체 화면에 일관되게 적용하고, 태블릿/PC에서도 사용성을 보장한다.

#### 세부 작업
- ⬜ 공통 레이아웃 컴포넌트 (`AppLayout.vue`) — 하단 탭바(모바일) / 사이드 메뉴(PC) 분기
- ⬜ 하단 탭바 (`BottomNav.vue`) — 메인, 통계, 검색, 설정 탭 (모바일 `sm:` 이하)
- ⬜ Tailwind 반응형 prefix 일관 적용 — `sm:`, `md:`, `lg:` 브레이크포인트 기준
- ⬜ 거래 목록 카드 레이아웃 — 모바일 1열, 태블릿 이상 2열
- ⬜ 통계 차트 크기 — 모바일에서 화면 너비 기준 반응형 크기
- ⬜ 폼 화면 — 모바일 전체화면 모달 또는 별도 페이지
- ⬜ 터치 친화적 버튼 크기 (최소 44px)
- ⬜ 공통 색상/타이포그래피 Tailwind 테마 커스터마이징 (`tailwind.config.ts`)

---

### T19 — 통합 테스트
**우선순위**: 높음 | **예상 소요**: 1일 | **선행 조건**: T11~T18 완료

#### 목표
실제 백엔드 API와 연동하여 전체 사용자 플로우를 브라우저에서 검증한다.

#### 검증 시나리오
- ⬜ **시나리오 1: 기본 거래 관리**
  - 카테고리/결제수단 추가 → 지출 거래 입력 → 메인 화면에서 목록 확인 → 거래 수정 → 거래 삭제
- ⬜ **시나리오 2: 포인트 예산 사용**
  - 포인트 예산 등록 → 결제수단에 포인트 연결 → 포인트로 지출 입력 → 잔액 차감 확인
- ⬜ **시나리오 3: 반복 지출**
  - 고정 지출 등록 → 다음 달로 이동 → 자동 반영 확인
  - 할부 지출 등록 → 잔여 횟수 확인
- ⬜ **시나리오 4: 통계 확인**
  - 여러 카테고리에 지출 입력 → 통계 화면에서 파이차트 확인 → 월별 추이 그래프 확인
- ⬜ **시나리오 5: 검색/필터**
  - 키워드 검색 → 카테고리 필터 → 날짜 범위 필터 → 금액 범위 필터
- ⬜ **시나리오 6: 커스텀 월 시작일**
  - 설정에서 월 시작일 변경 (예: 25일) → 메인 화면에서 날짜 범위 변경 확인
- ⬜ **시나리오 7: 반응형**
  - Chrome DevTools 모바일 에뮬레이터로 주요 화면 검증 (iPhone SE, iPad 기준)

---

## 의존성 및 리스크

| 리스크 | 가능성 | 영향도 | 대응 방안 |
|--------|--------|--------|-----------|
| Render 무료 플랜 콜드 스타트 (최대 50초 지연) | 높음 | 중간 | 개발 중 로컬 백엔드 병행 사용, 프로덕션 테스트 시 초기 요청 워밍업 고려 |
| Chart.js / vue-chartjs 버전 호환성 | 낮음 | 높음 | vue-chartjs 5.x + chart.js 4.x 조합 사용 (검증된 버전) |
| Tailwind CSS 3 + Vite 4 설정 충돌 | 낮음 | 높음 | `@tailwindcss/vite` 플러그인 또는 PostCSS 방식 명시적 설정 |
| Day.js 커스텀 월 시작일 계산 복잡도 | 보통 | 중간 | `useDateRange` 컴포저블로 로직 캡슐화, 단위 테스트 작성 |
| 백엔드 CORS 설정 미흡 | 낮음 | 높음 | Sprint 1 CORS 설정 확인 후 프론트 오리진 추가 필요 시 백엔드 수정 |
| 반복 지출 자동 반영 on-demand 방식 UI 피드백 | 보통 | 낮음 | 월 이동 시 로딩 인디케이터 표시, 오류 토스트 처리 |

---

## 기술적 접근 방향

### 상태 관리 전략
- 각 도메인별 Pinia 스토어 분리 (transactions, categories, paymentMethods, pointBudgets, settings)
- 스토어 내부에서 API 호출 + 로컬 캐시 관리
- 현재 선택 월(year, month)은 `useAppState` 컴포저블 또는 `router` 쿼리 파라미터로 공유

### 컴포넌트 구조 원칙
- View 컴포넌트: 데이터 페칭 + 레이아웃 (스토어 의존)
- UI 컴포넌트: Props/Emit 기반 순수 컴포넌트 (스토어 비의존)
- 공통 컴포넌트: `components/common/` — Button, Input, Modal, Toast, LoadingSpinner

### API 모듈 구조
- `api/client.ts` — 기존 HTTP 클라이언트 유지 (BASE_URL 환경변수 적용 완료)
- `api/{domain}.ts` — 도메인별 API 함수 모음 (타입 안전성 보장)

### 커스텀 월 시작일 처리
```typescript
// useDateRange.ts
function getMonthRange(year: number, month: number, startDay: number) {
  // startDay=25: 해당 월 25일 ~ 다음 달 24일
  const from = dayjs(`${year}-${month}-${startDay}`)
  const to = from.add(1, 'month').subtract(1, 'day')
  return { from: from.format('YYYY-MM-DD'), to: to.format('YYYY-MM-DD') }
}
```

---

## 완료 기준 (Definition of Done)

- ✅ 기준: 모든 항목이 체크되어야 스프린트 완료

### 기능 완료 기준
- ⬜ T11: 라우터 동작 확인, 스토어 초기화 성공, Tailwind 클래스 적용 확인
- ⬜ T12: 메인 화면에서 월별 거래 목록 + 요약 데이터가 실제 API 응답으로 렌더링
- ⬜ T13: 거래 생성/수정/삭제가 실제 DB에 반영되고 메인 화면에 즉시 반영
- ⬜ T14: 카테고리/결제수단 CRUD가 모두 동작하고 거래 폼에 반영
- ⬜ T15: 포인트 예산 등록 후 잔액이 지출 입력 시 정상 차감
- ⬜ T16: 파이차트/막대그래프가 실제 데이터로 렌더링
- ⬜ T17: 키워드 + 필터 조합 검색 결과 정상 출력
- ⬜ T18: Chrome DevTools iPhone SE(375px) 기준 모든 화면 레이아웃 깨짐 없음
- ⬜ T19: 통합 테스트 시나리오 1~7 모두 통과

### 코드 품질 기준
- ⬜ TypeScript 타입 에러 0건 (`npm run type-check`)
- ⬜ 콘솔 에러/경고 0건 (개발 서버 기준)
- ⬜ `npm run build` 성공 (프로덕션 빌드 오류 없음)

---

## 예상 산출물

| 산출물 | 설명 |
|--------|------|
| `frontend/src/views/` | MainView, TransactionFormView, SettingsView, StatsView, SearchView |
| `frontend/src/components/` | SummaryCard, TransactionList, TransactionItem, CategoryPieChart, MonthlyTrendChart, FilterPanel, BottomNav, AppLayout 등 |
| `frontend/src/stores/` | transactions, categories, paymentMethods, pointBudgets, settings |
| `frontend/src/api/` | 도메인별 API 모듈 7종 |
| `frontend/src/types/index.ts` | 전체 도메인 타입 정의 |
| `frontend/src/composables/` | useDateRange, useAppState 등 |
| `frontend/.env.example` | VITE_API_URL 추가 |
| `docs/sprint/sprint2.md` | 스프린트 계획 문서 (현재 파일) |

---

## 작업 순서 (권장)

```
T11 (기반 구조)
  └─ T12 (메인 화면)
       └─ T13 (거래 입력)
  └─ T14 (설정/관리)
       └─ T15 (포인트 예산)
  └─ T16 (통계)
  └─ T17 (검색)
  └─ T18 (반응형, T12~T17 병행)
       └─ T19 (통합 테스트)
```

T11 완료 후 T12~T17은 병행 개발 가능하나, T13은 T12 완료 후 진행 권장 (거래 목록과 폼의 상태 공유).

---

## 스프린트 회고 (완료 후 작성)

> 스프린트 완료 후 sprint-close 에이전트가 작성합니다.
