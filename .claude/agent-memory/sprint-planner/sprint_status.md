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
- 상태: ⬜ 예정
- 목표: 프론트엔드 UI 전체 구현 + 프론트-백엔드 통합 (T11~T19)
