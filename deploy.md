# 배포 후 수동 작업 가이드

> **목적**: 현재 완료되지 않은 수동 검증/작업 항목만 유지합니다.
> 완료된 기록은 `docs/deploy-history/YYYY-MM-DD.md`로 이동됩니다.

---

## 현재 미완료 항목

### Sprint 3 — 버그 수정 & 거래 유형 탭 UI 목업 (2026-03-15)

#### 자동 검증

- ⬜ 백엔드 서버 미실행으로 자동 검증 미수행 (dotnet test, API curl, Playwright 건너뜀)

#### 수동 검증 필요 항목

- ⬜ `docker compose up --build` — 로컬 Docker 환경에서 전체 스택 실행 확인
- ⬜ T20 검증: `monthStartDay=25` 설정 후 `GET /api/transactions?year=2026&month=3` 조회 시 2월 25일~28일 거래 포함 확인
- ⬜ T20 검증: 요약 API 집계 금액과 거래 목록 합계 일치 확인
- ⬜ T21 검증: 거래 추가 모달 일반/반복/할부 탭 전환 동작 확인
- ⬜ T21 검증: 반복(Fixed) 탭으로 거래 등록 후 `GET /api/recurring-transactions` 목록 확인
- ⬜ T21 검증: 기존 거래 수정 시 유형 탭 read-only 표시 및 수정 정상 동작 확인
- ⬜ T21 검증: 할부 탭 → 총 개월수 입력 → 미리보기 표시 → 저장 시 "준비 중" 안내 확인
- ⬜ UI 디자인/시각적 품질 확인

---

## 참고

- 검증 원칙: `docs/dev-process.md` 섹션 5
- 배포 이력: `docs/deploy-history/`
- 롤백 방법: `docs/dev-process.md` 섹션 6.4
