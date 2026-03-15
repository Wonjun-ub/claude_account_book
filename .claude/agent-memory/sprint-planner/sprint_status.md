---
name: 스프린트 진행 현황
description: 각 스프린트의 상태, 목표, 주요 달성 사항 기록
type: project
---

## Sprint 1
- 상태: ✅ 완료 (2026-03-13)
- 브랜치: `sprint1`
- 기간: 2026-03-13 ~ 2026-03-13 (단일 세션)
- 목표: 백엔드 핵심 API (T1~T10) 전체 구현
- 계획 문서: `docs/sprint/sprint1.md`
- PR 대상: `develop` 브랜치

**주요 달성 사항:**
- .NET 9 Web API + Vue3 초기 프로젝트 구조 설정
- 6개 엔티티 + EF Core + Fluent API + InitialCreate 마이그레이션
- 7개 컨트롤러 (Transactions, Categories, PaymentMethods, PointBudgets, RecurringTransactions, Summary, Settings)
- 포인트 잔액 차감/복구 원자성 처리 (거래 생성/수정/삭제)
- 반복 지출 on-demand 자동 반영 + 멱등성 보장
- DateRangeHelper: 커스텀 월 시작일 기준 날짜 범위 계산

**핵심 주의사항:**
- `SummaryController`가 `RecurringTransactionsController`를 직접 인스턴스화하는 패턴 사용 (DI 미활용) — Sprint 2 또는 리팩터링 스프린트에서 개선 권장
- 로컬 Node.js 16 환경에서는 프론트 빌드 불가 (Node 20 필요)
- Supabase 마이그레이션 적용은 수동 검증 단계에서 처리 필요

**Why:** Phase 1 — DB 모델 설계 + 핵심 REST API 구현이 Sprint 1 목표
**How to apply:** Sprint 2 계획 수립 시 T11~T19(Vue3 UI) 기반으로 작성

## Sprint 2
- 상태: 🔄 진행 중 (2026-03-13 시작)
- 브랜치: `sprint2` (develop에서 분기)
- 기간: 2026-03-13 ~ 2026-03-27 (2주)
- 목표: 프론트엔드 UI 전체 구현 + 프론트-백엔드 통합 (T11~T19)
- 계획 문서: `docs/sprint/sprint2.md`
- PR 대상: `develop` 브랜치
- 백엔드 API URL: https://budget-tracker-api-51n7.onrender.com

**주요 구현 범위:**
- T11: Vue3 기본 구조 — 라우터, Pinia, API 모듈, Tailwind CSS 3, Chart.js, Day.js
- T12: 가계부 메인 화면 — 월별 거래 목록 + 수입/지출/잔액 요약
- T13: 거래 입력/수정 화면 — 카테고리, 결제수단, 합산여부, 반복 설정
- T14: 카테고리/결제수단 관리 화면
- T15: 포인트 예산 관리 화면
- T16: 통계 화면 — 파이차트 + 전월 비교 + 막대그래프
- T17: 검색/필터 UI
- T18: 반응형 UI (모바일 우선)
- T19: 통합 테스트

**핵심 주의사항:**
- writing-plans 스킬 없음 → 일반 애자일 방법론 적용
- Render 무료 플랜 콜드 스타트(최대 50초) 주의 — 개발 중 로컬 백엔드 병행 권장
- 기존 `counter.ts` 스토어, `HelloWorld.vue` 등 템플릿 파일 교체 필요
- `@tailwindcss/vite` 플러그인 또는 PostCSS 방식 명시적 설정 필요
