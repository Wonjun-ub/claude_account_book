# 배포 후 수동 작업 가이드

> **목적**: 현재 완료되지 않은 수동 검증/작업 항목만 유지합니다.
> 완료된 기록은 `docs/deploy-history/YYYY-MM-DD.md`로 이동됩니다.

---

## 현재 미완료 항목

### Sprint 4 — 할부·반복 거래 목업 + Vitest 테스트 환경 (2026-03-15)

#### 자동 검증

- ✅ `npm test` — 54 케이스 전체 통과
- ✅ `npm run build` — TypeScript 타입 오류 0건
- ⬜ 백엔드 미실행 — dotnet test, API curl, Playwright 건너뜀

#### 수동 검증 필요 항목

- ⬜ 로컬 `/mock-up` 접속 후 UI 직접 확인
  - 할부 등록: 지출 탭 할부 토글 ON → 개월수 입력 → 미리보기 → 저장 → 목록 배지 표시
  - 할부 수정: 할부 거래 클릭 → 수정 모달 (할부 UI로 열림) 확인
  - 할부 삭제: X 버튼 → 3가지 옵션 bottom sheet (전체/이후/단건) 동작 확인
  - 반복 등록: 부가 정보 토글 ON → dayOfMonth/endDate 입력 → 저장 → 예정 배너 표시 확인
  - 반복 삭제: X 버튼 → 3가지 옵션 동작 확인 + 단건 삭제 후 배너 재등장 방지 확인
- ⬜ `docker compose up --build` — 로컬 Docker 환경에서 전체 스택 실행 확인
- ⬜ UI 디자인/시각적 품질 확인

---

## 참고

- 검증 원칙: `docs/dev-process.md` 섹션 5
- 배포 이력: `docs/deploy-history/`
- 롤백 방법: `docs/dev-process.md` 섹션 6.4
