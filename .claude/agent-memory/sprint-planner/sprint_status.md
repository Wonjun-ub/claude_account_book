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
- 상태: ✅ 완료 (2026-03-15)
- 유형: 목업
- 브랜치: `sprint4`
- 목표: 할부 CRUD 목업 + 반복 거래 CRUD 목업 + Vitest 테스트 환경 구축
- 계획 문서: `docs/sprint/sprint4.md`
- DB 스키마 변경: 없음 (프론트엔드 전용)
- PR: sprint4 → develop (https://github.com/Wonjun-ub/claude_account_book/compare/develop...sprint4)

**달성 사항:**
- T22-mock: 할부 등록 목업 — 인라인 토글, 월별 미리보기, 회차별 거래 자동 생성
- T23-mock: 할부 수정/삭제 목업 — 3가지 삭제 옵션 bottom sheet
- T25-mock: 반복 거래 목업 — dayOfMonth/endDate, 예정 배너, skip 키 영속, 3가지 삭제 옵션
- Vitest 테스트 환경: 54 케이스 전체 통과 (monthPeriod, installment, mockupLogic)
- monthPeriod 유틸 분리 (utils/), FAB 위치 수정, 월 시작일 25일 적용

**미완료 (이월):**
- T24-mock: 카드 결제 현황 탭 목업 → Sprint 6 계획 시 포함 여부 결정

**주의사항:**
- MockupView.vue가 977줄로 대형 단일 파일. 기능 이관 후 자연 해소 예정
- localStorage JSON.parse try/catch 없음 — DEV-only이므로 실서비스 영향 없음
- 수동 검증 미완료: /mock-up UI 직접 확인, docker compose 검증 필요

## Sprint 5
- 상태: ✅ 완료 (2026-03-15)
- 유형: 구현
- 브랜치: `sprint5`
- 목표: 할부/반복 거래 실서비스 이관 (DB + 백엔드 + 프론트 연동)
- 계획 문서: `docs/sprint/sprint5.md`
- DB 스키마 변경: InstallmentTransactions 신규 + RecurringSkips 신규 + Transactions에 FK/Sequence 추가 + RecurringTransactions에 StartDate/EndDate 추가
- PR: https://github.com/Wonjun-ub/claude_account_book/pull/2 (sprint5 → develop)

**달성 사항:**
- T30: InstallmentTransaction + RecurringSkip 엔티티 + 마이그레이션 (20260315134156_Sprint5_InstallmentAndRecurringSkip) Supabase 적용 완료
- T31: 할부 API POST/GET/DELETE (all/fromHere/single 3가지 삭제 모드), 카드사 방식 계산
- T32: 반복 거래 API 확장 — DELETE 3가지 모드(all/fromHere/skipMonth) + GET /pending
- T33: TransactionModal 실제 API 연동 + DeleteOptionSheet.vue 신규 + HomeView 배너/뱃지/건수

**주의사항:**
- 할부 등록 시 Transaction N건 개별 INSERT (루프) — 원자성 없음. 추후 bulk insert 개선 가능
- 수동 검증 미완료: docker compose 환경에서 전체 플로우 검증 필요 (deploy.md 참조)

## Sprint 6
- 상태: ✅ 완료 (2026-03-16)
- 유형: 목업
- 브랜치: `sprint6`
- 목표: 카드 결제 현황 + 홈 탭 UI 개선 목업 확정
- 계획 문서: `docs/sprint/sprint6.md`
- DB 스키마 변경: 없음

**달성 사항:**
- Step 1: 홈 탭 목업 전체 개선 v3 (다크모드, 저축 수단, 필터 아코디언, 카드 결제 위젯, 반복 배너 개선)
- Step 2: UI/UX 승인 완료 (2026-03-16)
- 카드 결제 현황 실서비스 구현은 Sprint 9로 이관

## Sprint 7
- 상태: ✅ 완료 (2026-03-17)
- 유형: 목업
- 브랜치: `sprint7`
- 목표: MockupView 통계 탭 UI/UX 목업 확정
- 계획 문서: `docs/sprint/sprint7.md`
- DB 스키마 변경: 없음
- 머지: sprint7 → develop (commit: 7353d16)

**달성 사항:**
- T38: 통계 탭 다크모드 목업 (카테고리 도넛 차트, 수입/지출/저축 탭, 전월 비교 4열 카드)
- T39: 최근 6개월 막대 추이 (수입/지출/저축 3색, 다크모드 그리드)
- 추가 개선: 수입 대비 %, 잔액 바, 범례 비율 표시
- Step 2 승인 완료 (2026-03-17)
- npm run build 성공 (TypeScript 오류 0건)

**주의사항:**
- MockupView.vue 파일이 계속 증가 중 → Sprint 9 실서비스 이관 시 각 View로 분리 예정
- 다음 사용 가능한 스프린트 번호: Sprint 8 (설정 화면 목업)
