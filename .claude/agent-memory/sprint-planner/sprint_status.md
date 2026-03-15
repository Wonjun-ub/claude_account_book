---
name: 스프린트 진행 현황
description: 각 스프린트의 상태, 목표, 주요 달성 사항 기록
type: project
---

## Sprint 1
- 상태: ✅ 완료 (2026-03-13)
- 브랜치: `sprint1`
- 목표: 백엔드 핵심 API (T1~T10) 전체 구현
- 계획 문서: `docs/sprint/sprint1.md`

**주요 달성 사항:**
- .NET 9 Web API + Vue3 초기 프로젝트 구조 설정
- 6개 엔티티 + EF Core + InitialCreate 마이그레이션
- 7개 컨트롤러 전체 구현 (3계층 아키텍처로 Sprint 2 중 리팩터링됨)
- DateRangeHelper: 커스텀 월 시작일 기준 날짜 범위 계산

## Sprint 2
- 상태: ✅ 완료 (2026-03-15)
- 브랜치: `sprint2` (develop에서 분기 → sprint3에 통합)
- 목표: 프론트엔드 UI 전체 구현 + 프론트-백엔드 통합 (T11~T19)
- 계획 문서: `docs/sprint/sprint2.md`

**주요 달성 사항:**
- Vue3 기반 전체 UI 구현 (HomeView, StatsView, SettingsView)
- Pinia 스토어, API 모듈, Tailwind CSS 3 통합
- 3계층 아키텍처 리팩터링 (Controller → Service → Repository)
- 통계 차트 (파이차트 + 6개월 추이 라인차트) 구현
- 거래 모달: 카테고리/결제수단/합산여부/반복 설정 포함

**남아있는 버그 (Sprint 3에서 수정 예정):**
- 거래 목록 API가 monthStartDay 무시 → 요약 API와 날짜 범위 불일치

## Sprint 3
- 상태: ✅ 완료 (2026-03-15)
- 유형: 버그 수정 + 목업
- 브랜치: `sprint3`
- 목표: monthStartDay 버그 수정 + 거래 유형 3분류 탭 UI (할부 탭은 mock)
- 계획 문서: `docs/sprint/sprint3.md`
- DB 스키마 변경: 없음
- PR: https://github.com/Wonjun-ub/claude_account_book/pull/1 (sprint3 → develop)

**달성 사항:**
- T20: TransactionService.GetAllAsync()에 ISettingsRepository 주입. year/month 파라미터 사용 시 DateRangeHelper.GetMonthRange()로 변환, from/to 직접 지정 시 기존 동작 유지
- T21: TransactionModal.vue 전면 개편 — 일반/반복/할부 3탭 UI. 수정 모드 read-only. 할부 목업(installment.mock.ts) 분리

**주의사항:**
- 수동 검증 미완료: Docker 미실행으로 dotnet test, API curl, Playwright 미수행. deploy.md 참조

## Sprint 4
- 상태: ⬜ 예정 (Sprint 3 완료 후)
- 유형: 목업
- 브랜치: `sprint4`
- 목표: 할부 + 카드 결제 현황 목업 (두 기능을 한 스프린트에서 동시 검증)
- 계획 문서: `docs/sprint/sprint4.md`
- DB 스키마 변경: 없음 (프론트엔드 전용)

**구현 범위:**
- T22-mock: 할부 거래 상세 팝업 목업 (InstallmentDetailModal)
- T23-mock: 할부 등록 모달 mock 저장 완성
- T24-mock: 카드 결제 현황 탭 목업 (CardBillingView + 2슬롯 + 드릴다운)
- T25-mock: 설정 화면 카드 청구 설정 UI 목업

**Sprint 4 완료 후 확정 필요 항목:**
- 할부 상세 팝업 표시 항목
- 카드 결제 현황 탭 드릴다운 방식 (슬라이드업 vs 별도 화면)
- 하단 탭바 5번째 탭 레이아웃

## Sprint 5
- 상태: ⬜ 예정 (Sprint 4 완료 + 할부 요구사항 확정 후)
- 유형: 구현
- 브랜치: `sprint5`
- 목표: 할부 기능 실제 구현 (DB + 백엔드 + 프론트 연동)
- 계획 문서: `docs/sprint/sprint5.md`
- DB 스키마 변경: InstallmentTransactions 테이블 신규 + Transactions에 FK 추가

**구현 범위:**
- T30: DB 모델 및 마이그레이션
- T31: 할부 백엔드 API (카드사 방식 계산)
- T32: 할부 on-demand 자동 반영 + 회차별 금액 + 완료 비활성화
- T33: 프론트엔드 mock → 실제 API 교체

## Sprint 6
- 상태: ⬜ 예정 (Sprint 4 완료 + 카드 결제 현황 요구사항 확정 후)
- 유형: 구현
- 브랜치: `sprint6`
- 목표: 카드 결제 현황 탭 신규 구현 (DB + 백엔드 + 프론트 연동)
- 계획 문서: `docs/sprint/sprint6.md`
- DB 스키마 변경: PaymentMethods에 BillingCutoffDay, PaymentDueDay 추가

**구현 범위:**
- T34: DB 마이그레이션 (카드 청구 설정 컬럼)
- T35: 카드 청구 설정 API (기존 PUT 확장)
- T36: 카드 결제 현황 백엔드 API (2슬롯 계산 — 케이스 A/B)
- T37: 프론트엔드 mock → 실제 API 교체

**Why:** 목업 → 구현 순서 원칙에 따라 Sprint 4(목업)에서 두 기능을 동시 검증 후, Sprint 5(할부), Sprint 6(카드 현황) 순으로 실제 구현
**How to apply:** DB 스키마 변경 있는 스프린트(5, 6)는 반드시 로컬 PostgreSQL 검증 후 Supabase 적용
