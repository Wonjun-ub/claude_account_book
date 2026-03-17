# 배포 후 수동 작업 가이드

> **목적**: 현재 완료되지 않은 수동 검증/작업 항목만 유지합니다.
> 완료된 기록은 `docs/deploy-history/YYYY-MM-DD.md`로 이동됩니다.

---

## Sprint 9 검증 기록 (2026-03-17)

**스프린트**: Sprint 9 — Local-first 실서비스 구현 (Dexie.js IndexedDB 기반, 백엔드 없음)

### 자동 검증

- ✅ `npm run build` 성공 (TypeScript 오류 0건)
- ✅ `npm test` 84 케이스 전체 PASS (기존 54 + 신규 30)

### 수동 검증

- ⬜ 로컬 직접 실행 검증 (`npm run dev`) — 브라우저에서 전체 플로우 확인
  - HomeView: 거래 추가/수정/삭제, 월 이동, 반복 예정 배너, 카드 결제 위젯
  - StatsView: 카테고리 도넛 차트, 전월 비교, 6개월 추이 막대 차트
  - SettingsView: 카테고리/결제수단/저축 수단 CRUD, 월 시작일 설정
- ⬜ UI 다크모드 시각적 품질 확인 (Galaxy S25 360×780 기준)
- ⬜ IndexedDB 시드 데이터 생성 확인 (브라우저 DevTools > Application > IndexedDB)
- ⬜ PWA 설치 프롬프트 및 아이콘 확인 (Chrome DevTools > Application > Manifest)

### 해당 없음

- 백엔드 API 검증: v2.0 Local-first 전환으로 백엔드 완전 제거
- Playwright E2E: 미구현 (계획)

---

## 참고

- 검증 원칙: `docs/dev-process.md` 섹션 5
- 배포 이력: `docs/deploy-history/`
- 롤백 방법: `docs/dev-process.md` 섹션 6.4
