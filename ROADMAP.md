# 가계부 웹앱 (BudgetTracker) - 로드맵

## 프로젝트 개요
- **목표**: 개인 수입/지출 관리 웹앱 MVP
- **기술 스택**: CLAUDE.md 참조
- **PRD**: `docs/PRD.md` (v1.1, 2026-03-15 기준)

---

## 스프린트 구성 원칙

요구사항은 언제든 변경될 수 있다. 백엔드/DB 변경이 조기에 일어나는 것을 방지하기 위해, 모든 새 기능은 다음 순서를 따른다.

```
목업 스프린트 (프론트엔드만, mock 데이터)
  → 요구사항 확정
    → 구현 스프린트 (DB + 백엔드 + 프론트 연동)
```

| 스프린트 유형 | 특징 | DB/백엔드 변경 |
|--------------|------|--------------|
| 목업 | 하드코딩된 mock 데이터, UI/UX 흐름 확인 | 없음 |
| 구현 | 목업 확인 + 요구사항 확정 후 진행 | 있음 |
| 버그 수정 | 범위가 작고 명확한 경우 목업 생략 가능 | 없음 또는 최소 |

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
- **반복 (Fixed Recurring)**: 매달 같은 날 같은 금액 자동 반영
- **할부 (Installment)**: 총금액+개월수 입력, 카드사 방식 자동 계산 (Sprint 5에서 전면 개편)
- 모달에서 3가지 유형 탭으로 명확히 선택 (Sprint 3에서 UI 개편, Sprint 4에서 목업 완성)

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
- 카테고리 필터 칩 (메인 화면)
- 메모 키워드 검색

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
| T7 | 반복 지출 API | ✅ | GET/POST /api/recurring-transactions + 자동 반영 로직 |
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

## Phase 4 — Sprint 4: 할부 + 카드 결제 현황 목업 ⬜ 예정

**유형**: 목업
**목표**: 할부 상세 팝업 + 카드 결제 현황 탭 전체 UI/UX 검증 (모두 mock 데이터)
**브랜치**: `sprint4`
**DB 스키마 변경**: 없음 (프론트엔드 전용)
**선행 조건**: Sprint 3 완료

| ID | 태스크 | 상태 | 유형 | 설명 |
|----|--------|------|------|------|
| T22-mock | 할부 거래 상세 팝업 목업 | ⬜ | 목업 | InstallmentDetailModal — mock 데이터 기반 |
| T23-mock | 할부 등록 모달 목업 완성 | ⬜ | 목업 | 할부 탭 mock 저장 → 전체 흐름 시뮬레이션 |
| T24-mock | 카드 결제 현황 탭 목업 | ⬜ | 목업 | CardBillingView + 2슬롯 + 드릴다운 (mock 데이터) |
| T25-mock | 카드 청구 설정 UI 목업 | ⬜ | 목업 | 설정 화면 정산일/결제일 입력 폼 (mock 저장) |

**Sprint 4 완료 후 확정 항목**
- 할부 상세 팝업 표시 항목 최종 확정
- 카드 결제 현황 탭 레이아웃 및 드릴다운 방식 확정 (슬라이드업 vs 별도 화면)
- 하단 탭바 5번째 탭 레이아웃 방식 확정

---

## Phase 5 — Sprint 5: 할부 기능 실제 구현 ⬜ 예정

**유형**: 구현
**목표**: InstallmentTransactions 테이블 분리 + 카드사 방식 계산 + 프론트 연동
**브랜치**: `sprint5`
**DB 스키마 변경**: `InstallmentTransactions` 테이블 신규 + `Transactions`에 FK 컬럼 추가
**선행 조건**: Sprint 4 완료 + 할부 UI 요구사항 확정

| ID | 태스크 | 상태 | 유형 | 설명 |
|----|--------|------|------|------|
| T30 | DB 모델 및 마이그레이션 | ⬜ | 구현 | InstallmentTransaction 엔티티 + EF Core 마이그레이션 |
| T31 | 할부 백엔드 API | ⬜ | 구현 | POST/GET/DELETE /api/installment-transactions + 카드사 방식 계산 |
| T32 | 할부 on-demand 자동 반영 | ⬜ | 구현 | 회차별 금액 계산 + 완료 시 비활성화 |
| T33 | 프론트엔드 mock → 실제 API 교체 | ⬜ | 구현 | InstallmentDetailModal + TransactionModal 연동 |

---

## Phase 6 — Sprint 6: 카드 결제 현황 실제 구현 ⬜ 예정

**유형**: 구현
**목표**: PaymentMethods 정산일/결제일 컬럼 추가 + 2슬롯 청구 현황 API + 프론트 연동
**브랜치**: `sprint6`
**DB 스키마 변경**: `PaymentMethods`에 `BillingCutoffDay`, `PaymentDueDay` 컬럼 추가
**선행 조건**: Sprint 4 완료 + 카드 결제 현황 UI 요구사항 확정

| ID | 태스크 | 상태 | 유형 | 설명 |
|----|--------|------|------|------|
| T34 | DB 모델 및 마이그레이션 | ⬜ | 구현 | PaymentMethod에 정산일/결제일 컬럼 추가 |
| T35 | 카드 청구 설정 API | ⬜ | 구현 | PUT /api/payment-methods/{id} 확장 |
| T36 | 카드 결제 현황 백엔드 API | ⬜ | 구현 | GET /api/card-billing/summary (2슬롯 계산) + 드릴다운 |
| T37 | 프론트엔드 mock → 실제 API 교체 | ⬜ | 구현 | CardBillingView + SettingsView 연동 |

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

### RecurringTransaction (반복 지출 — Fixed 전용)
| 필드 | 타입 | 설명 |
|------|------|------|
| Id | int | PK |
| Amount | decimal | 금액 |
| CategoryId | int (FK) | 카테고리 |
| PaymentMethodId | int (FK) | 결제수단 |
| Type | enum | Fixed (Installment는 Sprint 5에서 분리) |
| DayOfMonth | int | 매월 반복 일자 |
| Memo | string? | 메모 |

### InstallmentTransaction (할부 원부 — Sprint 5에서 신규 추가)
| 필드 | 타입 | 설명 |
|------|------|------|
| Id | int | PK |
| TotalAmount | decimal | 총 금액 |
| MonthlyAmount | decimal | 월 할부금 = floor(총금액 ÷ 개월수) |
| FirstMonthExtra | decimal | 첫 달 추가금 = 총금액 mod 개월수 |
| TotalInstallments | int | 총 개월수 |
| RemainingInstallments | int | 남은 개월수 |
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
| GET | /api/transactions | 거래 목록 (필터/검색 지원) | ✅ (Sprint 3에서 monthStartDay 버그 수정 예정) |
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
| POST | /api/recurring-transactions | 반복 지출 등록 | ✅ |
| DELETE | /api/recurring-transactions/{id} | 반복 지출 삭제 | ✅ |
| GET | /api/summary/monthly | 월별 수입/지출/잔액 (커스텀 시작일 기준) | ✅ |
| GET | /api/summary/category | 카테고리별 집계 | ✅ |
| GET | /api/summary/trend | 월별 추이 | ✅ |
| GET | /api/settings | 사용자 설정 조회 | ✅ |
| PUT | /api/settings | 사용자 설정 수정 (월 시작일 등) | ✅ |
| POST | /api/installment-transactions | 할부 등록 | ⬜ Sprint 5 |
| GET | /api/installment-transactions | 할부 목록 | ⬜ Sprint 5 |
| GET | /api/installment-transactions/{id} | 할부 상세 | ⬜ Sprint 5 |
| DELETE | /api/installment-transactions/{id} | 할부 삭제 | ⬜ Sprint 5 |
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
| Sprint 4 | 목업 | ⬜ 예정 | 할부 + 카드 결제 현황 목업 (T22-mock~T25-mock) | — |
| Sprint 5 | 구현 | ⬜ 예정 | 할부 기능 실제 구현 (T30~T33) | — |
| Sprint 6 | 구현 | ⬜ 예정 | 카드 결제 현황 실제 구현 (T34~T37) | — |
