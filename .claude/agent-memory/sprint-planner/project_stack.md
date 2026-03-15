---
name: 프로젝트 기술 스택 및 구조
description: BudgetTracker(가계부 웹앱) 프로젝트의 기술 스택, 디렉토리 구조, 주요 아키텍처 결정사항
type: project
---

## 프로젝트명
BudgetTracker — 개인 수입/지출 관리 웹앱 MVP

## 기술 스택
- 프론트엔드: Vue3 + Vite + TypeScript + Tailwind CSS
- 차트: Chart.js
- 날짜 처리: Day.js
- 상태관리: Pinia
- 패키지 매니저(프론트): npm
- 백엔드: .NET 8.0 Web API (C#)
- ORM: Entity Framework Core 8.x
- DB: Supabase PostgreSQL
- 배포: Render (프론트 + 백엔드 통합)

## 디렉토리 구조
- `backend/BudgetTracker.Api/` — .NET 8.0 Web API
- `frontend/` — Vue3 + Vite + TS
- `docs/sprint/sprint{n}.md` — 스프린트 계획/완료 문서
- `docs/sprint/sprint{n}/` — 스프린트 첨부 파일

## 핵심 도메인 엔티티
Transaction, Category, PaymentMethod, PointBudget, RecurringTransaction, UserSettings
Sprint 4 추가 예정: InstallmentTransaction (할부 원부 전용 테이블)

## 주요 아키텍처 결정
- MVP 단계에서 Repository 패턴 없이 DbContext 직접 주입으로 단순화
- 반복 지출 자동 반영: on-demand 방식 (월별 요약 API 호출 시 생성)
- 포인트 잔액 차감: 거래 생성/수정/삭제 트랜잭션 내에서 처리

**Why:** MVP 빠른 개발을 위한 단순화 결정
**How to apply:** 스프린트 계획 시 과도한 추상화 레이어 지양
