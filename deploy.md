# 배포 후 수동 작업 가이드

> **목적**: 현재 완료되지 않은 수동 검증/작업 항목만 유지합니다.
> 완료된 기록은 `docs/deploy-history/YYYY-MM-DD.md`로 이동됩니다.

---

## Sprint 8 검증 기록 (2026-03-17)

**스프린트**: Sprint 8 — 설정 화면 목업 + Local-first v2.0 전환 (목업 전용, DB/백엔드 변경 없음)

### 자동 검증

- ✅ `npm run build` 성공 (TypeScript 오류 0건)

### 수동 검증

- ✅ `/mock-up` → 설정 탭 UI/UX 확인 (사용자 승인 완료, 2026-03-17)
- ⬜ Vitest 단위 테스트 (`npm run test`) — 목업 변경이므로 기존 케이스 영향 없음, 선택적 실행

### 해당 없음 (목업 전용)

- 백엔드 API 검증: DB/백엔드 변경 없음 (v2.0 Local-first 전환으로 백엔드 제거)
- Playwright E2E: 목업 스프린트 해당 없음

---

## 참고

- 검증 원칙: `docs/dev-process.md` 섹션 5
- 배포 이력: `docs/deploy-history/`
- 롤백 방법: `docs/dev-process.md` 섹션 6.4
