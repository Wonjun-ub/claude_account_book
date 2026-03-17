# 가계부 웹앱 (BudgetTracker) - 로드맵

## 프로젝트 개요
- **목표**: 개인 수입/지출 관리 웹앱 MVP
- **기술 스택**: CLAUDE.md 참조
- **PRD**: `docs/PRD.md` (v2.0, 2026-03-17 기준)

---

## ⚡ 전략 전환 (2026-03-17) — Local-first + PWA

> **배경**: 서버 운영 비용·복잡도 제거. 계정 없이 즉시 설치·사용 가능한 오프라인 앱으로 MVP를 먼저 출시.

### 변경 요약

| 항목 | 기존 (v1.x) | 변경 후 (v2.0) |
|------|------------|---------------|
| 데이터베이스 | Supabase PostgreSQL | **브라우저 IndexedDB (Dexie.js)** |
| 백엔드 | .NET 8 Web API | **없음 (제거)** |
| 배포 | Render (프론트 + 백엔드) | **Render 정적 사이트 (프론트만)** |
| 오프라인 | 불가 | **PWA — 설치·오프라인 완전 지원** |
| 인증 | 미구현 (예정) | **불필요 (로컬 데이터)** |
| 할부/반복 로직 | .NET 서버 처리 | **프론트엔드 Dexie 헬퍼로 이관** |

### 신규 파일

| 파일 | 역할 |
|------|------|
| `frontend/src/database/db.ts` | Dexie 스키마 + 초기 시드 + 할부/반복 헬퍼 |
| `frontend/vite.config.ts` | VitePWA 플러그인 설정 |
| `frontend/public/icons/` | PWA 아이콘 (192×192, 512×512) |

### 유지되는 기획

PRD의 모든 비즈니스 로직(MonthStartDay, 할부 계산, 반복 거래, 카드 청구, 포인트 예산)은 **그대로 유지**. 구현 위치만 백엔드 → 프론트엔드로 이동.

---

## 개발 프로세스 — 스프린트 3단계 원칙

> **⚠️ 필수 준수**: Sprint 4 이후 모든 스프린트는 아래 3단계를 **반드시 순서대로** 진행한다.
> 각 단계는 사용자(Product Owner)의 **명시적 승인("진행해")** 없이 다음 단계로 넘어갈 수 없다.

### 1단계 — 목업(Mockup) 페이지 구성

| 항목 | 내용 |
|------|------|
| 목표 | 실제 데이터 연결 없이 순수 UI/UX 레이아웃 구현 |
| 작업 | Vue 3 + Tailwind CSS로 하드코딩된 정적(static) 데이터로 화면 퍼블리싱 |
| 데이터 | `frontend/src/mocks/` 아래 mock 파일 사용 (빈 배열 또는 고정 샘플 데이터) |
| 결과물 | 클릭 가능한 정적 UI 컴포넌트 + 페이지 라우팅 적용 상태 |
| 완료 조건 | `npm run build` 성공, 브라우저에서 화면 확인 가능 |

### 2단계 — 목업 테스트 및 UI/UX 확정

| 항목 | 내용 |
|------|------|
| 목표 | 1단계 화면이 PRD 기획 의도 + Galaxy S25(360×780) 환경에 맞는지 검증 |
| 작업 | 폼 입력·모달·스크롤 등 인터랙션 수동 테스트, Playwright E2E 레이아웃 체크 코드 작성 |
| 결과물 | 수정사항 반영 후 UI/UX 최종 확정 |
| 완료 조건 | 사용자가 **"진행해"** 승인 → 3단계 진입 |

### 3단계 — 실제 서비스 구현 및 연동

| 항목 | 내용 |
|------|------|
| 목표 | 확정된 화면에 실제 데이터 연결 |
| 백엔드 | PRD + 제약 조건(UNIQUE, CHECK 등)에 맞춰 .NET 8 EF Core API + Supabase DB 구현 |
| 프론트엔드 | mock 데이터 → 실제 API 호출(fetch)로 교체, Pinia 상태 관리 적용 |
| 결과물 | E2E 테스트(단위 + API + 화면) 최종 통과, 스프린트 Done 처리 |
| 완료 조건 | DoD 6개 항목 전부 충족 (ROADMAP.md §스프린트 완료 기준 참조) |

---

## 스프린트 구성 원칙

> **2026-03-16 전략 변경**: 기능별 목업→구현 순서에서 **전체 목업 완성 후 일괄 구현** 방식으로 변경.
> 통계/설정 화면 UI 미확정 상태에서 백엔드/DB를 조기에 변경하는 낭비를 방지.

```
전체 목업 스프린트 (홈 + 통계 + 설정 모두 MockupView에서 확정)
  → 모든 화면 UI/UX 승인
    → 구현 스프린트 (DB + 백엔드 + 프론트 전체 연동)
```

| 스프린트 유형 | 특징 | DB/백엔드 변경 |
|--------------|------|--------------|
| 목업 | MockupView에서 mock 데이터로 UI/UX 확정 | 없음 |
| 구현 | 전체 목업 승인 후 진행 | 있음 |
| 버그 수정 | 범위가 작고 명확한 경우 목업 생략 가능 | 없음 또는 최소 |

### 스프린트 완료 기준 (Definition of Done)

모든 스프린트는 기능 구현 완료 후 **아래 조건을 모두 충족해야만 Done 처리**할 수 있다.

| # | 조건 | 도구 | 합격 기준 |
|---|------|------|----------|
| 1 | 단위 테스트 통과 | Vitest | 전체 케이스 PASS, 신규 기능 테스트 추가 |
| 2 | E2E 테스트 통과 | Playwright — Galaxy S25 (360×780) | 핵심 시나리오 PASS (목업 스프린트는 해당 없음) |
| 3 | 프론트엔드 빌드 | `npm run build` | TypeScript 오류 0건 |
| 4 | ~~백엔드 빌드~~ | ~~`dotnet build`~~ | **Local-first 전환으로 폐지** |
| 5 | 테스트 검증 리포트 | `docs/sprint/sprintN.md` §테스트 및 검증 리포트 | 결과 기록 완료 |
| 6 | PR 생성 | `sprintN → develop` | 리뷰 가능 상태로 생성 |

---

## 확정된 요구사항

### 1. 월 관리
- 커스텀 월 시작일 설정 (예: 25일 설정 시 → 3/25 ~ 4/24가 한 달)
- 거래 목록과 요약 API가 동일한 날짜 범위 기준으로 동작 (Sprint 3에서 버그 수정)

### 2. 카테고리
- 카테고리 직접 추가/관리 가능
- 수입/지출 카테고리 구분

### 3. 결제수단
- 현금 / 카드 / 포인트 구분
- 카드는 종류별로 직접 추가 가능 (예: 신한카드, 국민카드 등)
- 카드별 정산일/결제일 설정 (Sprint 6에서 구현)

### 4. 포인트/복지 예산
- 포인트 항목 생성 시 총액 설정 (예: 복지포인트 200만원)
- 지출 작성 시 결제수단으로 해당 포인트 선택 가능
- 사용 시 잔액 차감 (200만 → 190만)
- 거래 입력 시 합산 포함 여부 직접 선택 가능

### 5. 거래 유형 3분류
- **일반 (One-time)**: 단건 거래
- **반복 (Fixed Recurring)**: 매달 같은 날 같은 금액 자동 반영 (수입/지출 공통)
  - 부가 정보 섹션 내 토글, 매월 반복 일자(dayOfMonth) + 종료일(endDate) 입력
  - 실서비스: GET /api/recurring-transactions/pending 호출 시 on-demand로 미등록 반복 목록 반환, POST 적용 시 Transaction 생성
  - 삭제 옵션: 전체 삭제 / 이후 삭제 / 단건 삭제(이번 달만, 반복 유지) 3가지 bottom sheet
  - 반복 예정 배너: 이번 달 미등록 반복 항목을 검색바·카테고리 칩 사이에 요약 표시
- **할부 (Installment)**: 총금액+개월수 입력, 카드사 방식 자동 계산 (Sprint 5에서 전면 개편)
  - 금액 입력 필드 우측 인라인 토글로 선택 (지출 전용), Sprint 4 목업에서 UI/UX 확정
  - 삭제 옵션: 전체 삭제 / 이후 삭제 / 단건 삭제 3가지 bottom sheet

### 6. 통계 화면
- 카테고리별 원형 그래프 (파이차트)
- 전월 대비 지출 비교
- 월별 지출 추이 라인차트

### 7. 카드 결제 현황 (Sprint 6에서 구현)
- 카드별 정산일/결제일 기준 2슬롯(현재+다음 청구) 표시
- D-day 표기, 청구 기간 내 이용금액 합계
- 슬롯 클릭 시 해당 기간 거래 목록 드릴다운

### 8. 반응형 UI
- 모바일 우선 설계 (Mobile First, 최대 너비 512px)
- PC / 태블릿 / 모바일 모두 최적화

### 9. 검색 및 필터
- 메모 키워드 검색 + 유형 필터 + 카테고리 칩 (검색/필터 아코디언 패널)

### 10. 저축 수단
- 저축 거래에는 결제수단 대신 저축 수단(은행 계좌) 선택
- 기본 항목: 기업은행, 카카오뱅크, 현금
- 설정 화면에서 추가/관리 가능

---

## Phase 0: 프로젝트 초기 설정

- ✅ 기존 코드 정리 (Python/FastAPI 템플릿 → .NET + Vue3 교체)
- ✅ .NET 8.0 Web API 프로젝트 생성 (backend/BudgetTracker.Api/)
- ✅ Vue3 + Vite + TS 프론트 생성 (frontend/)
- ✅ Supabase PostgreSQL + EF Core 설정 (마이그레이션 적용 완료)
- ✅ CI 워크플로우 .NET 기반으로 구성
- ✅ .env.example 작성
- ⬜ Render 프론트엔드 서비스 생성 및 GitHub 연동
- ⬜ Render 백엔드 서비스 생성 및 GitHub 연동
- ⬜ Render 환경변수 설정 (DB 연결 정보 등)

---

## Phase 1 — Sprint 1: 백엔드 핵심 API ✅ 완료

**목표**: DB 모델 설계 + 핵심 API 구현
**완료일**: 2026-03-13

| ID | 태스크 | 상태 | 설명 |
|----|--------|------|------|
| T1 | 프로젝트 구조 설정 | ✅ | .NET 솔루션 + Vue3 초기화 |
| T2 | DB 모델 및 마이그레이션 | ✅ | Transaction, Category, PaymentMethod, PointBudget, RecurringTransaction 엔티티 |
| T3 | 거래 CRUD API | ✅ | GET/POST/PUT/DELETE /api/transactions |
| T4 | 카테고리 API | ✅ | GET/POST/PUT/DELETE /api/categories |
| T5 | 결제수단 API | ✅ | GET/POST/PUT/DELETE /api/payment-methods |
| T6 | 포인트 예산 API | ✅ | GET/POST /api/point-budgets + 잔액 차감 처리 |
| T7 | 반복 지출 API | ✅ | GET/POST /api/recurring-transactions + 자동 반영 로직 (on-demand 방식: ApplyRecurringTransactionsAsync 호출 시 해당 월 Transaction 생성) |
| T8 | 월별 요약 API | ✅ | GET /api/summary/monthly (커스텀 시작일 기준) |
| T9 | 통계 API | ✅ | GET /api/summary/category, /api/summary/trend |
| T10 | 검색/필터 API | ✅ | GET /api/transactions?category=&paymentMethod=&from=&to=&keyword= |

---

## Phase 2 — Sprint 2: 프론트엔드 UI ✅ 완료

**목표**: Vue3 UI 전체 구현 + 프론트-백엔드 통합
**완료일**: 2026-03-15

| ID | 태스크 | 상태 | 설명 |
|----|--------|------|------|
| T11 | Vue3 기본 구조 | ✅ | 라우터, Pinia, API 클라이언트, Tailwind CSS 3 설정 |
| T12 | 가계부 메인 화면 | ✅ | 월별 거래 목록 + 수입/지출/잔액 요약 + 카테고리 필터 칩 |
| T13 | 거래 입력/수정 화면 | ✅ | 카테고리, 결제수단, 합산여부, 반복 설정 포함 모달 |
| T14 | 카테고리/결제수단 관리 | ✅ | 설정 화면에서 추가/수정/삭제 |
| T15 | 포인트 예산 관리 화면 | ✅ | 포인트 등록, 잔액 확인 |
| T16 | 통계 화면 | ✅ | 파이차트 + 전월 비교 + 6개월 추이 라인차트 |
| T17 | 검색/필터 UI | ✅ | 키워드 검색 + 카테고리 필터 칩 |
| T18 | 반응형 UI 적용 | ✅ | 모바일 우선 CSS, 하단 탭바 |
| T19 | 통합 테스트 | ✅ | 전체 플로우 검증 |

---

## Phase 3 — Sprint 3: 버그 수정 & 거래 유형 탭 UI 목업 ✅ 완료

**유형**: 버그 수정 + 목업
**목표**: monthStartDay 버그 수정 + 거래 유형 3분류 탭 UI (할부 탭은 mock)
**브랜치**: `sprint3`
**완료일**: 2026-03-15
**DB 스키마 변경**: 없음

| ID | 태스크 | 상태 | 유형 | 설명 |
|----|--------|------|------|------|
| T20 | 거래 목록 API monthStartDay 버그 수정 | ✅ | 버그 수정 | 백엔드: DateRangeHelper.GetMonthRange() 적용 |
| T21 | 거래 입력 모달 유형 선택 탭 UI (목업) | ✅ | 목업 | 일반/반복/할부 탭, 할부는 mock 처리 |

---

## Phase 4 — Sprint 4: 할부 + 반복 거래 목업 ✅ 완료

**유형**: 목업
**목표**: 할부 CRUD + 반복 거래 CRUD + 예정 배너 UI/UX 검증 (mock 데이터)
**브랜치**: `sprint4`
**완료일**: 2026-03-15
**DB 스키마 변경**: 없음 (프론트엔드 전용)
**선행 조건**: Sprint 3 완료

| ID | 태스크 | 상태 | 유형 | 설명 |
|----|--------|------|------|------|
| T22-mock | 할부 등록 목업 | ✅ | 목업 | 인라인 할부 토글 + 개월수 입력 + 미리보기 + mock 저장 |
| T23-mock | 할부 수정/삭제 목업 | ✅ | 목업 | 클릭→수정 모달(할부 UI), X→3가지 삭제 옵션 bottom sheet |
| T25-mock | 반복 거래 등록/삭제/예정 배너 목업 | ✅ | 목업 | 부가 정보 토글, 예정 배너, 3가지 삭제 옵션 bottom sheet |
| T24-mock | 카드 결제 현황 탭 목업 | 🔄 Sprint 6 Step 1 | 목업 | CardBillingView + 2슬롯 + 드릴다운 (mock 데이터) — 2026-03-16 Sprint 6 Step 1로 이관 완료 |

> 통계/설정 화면 목업은 가계부(홈) 화면 완성 후 별도 스프린트에서 처리

**Sprint 4 완료 후 확정 항목**
- 카드 결제 현황 탭 레이아웃 및 드릴다운 방식 확정 (슬라이드업 vs 별도 화면)
- 하단 탭바 4번째 탭 레이아웃 방식 확정

---

## Phase 5 — Sprint 5: 할부 기능 실제 구현 ✅ 완료

**유형**: 구현
**목표**: InstallmentTransactions 테이블 분리 + 카드사 방식 계산 + 프론트 연동
**브랜치**: `sprint5`
**완료일**: 2026-03-15
**DB 스키마 변경**: `InstallmentTransactions` 테이블 신규 + `RecurringSkips` 테이블 신규 + `Transactions`에 FK 컬럼 추가 + `RecurringTransactions`에 StartDate/EndDate 추가
**선행 조건**: Sprint 4 완료 + 할부 UI 요구사항 확정

| ID | 태스크 | 상태 | 유형 | 설명 |
|----|--------|------|------|------|
| T30 | DB 모델 및 마이그레이션 | ✅ | 구현 | InstallmentTransaction 엔티티 + RecurringSkip 엔티티 + EF Core 마이그레이션 |
| T31 | 할부 백엔드 API | ✅ | 구현 | POST/GET/DELETE /api/installment-transactions + 카드사 방식 계산 (3가지 삭제 모드) |
| T32 | 반복 거래 API 확장 | ✅ | 구현 | DELETE 3가지 모드(all/fromHere/skipMonth) + GET /pending + RecurringSkips 테이블 |
| T33 | 프론트엔드 mock → 실제 API 교체 | ✅ | 구현 | TransactionModal 할부 API 연동 + DeleteOptionSheet.vue + HomeView 배너/뱃지 |

---

## Phase 6 — Sprint 6: 카드 결제 현황 + 홈 탭 UI 개선 목업 ✅ 완료

**유형**: 목업 (실서비스 구현은 Sprint 8로 이관)
**목표**: 카드 결제 현황 위젯 + 홈 탭 전체 UX 개선 목업 확정
**브랜치**: `sprint6`
**DB 스키마 변경**: 없음
**선행 조건**: Sprint 5 완료 ✅

| ID | 태스크 | 상태 | 유형 | 설명 |
|----|--------|------|------|------|
| Step 1 | 홈 탭 목업 전체 개선 (v3) | ✅ 완료 (2026-03-16) | 목업 | 다크모드, 저축 수단, 필터 아코디언, 카드 결제 위젯, 반복 배너 개선 |
| Step 2 | UI/UX 확정 | ✅ 승인 완료 (2026-03-16) | 승인 | 로컬 `/mock-up` 확인 후 승인 완료 |
| T34~T37 | 카드 결제 현황 실서비스 구현 | ⬜ **Sprint 9로 이관** | 구현 | 전체 목업 승인 후 Sprint 9에서 진행 |

---

## Phase 7 — Sprint 7: 통계 화면 목업 ✅ 완료

**유형**: 목업
**목표**: MockupView 통계 탭 UI/UX 목업 확정
**브랜치**: `sprint7`
**DB 스키마 변경**: 없음
**선행 조건**: Sprint 6 Step 2 승인 ✅
**기간**: 2026-03-16 ~ 2026-03-17

| ID | 태스크 | 상태 | 설명 |
|----|--------|------|------|
| T38 | 통계 탭 다크모드 목업 | ✅ 완료 | 카테고리별 도넛 차트, 수입/지출/저축 탭, 전월 비교 4열 카드 |
| T39 | 월별 비교 섹션 목업 | ✅ 완료 | 최근 6개월 막대그래프 추이, 수입/지출/저축 색상 통일 |

---

## Phase 8 — Sprint 8: 설정 화면 목업 🔄 Step 1 완료 / Step 2 승인 대기

**유형**: 목업
**목표**: MockupView 설정 탭 UI/UX 목업 확정
**브랜치**: `sprint8`
**DB 스키마 변경**: 없음
**선행 조건**: Sprint 7 Step 2 승인 ✅ (2026-03-17)
**시작일**: 2026-03-17

| ID | 태스크 | 상태 | 설명 |
|----|--------|------|------|
| T40 | 카드 청구 설정 목업 | ✅ 완료 | 결제수단 클릭 시 인라인 정산일/결제일 설정 |
| T41 | 저축 수단 관리 목업 | ✅ 완료 | 기업은행/카카오뱅크/현금 기본값, 추가/삭제 |
| T42 | 반복 거래 관리 목업 | ~~스코프 외~~ | 설정 탭 제거 확정 — 홈 화면 바텀 시트에서 직접 처리 |
| T43 | 카테고리 관리 목업 | ✅ 완료 | 수입/지출/저축 카테고리 탭 구분 UI |
| T44 | 결제수단 관리 목업 | ✅ 완료 | 신용/체크/현금/포인트 타입 구분, 인라인 설정 |

---

## Phase 9 — Sprint 9: Local-first 실서비스 구현 ⬜ 대기

**유형**: 구현 (Local-first)
**목표**: 전체 목업 확정 후 Dexie.js 기반 실서비스 일괄 구현 (백엔드 없음)
**브랜치**: `sprint9` (시작 시 생성)
**선행 조건**: Sprint 8 Step 2 승인 (설정 목업 최종 확정)

### Dexie Store 구현

| ID | 태스크 | 상태 | 설명 |
|----|--------|------|------|
| T34 | Pinia stores 구현 | ⬜ | useTransactionStore, useCategoryStore, useSettingsStore 등 |
| T35 | 거래 CRUD (Dexie) | ⬜ | 단건/할부/반복 생성·수정·삭제 — db.ts 헬퍼 활용 |
| T36 | 카드 결제 현황 (로컬 계산) | ⬜ | 카드 청구 기간 내 거래 합산 — getMonthPeriod 응용 |
| T37 | 반복 거래 자동 적용 | ⬜ | 월 진입 시 applyRecurringForMonth() 호출 |

### 저축 수단 / 설정

| ID | 태스크 | 상태 | 설명 |
|----|--------|------|------|
| T45 | 저축 수단 CRUD (Dexie) | ⬜ | db.savingsMethods 기반 |
| T46 | 설정 CRUD (Dexie) | ⬜ | monthStartDay, 카테고리, 결제수단, 포인트 예산 |
| T47 | 포인트 잔액 자동 차감 | ⬜ | 지출 저장 시 paymentMethod.remainingBudget 갱신 |

### 화면 연동

| ID | 태스크 | 상태 | 설명 |
|----|--------|------|------|
| T48 | HomeView — Dexie 연동 | ⬜ | MockupView → HomeView 이관, mock 데이터 제거 |
| T49 | StatsView — Dexie 연동 | ⬜ | 목업 기반 StatsView 완성 |
| T50 | SettingsView — Dexie 연동 | ⬜ | 목업 기반 SettingsView 완성 |
| T51 | PWA 아이콘 생성 | ⬜ | public/icons/icon-192.png, icon-512.png |

---

## DB 모델 (현재 기준)

### Transaction (거래)
| 필드 | 타입 | 설명 |
|------|------|------|
| Id | int | PK |
| Amount | decimal | 금액 |
| Date | DateTime | 거래일 |
| Memo | string? | 메모 |
| Type | enum | Income / Expense |
| CategoryId | int (FK) | 카테고리 |
| PaymentMethodId | int (FK) | 결제수단 |
| IsIncludedInTotal | bool | 합산 포함 여부 |
| RecurringTransactionId | int? (FK) | 반복 거래 참조 |
| InstallmentTransactionId | int? (FK) | 할부 원부 참조 (Sprint 5에서 추가) |
| InstallmentIndex | int? | 할부 회차 (Sprint 5에서 추가) |

### Category (카테고리)
| 필드 | 타입 | 설명 |
|------|------|------|
| Id | int | PK |
| Name | string | 카테고리명 |
| Type | enum | Income / Expense |
| IsDefault | bool | 기본 시드 여부 |

### PaymentMethod (결제수단)
| 필드 | 타입 | 설명 |
|------|------|------|
| Id | int | PK |
| Name | string | 결제수단명 (예: 신한카드) |
| Type | enum | Cash / Card / Point |
| IsDefault | bool | 기본 항목 여부 |
| BillingCutoffDay | int? | 정산일 (Card 타입 전용, Sprint 6에서 추가) |
| PaymentDueDay | int? | 결제일 (Card 타입 전용, Sprint 6에서 추가) |

### PointBudget (포인트 예산)
| 필드 | 타입 | 설명 |
|------|------|------|
| Id | int | PK |
| Name | string | 포인트명 (예: 복지포인트) |
| TotalAmount | decimal | 총액 |
| RemainingAmount | decimal | 잔액 |

### RecurringTransaction (반복 원부 — Fixed 전용)
| 필드 | 타입 | 설명 |
|------|------|------|
| Id | int | PK |
| Amount | decimal | 금액 |
| Type | enum | Income / Expense |
| CategoryId | int (FK) | 카테고리 |
| PaymentMethodId | int (FK) | 결제수단 |
| DayOfMonth | int | 매월 반복 일자 (1~28) |
| StartDate | DateTime | 반복 시작일 |
| EndDate | DateTime? | 반복 종료일 (null = 무기한) |
| IsActive | bool | 활성 여부 |
| Memo | string? | 메모 |

> **자동 반영 메커니즘**: on-demand 방식 — `ApplyRecurringTransactionsAsync(year, month)` 호출 시 해당 월 활성 원부를 조회하여 미생성 Transaction을 INSERT. 멱등성 보장(`GetByRecurringAndDateAsync`로 중복 체크). `EndDate` 초과 또는 스킵 등록 시 건너뜀.

### InstallmentTransaction (할부 원부 — Sprint 5에서 신규 추가)
| 필드 | 타입 | 설명 |
|------|------|------|
| Id | int | PK |
| TotalAmount | decimal | 총 금액 |
| MonthlyAmount | decimal | 월 할부금 = floor(총금액 ÷ 개월수) |
| FirstMonthAmount | decimal | 1회차 금액 = MonthlyAmount + (총금액 mod 개월수) |
| TotalInstallments | int | 총 개월수 |
| StartDate | DateTime | 첫 번째 할부 날짜 |
| CategoryId | int (FK) | 카테고리 |
| PaymentMethodId | int (FK) | 결제수단 |
| Memo | string? | 메모 |
| IsActive | bool | 활성 여부 |

### UserSettings (사용자 설정)
| 필드 | 타입 | 설명 |
|------|------|------|
| Id | int | PK |
| MonthStartDay | int | 월 시작일 (기본값: 1) |

---

## API 엔드포인트

| 메서드 | 경로 | 설명 | 상태 |
|--------|------|------|------|
| GET | /api/transactions | 거래 목록 (필터/검색 지원) | ✅ (Sprint 3에서 monthStartDay 버그 수정 완료) |
| POST | /api/transactions | 거래 생성 | ✅ |
| GET | /api/transactions/{id} | 거래 상세 | ✅ |
| PUT | /api/transactions/{id} | 거래 수정 | ✅ |
| DELETE | /api/transactions/{id} | 거래 삭제 | ✅ |
| GET | /api/categories | 카테고리 목록 | ✅ |
| POST | /api/categories | 카테고리 생성 | ✅ |
| PUT | /api/categories/{id} | 카테고리 수정 | ✅ |
| DELETE | /api/categories/{id} | 카테고리 삭제 | ✅ |
| GET | /api/payment-methods | 결제수단 목록 | ✅ |
| POST | /api/payment-methods | 결제수단 생성 | ✅ |
| PUT | /api/payment-methods/{id} | 결제수단 수정 (Sprint 6에서 청구 설정 확장) | ✅ |
| DELETE | /api/payment-methods/{id} | 결제수단 삭제 | ✅ |
| GET | /api/point-budgets | 포인트 예산 목록 | ✅ |
| POST | /api/point-budgets | 포인트 예산 생성 | ✅ |
| GET | /api/recurring-transactions | 반복 지출 목록 | ✅ |
| GET | /api/recurring-transactions/pending?year=Y&month=M | 미등록 반복 목록 조회 | ✅ Sprint 5 |
| POST | /api/recurring-transactions | 반복 지출 등록 | ✅ |
| DELETE | /api/recurring-transactions/{id} | 반복 지출 삭제 | ✅ |
| GET | /api/summary/monthly | 월별 수입/지출/잔액 (커스텀 시작일 기준) | ✅ |
| GET | /api/summary/category | 카테고리별 집계 | ✅ |
| GET | /api/summary/trend | 월별 추이 | ✅ |
| GET | /api/settings | 사용자 설정 조회 | ✅ |
| PUT | /api/settings | 사용자 설정 수정 (월 시작일 등) | ✅ |
| POST | /api/installment-transactions | 할부 등록 | ✅ Sprint 5 |
| GET | /api/installment-transactions | 할부 목록 | ✅ Sprint 5 |
| DELETE | /api/installment-transactions/{id} | 할부 삭제 (all/fromHere/single 모드) | ✅ Sprint 5 |
| GET | /api/card-billing/summary | 카드별 2슬롯 청구 현황 | ⬜ Sprint 6 |
| GET | /api/card-billing/{id}/transactions | 청구 기간 거래 목록 (드릴다운) | ⬜ Sprint 6 |

---

## Backlog (향후 개발)

- 사용자 인증 (JWT)
- 예산 설정 및 초과 알림
- 영수증 이미지 첨부
- CSV 내보내기
- 날짜/금액 범위 필터 UI (API는 이미 지원)

---

## Sprint 이력

| Sprint | 유형 | 상태 | 내용 | 완료일 |
|--------|------|------|------|--------|
| Sprint 1 | 구현 | ✅ 완료 | 백엔드 핵심 API (T1~T10) | 2026-03-13 |
| Sprint 2 | 구현 | ✅ 완료 | 프론트엔드 UI 전체 구현 (T11~T19) | 2026-03-15 |
| Sprint 3 | 버그 수정 + 목업 | ✅ 완료 | monthStartDay 버그 수정 + 거래 유형 탭 UI 목업 (T20~T21) | 2026-03-15 |
| Sprint 4 | 목업 | ✅ 완료 | 할부 CRUD 목업 (T22-mock, T23-mock) + 반복 거래 CRUD 목업 (T25-mock) + Vitest 테스트 환경 구축 (54 케이스) | 2026-03-15 |
| Sprint 5 | 구현 | ✅ 완료 | 할부/반복 거래 실서비스 이관 (T30~T33) + RecurringSkips + MonthlySummary 건수 추가 | 2026-03-15 |
| Sprint 6 | 목업 | ✅ 완료 | 홈 탭 전체 UX 개선 목업 v3 (다크모드, 저축 수단, 필터 아코디언, 카드 결제 위젯, 반복 배너 개선) | 2026-03-16 |
| Sprint 7 | 목업 | ✅ 완료 | 통계 탭 목업 (T38: 도넛 차트/전월 비교, T39: 6개월 막대 추이) + 추가 개선 (수입 대비 %, 잔액 바, 범례 비율 표시) | 2026-03-17 |
| Sprint 8 | 목업 | 🔄 진행 중 | 설정 탭 목업 (T40: 카드 청구 인라인, T41: 저축 수단, T43: 카테고리 탭, T44: 결제수단) — Step 2 승인 대기 | — |
| Sprint 9 | 구현 | ⬜ 대기 | 전체 실서비스 구현 — 카드 결제 현황 + 저축 수단 + 통계/설정 UI 연동 (Sprint 8 완료 후 시작) | — |
