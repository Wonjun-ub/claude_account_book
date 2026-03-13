# 가계부 웹앱 (BudgetTracker) - 로드맵

## 프로젝트 개요
- **목표**: 개인 수입/지출 관리 웹앱 MVP
- **기술 스택**: CLAUDE.md 참조

---

## 확정된 요구사항

### 1. 월 관리
- 커스텀 월 시작일 설정 (예: 25일 설정 시 → 3/25 ~ 4/24가 한 달)

### 2. 카테고리
- 카테고리 직접 추가/관리 가능
- 수입/지출 카테고리 구분

### 3. 결제수단
- 현금 / 카드 구분
- 카드는 종류별로 직접 추가 가능 (예: 신한카드, 국민카드 등)

### 4. 포인트/복지 예산
- 포인트 항목 생성 시 총액 설정 (예: 복지포인트 200만원)
- 지출 작성 시 결제수단으로 해당 포인트 선택 가능
- 사용 시 잔액 차감 (200만 → 190만)
- 가계부 리스트에 내역 표시
- 거래 입력 시 합산 포함 여부 직접 선택 가능

### 5. 반복 지출
- 고정 지출: 매달 같은 날 같은 금액 자동 반영 (예: 월세, 구독료)
- 할부 지출: 총 횟수 설정, 매달 자동 입력 (예: 6개월 할부)
- 월 시작일 기준으로 자동 반영

### 6. 통계 화면
- 카테고리별 원형 그래프 (파이차트)
- 전월 대비 지출 비교
- 월별 지출 추이 막대그래프
- 원형 그래프와 같은 화면에 통합

### 7. 반응형 UI
- 모바일 우선 설계 (Mobile First)
- PC / 태블릿 / 모바일 모두 최적화

### 8. 검색 및 필터
- 날짜, 카테고리, 결제수단, 금액 범위 기준 필터
- 키워드 검색 (메모 등)

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

## Phase 1 — Sprint 1: 백엔드 핵심 API

**목표**: DB 모델 설계 + 핵심 API 구현

| ID | 태스크 | 상태 | 설명 |
|----|--------|------|------|
| T1 | 프로젝트 구조 설정 | ✅ | .NET 솔루션 + Vue3 초기화 |
| T2 | DB 모델 및 마이그레이션 | ✅ | Transaction, Category, PaymentMethod, PointBudget, RecurringTransaction 엔티티 |
| T3 | 거래 CRUD API | ✅ | GET/POST/PUT/DELETE /api/transactions (합산포함여부, 결제수단 포함) |
| T4 | 카테고리 API | ✅ | GET/POST/PUT/DELETE /api/categories |
| T5 | 결제수단 API | ✅ | GET/POST/PUT/DELETE /api/payment-methods |
| T6 | 포인트 예산 API | ✅ | GET/POST /api/point-budgets + 잔액 차감 처리 |
| T7 | 반복 지출 API | ✅ | GET/POST /api/recurring-transactions + 자동 반영 로직 |
| T8 | 월별 요약 API | ✅ | GET /api/summary/monthly (커스텀 시작일 기준) |
| T9 | 통계 API | ✅ | GET /api/summary/category, /api/summary/trend |
| T10 | 검색/필터 API | ✅ | GET /api/transactions?category=&paymentMethod=&from=&to=&keyword= |

---

## Phase 2 — Sprint 2: 프론트엔드 UI

**목표**: Vue3 UI 전체 구현 + 프론트-백엔드 통합

| ID | 태스크 | 상태 | 설명 |
|----|--------|------|------|
| T11 | Vue3 기본 구조 | ⬜ | 라우터, 상태관리(Pinia), API 클라이언트 설정 |
| T12 | 가계부 메인 화면 | ⬜ | 월별 거래 목록 + 수입/지출/잔액 요약 |
| T13 | 거래 입력/수정 화면 | ⬜ | 카테고리, 결제수단, 합산여부, 반복 설정 |
| T14 | 카테고리/결제수단 관리 | ⬜ | 설정 화면에서 추가/수정/삭제 |
| T15 | 포인트 예산 관리 화면 | ⬜ | 포인트 등록, 잔액 확인 |
| T16 | 통계 화면 | ⬜ | 파이차트 + 전월 비교 + 월별 추이 막대그래프 |
| T17 | 검색/필터 UI | ⬜ | 필터 패널 + 검색 결과 |
| T18 | 반응형 UI 적용 | ⬜ | 모바일 우선 CSS, 전체 화면 최적화 |
| T19 | 통합 테스트 | ⬜ | 전체 플로우 검증 |

---

## DB 모델

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

### PointBudget (포인트 예산)
| 필드 | 타입 | 설명 |
|------|------|------|
| Id | int | PK |
| Name | string | 포인트명 (예: 복지포인트) |
| TotalAmount | decimal | 총액 |
| RemainingAmount | decimal | 잔액 |

### RecurringTransaction (반복 지출)
| 필드 | 타입 | 설명 |
|------|------|------|
| Id | int | PK |
| Amount | decimal | 금액 |
| CategoryId | int (FK) | 카테고리 |
| PaymentMethodId | int (FK) | 결제수단 |
| Type | enum | Fixed / Installment |
| DayOfMonth | int | 매월 반복 일자 |
| TotalInstallments | int? | 총 할부 횟수 |
| RemainingInstallments | int? | 남은 할부 횟수 |
| Memo | string? | 메모 |

### UserSettings (사용자 설정)
| 필드 | 타입 | 설명 |
|------|------|------|
| Id | int | PK |
| MonthStartDay | int | 월 시작일 (기본값: 1) |

---

## API 엔드포인트

| 메서드 | 경로 | 설명 |
|--------|------|------|
| GET | /api/transactions | 거래 목록 (필터/검색 지원) |
| POST | /api/transactions | 거래 생성 |
| GET | /api/transactions/{id} | 거래 상세 |
| PUT | /api/transactions/{id} | 거래 수정 |
| DELETE | /api/transactions/{id} | 거래 삭제 |
| GET | /api/categories | 카테고리 목록 |
| POST | /api/categories | 카테고리 생성 |
| PUT | /api/categories/{id} | 카테고리 수정 |
| DELETE | /api/categories/{id} | 카테고리 삭제 |
| GET | /api/payment-methods | 결제수단 목록 |
| POST | /api/payment-methods | 결제수단 생성 |
| PUT | /api/payment-methods/{id} | 결제수단 수정 |
| DELETE | /api/payment-methods/{id} | 결제수단 삭제 |
| GET | /api/point-budgets | 포인트 예산 목록 |
| POST | /api/point-budgets | 포인트 예산 생성 |
| GET | /api/recurring-transactions | 반복 지출 목록 |
| POST | /api/recurring-transactions | 반복 지출 등록 |
| DELETE | /api/recurring-transactions/{id} | 반복 지출 삭제 |
| GET | /api/summary/monthly | 월별 수입/지출/잔액 (커스텀 시작일 기준) |
| GET | /api/summary/category | 카테고리별 집계 |
| GET | /api/summary/trend | 월별 추이 |
| GET | /api/settings | 사용자 설정 조회 |
| PUT | /api/settings | 사용자 설정 수정 (월 시작일 등) |

---

## Backlog (향후 개발)

- 사용자 인증 (JWT)
- 예산 설정 및 초과 알림
- 영수증 이미지 첨부
- CSV 내보내기

---

## Sprint 이력

| Sprint | 상태 | 내용 | 완료일 |
|--------|------|------|--------|
| Sprint 1 | ✅ 완료 | 백엔드 핵심 API | 2026-03-13 |
| Sprint 2 | ⬜ 예정 | 프론트엔드 UI | — |
